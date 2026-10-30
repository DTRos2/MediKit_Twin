using System.Linq;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class OrderManagerTests
    {
        private RecordingOrderEventSink _events;

        [SetUp]
        public void SetUp()
        {
            _events = new RecordingOrderEventSink();
        }

        private OrderManager Create(string csv, params string[] recipeCodes)
        {
            var catalog = new RecipeCatalog(recipeCodes.Select(RecipeTestFactory.Create));
            var loader = new OrderLoader(
                new StringOrderTextSource(csv),
                OrderTestFactory.Parser(new QuantityRangeValidator(1, 10), new UniqueSequenceValidator()),
                new PerKitCodeDefaultOrderProvider(catalog.KitCodes, "DEFAULT", 1));
            var intake = new OrderIntake(loader, new KitRequestPlanner(new SequentialLotNumberGenerator("LOT-", 4))).Run();
            var settings = new OrderManagerSettings(3, new[] { StationId.ArmA });
            return new OrderManager(intake, catalog, _events, settings);
        }

        private static KitData Next(OrderManager manager)
        {
            Assert.That(manager.TryGetNext(out KitData kit, out RecipeSO _), Is.True);
            return kit;
        }

        // 주문 1(EM x1), 주문 2(DX x2), 주문 3(BL x1)
        private OrderManager MixedManager()
        {
            return Create("1,A병원,EM,1\n2,B병원,DX,2\n3,C병원,BL,1", "EM", "DX", "BL");
        }

        [Test]
        public void TryGetNext_ReturnsKitsInFileOrderWithIncreasingSequence()
        {
            var manager = MixedManager();

            var kits = new[] { Next(manager), Next(manager), Next(manager), Next(manager) };

            Assert.That(kits.Select(k => k.KitCode), Is.EqualTo(new[] { "EM", "DX", "DX", "BL" }));
            Assert.That(kits.Select(k => k.KitSequence), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void TryGetNext_ReturnsRecipeOfTheKit()
        {
            var manager = MixedManager();

            manager.TryGetNext(out KitData kit, out RecipeSO recipe);

            Assert.That(recipe.KitCode, Is.EqualTo(kit.KitCode));
        }

        [Test]
        public void TryGetNext_AllKitsDone_ReturnsFalse()
        {
            var manager = Create("1,A,EM,1", "EM");
            Next(manager);

            Assert.That(manager.TryGetNext(out KitData kit, out RecipeSO recipe), Is.False);
            Assert.That(kit, Is.Null);
            Assert.That(recipe, Is.Null);
        }

        [Test]
        public void PendingKitCount_DecreasesAsKitsAreHandedOut()
        {
            var manager = MixedManager();
            Assert.That(manager.PendingKitCount, Is.EqualTo(4));

            Next(manager);
            Next(manager);

            Assert.That(manager.PendingKitCount, Is.EqualTo(2));
        }

        [Test]
        public void TryGetNext_MissingRecipe_HoldsOrderWarnsAndServesNextOrder()
        {
            var manager = Create("1,A,EM,1\n2,B,DX,1", "EM");

            // 주문 1(EM)을 먼저 소진한 뒤 레시피가 없는 주문 2(DX)를 만난다.
            Next(manager);
            bool hasNext = manager.TryGetNext(out _, out _);

            Assert.That(hasNext, Is.False);
            Assert.That(_events.WarningCodes, Is.EqualTo(new[] { OrderIssueCodes.RecipeMissing }));
            Assert.That(_events.Held, Is.EqualTo(new[] { 2 }));
            Assert.That(manager.HeldOrderCount, Is.EqualTo(1));
        }

        [Test]
        public void TryGetNext_MissingRecipeFirst_SkipsToNextOrder()
        {
            // 파서에 코드 검증기를 두지 않아 레시피 없는 주문이 대기열에 들어온 상황이다.
            var manager = Create("1,A,ZZ,1\n2,B,EM,1", "EM");

            var kit = Next(manager);

            Assert.That(kit.OrderSequence, Is.EqualTo(2));
            Assert.That(_events.Held, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void OnStockEmpty_HoldsOrderOnceEvenIfSignalRepeats()
        {
            var manager = MixedManager();
            Next(manager);
            Next(manager);

            manager.OnStockEmpty("C-3", 2);
            manager.OnStockEmpty("C-3", 2);

            Assert.That(_events.Held, Is.EqualTo(new[] { 2 }));
            Assert.That(manager.HeldOrderCount, Is.EqualTo(1));
        }

        [Test]
        public void OnStockEmpty_OrderKeepsMakingNextOrderMeanwhile()
        {
            var manager = MixedManager();
            Next(manager);
            Next(manager);
            manager.OnStockEmpty("C-3", 2);

            var kit = Next(manager);

            Assert.That(kit.KitCode, Is.EqualTo("BL"));
        }

        [Test]
        public void OnStockEmpty_InFlightKitIsRequeuedSoPendingCountKeepsIt()
        {
            var manager = MixedManager();
            Next(manager);
            Next(manager);
            Assert.That(manager.PendingKitCount, Is.EqualTo(2));

            manager.OnStockEmpty("C-3", 2);

            Assert.That(manager.PendingKitCount, Is.EqualTo(3));
        }

        [Test]
        public void OnStockRefilled_ResumesHeldOrderAtFrontWithNewKitDataAndSameLot()
        {
            var manager = MixedManager();
            Next(manager);
            var interrupted = Next(manager);
            manager.OnStockEmpty("C-3", 2);
            Next(manager);

            manager.OnStockRefilled("C-3");
            var resumed = Next(manager);

            Assert.That(_events.Resumed, Is.EqualTo(new[] { 2 }));
            Assert.That(resumed.OrderSequence, Is.EqualTo(2));
            Assert.That(resumed, Is.Not.SameAs(interrupted));
            Assert.That(resumed.LotNumber, Is.EqualTo(interrupted.LotNumber));
            Assert.That(resumed.KitSequence, Is.EqualTo(4));
            Assert.That(manager.HeldOrderCount, Is.EqualTo(0));
        }

        [Test]
        public void OnStockRefilled_OtherSlot_DoesNotResume()
        {
            var manager = MixedManager();
            Next(manager);
            Next(manager);
            manager.OnStockEmpty("C-3", 2);

            manager.OnStockRefilled("C-1");

            Assert.That(_events.Resumed, Is.Empty);
            Assert.That(manager.HeldOrderCount, Is.EqualTo(1));
        }

        [Test]
        public void OnStockRefilled_OrderWaitingOnTwoSlots_ResumesOnlyAfterBoth()
        {
            var manager = MixedManager();
            Next(manager);
            Next(manager);
            manager.OnStockEmpty("C-3", 2);
            manager.OnStockEmpty("C-1", 2);

            manager.OnStockRefilled("C-3");
            Assert.That(_events.Resumed, Is.Empty);

            manager.OnStockRefilled("C-1");
            Assert.That(_events.Resumed, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void OnStockRefilled_SeveralHeldOrders_ResumeInHeldOrderAtFront()
        {
            var manager = Create("1,A,EM,1\n2,B,DX,1\n3,C,BL,1\n4,D,EM,1", "EM", "DX", "BL");
            Next(manager);
            manager.OnStockEmpty("A-1", 1);
            Next(manager);
            manager.OnStockEmpty("A-1", 2);

            manager.OnStockRefilled("A-1");

            Assert.That(_events.Resumed, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(Next(manager).OrderSequence, Is.EqualTo(1));
            Assert.That(Next(manager).OrderSequence, Is.EqualTo(2));
            Assert.That(Next(manager).OrderSequence, Is.EqualTo(3));
        }

        [Test]
        public void AllOrdersHeld_TryGetNextFalseThenTrueAfterRefill()
        {
            var manager = Create("1,A,EM,1", "EM");
            Next(manager);
            manager.OnStockEmpty("A-1", 1);

            Assert.That(manager.TryGetNext(out _, out _), Is.False);

            manager.OnStockRefilled("A-1");
            Assert.That(manager.TryGetNext(out _, out _), Is.True);
        }

        [Test]
        public void OnStockEmpty_ForFinishedOrUnknownOrder_IsIgnored()
        {
            var manager = Create("1,A,EM,1", "EM");
            manager.OnKitReady(Next(manager));

            manager.OnStockEmpty("A-1", 1);
            manager.OnStockEmpty("A-1", 99);

            Assert.That(_events.Held, Is.Empty);
            Assert.That(manager.HeldOrderCount, Is.EqualTo(0));
        }

        [Test]
        public void RepeatedHold_WarnsOnceWhenReachingThreshold()
        {
            var manager = Create("1,A,EM,1", "EM");

            for (int i = 0; i < 4; i++)
            {
                Next(manager);
                manager.OnStockEmpty("A-1", 1);
                manager.OnStockRefilled("A-1");
            }

            Assert.That(_events.Held.Count, Is.EqualTo(4));
            Assert.That(_events.WarningCodes, Is.EqualTo(new[] { OrderIssueCodes.RepeatedHold }));
        }

        [Test]
        public void StationFault_HoldsInFlightOrderAndRecoveryResumesIt()
        {
            var manager = MixedManager();
            manager.OnKitReady(Next(manager));
            Next(manager);

            manager.OnStationStateChanged(StationId.ArmA, StationState.Fault);
            Assert.That(_events.Held, Is.EqualTo(new[] { 2 }));

            manager.OnStationStateChanged(StationId.ArmA, StationState.Idle);
            Assert.That(_events.Resumed, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void StationFault_OfStationNotConfigured_IsIgnored()
        {
            var manager = MixedManager();
            Next(manager);

            manager.OnStationStateChanged(StationId.Conveyor, StationState.Fault);

            Assert.That(_events.Held, Is.Empty);
        }

        [Test]
        public void StationFault_DoesNotResumeOrderHeldForStock()
        {
            var manager = MixedManager();
            Next(manager);
            Next(manager);
            manager.OnStockEmpty("C-3", 2);

            manager.OnStationStateChanged(StationId.ArmA, StationState.Idle);

            Assert.That(_events.Resumed, Is.Empty);
        }

        [Test]
        public void OnKitReady_FinishedKitIsNotRequeuedByLaterHold()
        {
            var manager = Create("1,B,DX,2", "DX");
            manager.OnKitReady(Next(manager));
            Next(manager);

            manager.OnStockEmpty("C-3", 1);

            // 끝난 첫 키트는 되돌리지 않고, 만들던 둘째 키트만 남은 키트로 돌아온다.
            Assert.That(manager.PendingKitCount, Is.EqualTo(1));
        }

        [Test]
        public void Hold_SeveralInFlightKitsOfOneOrder_AreRequeuedInOriginalOrder()
        {
            var manager = Create("1,B,DX,3", "DX");
            Next(manager);
            Next(manager);

            manager.OnStockEmpty("C-3", 1);
            manager.OnStockRefilled("C-3");

            Assert.That(Next(manager).LotNumber, Is.EqualTo("LOT-0001"));
            Assert.That(Next(manager).LotNumber, Is.EqualTo("LOT-0002"));
            Assert.That(Next(manager).LotNumber, Is.EqualTo("LOT-0003"));
        }

        [Test]
        public void StationFault_HoldsEveryOrderWithKitInFlight()
        {
            var manager = MixedManager();
            Next(manager);
            Next(manager);

            manager.OnStationStateChanged(StationId.ArmA, StationState.Fault);

            Assert.That(_events.Held, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Bridge_ForwardsCellEventsUntilDisposed()
        {
            var manager = MixedManager();
            var first = Next(manager);
            Next(manager);

            using (new OrderManagerCellEventsBridge(manager))
            {
                CellEvents.RaiseKitReady(first);
                CellEvents.RaiseStockEmpty("C-3", 2);
                CellEvents.RaiseStockRefilled("C-3");
            }

            CellEvents.RaiseStockEmpty("C-3", 2);

            Assert.That(_events.Held, Is.EqualTo(new[] { 2 }));
            Assert.That(_events.Resumed, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Settings_InvalidThreshold_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new OrderManagerSettings(0, new StationId[0]));
        }
    }
}
