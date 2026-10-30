using System;
using System.Linq;
using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class OrderSessionTests
    {
        private RecordingOrderEventSink _events;
        private int _connectCount;
        private int _disposeCount;

        [SetUp]
        public void SetUp()
        {
            _events = new RecordingOrderEventSink();
            _connectCount = 0;
            _disposeCount = 0;
        }

        private OrderSession Create()
        {
            var catalog = new RecipeCatalog(new[]
            {
                RecipeTestFactory.Create("EM"), RecipeTestFactory.Create("BL"), RecipeTestFactory.Create("DX")
            });
            var settings = new OrderIntakeSettings(1, 10, "DEFAULT", 1, "LOT-", 4, ',', '"', '#', CsvOrderColumns.Default);
            return new OrderSession(catalog, settings, new OrderManagerSettings(3, new[] { StationId.ArmA }), _events, manager =>
            {
                _connectCount++;
                return new Disposer(() => _disposeCount++);
            });
        }

        private sealed class Disposer : IDisposable
        {
            private readonly Action _onDispose;

            internal Disposer(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose() => _onDispose();
        }

        [Test]
        public void BeforeAnyLoad_HasNoKitsAndZeroStatus()
        {
            var session = Create();

            Assert.That(session.TryGetNext(out _, out _), Is.False);
            Assert.That(session.PendingKitCount, Is.EqualTo(0));
            Assert.That(session.HeldOrderCount, Is.EqualTo(0));
            Assert.That(session.LastIntake, Is.Null);
        }

        [Test]
        public void Load_Csv_MakesOrdersAndKitsAvailableThroughIKitSource()
        {
            var session = Create();

            var result = session.Load(new StringOrderTextSource("1,A병원,EM,2\n2,B병원,DX,1"));

            Assert.That(result.Orders.Count, Is.EqualTo(2));
            Assert.That(session.PendingKitCount, Is.EqualTo(3));
            Assert.That(session.TryGetNext(out KitData kit, out _), Is.True);
            Assert.That(kit.KitCode, Is.EqualTo("EM"));
            Assert.That(kit.HospitalName, Is.EqualTo("A병원"));
        }

        [Test]
        public void Load_IssuesAreForwardedAsWarnings()
        {
            Create().Load(new StringOrderTextSource("1,A,ZZ,1\n2,B,EM,1"));

            Assert.That(_events.WarningCodes, Is.EqualTo(new[] { OrderIssueCodes.UnknownKitCode }));
        }

        [Test]
        public void Load_Again_ReplacesTheOrdersWithoutChangingTheSessionReference()
        {
            var session = Create();
            IKitSource source = session;
            session.Load(new StringOrderTextSource("1,A,EM,3"));
            session.TryGetNext(out _, out _);

            session.Load(new StringOrderTextSource("1,B,DX,1"));

            Assert.That(session.PendingKitCount, Is.EqualTo(1));
            Assert.That(source.TryGetNext(out KitData kit, out _), Is.True);
            Assert.That(kit.KitCode, Is.EqualTo("DX"));
            Assert.That(kit.KitSequence, Is.EqualTo(1), "KitSequence는 파일마다 1부터 센다");
        }

        [Test]
        public void Load_Again_ContinuesLotNumbersSoTheyStayUniqueInOneRun()
        {
            var session = Create();
            session.Load(new StringOrderTextSource("1,A,EM,2"));
            session.TryGetNext(out KitData first, out _);
            session.TryGetNext(out KitData second, out _);

            session.Load(new StringOrderTextSource("1,A,EM,1"));
            session.TryGetNext(out KitData third, out _);

            Assert.That(new[] { first.LotNumber, second.LotNumber, third.LotNumber },
                Is.EqualTo(new[] { "LOT-0001", "LOT-0002", "LOT-0003" }));
        }

        [Test]
        public void Load_MissingFile_FallsBackToOneKitPerRecipeAndWarns()
        {
            var session = Create();

            var result = session.Load(new FileOrderTextSource("no/such/orders.csv"));

            Assert.That(result.KitRequests.Count, Is.EqualTo(3));
            Assert.That(_events.WarningCodes, Is.EqualTo(new[] { OrderIssueCodes.FileMissing }));
        }

        [Test]
        public void Load_ConnectsTheNewManagerAndDisposesThePreviousConnection()
        {
            var session = Create();

            session.Load(new StringOrderTextSource("1,A,EM,1"));
            Assert.That((_connectCount, _disposeCount), Is.EqualTo((1, 0)));

            session.Load(new StringOrderTextSource("1,A,EM,1"));
            Assert.That((_connectCount, _disposeCount), Is.EqualTo((2, 1)));
        }

        [Test]
        public void Disconnect_DisposesTheConnectionOnlyOnce()
        {
            var session = Create();
            session.Load(new StringOrderTextSource("1,A,EM,1"));

            session.Disconnect();
            session.Disconnect();

            Assert.That(_disposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Load_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Create().Load(null));
        }

        [Test]
        public void Session_UsedAsIOrderStatus_ReportsHeldAndPending()
        {
            var session = Create();
            session.Load(new StringOrderTextSource("1,A,EM,2"));
            IOrderStatus status = session;

            session.TryGetNext(out _, out _);

            Assert.That(status.PendingKitCount, Is.EqualTo(1));
            Assert.That(status.HeldOrderCount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            var settings = new OrderIntakeSettings(1, 10, "D", 1, "LOT-", 4, ',', '"', '#', CsvOrderColumns.Default);
            var catalog = new RecipeCatalog(new[] { RecipeTestFactory.Create("EM") });

            Assert.Throws<ArgumentNullException>(() =>
                new OrderSession(null, settings, new OrderManagerSettings(3, Array.Empty<StationId>()), _events));
            Assert.Throws<ArgumentNullException>(() =>
                new OrderSession(catalog, settings, null, _events));
        }
    }
}
