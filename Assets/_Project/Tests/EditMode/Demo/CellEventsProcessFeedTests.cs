using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    /// <summary>이벤트 버스(CellEvents)에서 ProcessTally로 신호가 실제로 건너가는지, 끊으면 더 안 건너가는지 시험한다.</summary>
    public sealed class CellEventsProcessFeedTests
    {
        private ProcessTally _tally;
        private CellEventsProcessFeed _feed;

        [SetUp]
        public void SetUp()
        {
            _tally = new ProcessTally();
            _feed = new CellEventsProcessFeed(_tally);
        }

        [TearDown]
        public void TearDown()
        {
            // 시험이 실패해도 다음 시험이 이 구독을 이어받지 않게 한다.
            _feed.Detach();
        }

        private static KitData Judged(int sequence, bool isPass)
        {
            var kit = new KitData(sequence, sequence, $"LOT-{sequence}", "병원", "BL");
            kit.Verdict = new InspectionVerdict(isPass, null, 1f, JudgeMethod.Cnn);
            return kit;
        }

        [Test]
        public void WhileAttached_TheBusSignalsReachTheTally()
        {
            _feed.Attach();

            CellEvents.RaiseInspectionResult(Judged(1, isPass: false));
            CellEvents.RaiseAtConveyorEnd(Judged(2, isPass: true));
            CellEvents.RaiseStockEmpty("A-1", 3);
            CellEvents.RaiseStationStateChanged(StationId.Pusher, StationState.Fault);
            CellEvents.RaiseWarning("W1", "경고");

            Assert.That(_tally.RejectedCount, Is.EqualTo(1));
            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
            Assert.That(_tally.StockShortageCount, Is.EqualTo(1));
            Assert.That(_tally.FaultCount, Is.EqualTo(1));
            Assert.That(_tally.LastWarning, Is.EqualTo("W1: 경고"));
        }

        [Test]
        public void AttachingTwice_DoesNotCountEachSignalTwice()
        {
            _feed.Attach();
            _feed.Attach();

            CellEvents.RaiseStockEmpty("A-1", 1);

            Assert.That(_tally.StockShortageCount, Is.EqualTo(1));
        }

        [Test]
        public void AfterDetaching_TheSignalsAreNoLongerCounted()
        {
            _feed.Attach();
            _feed.Detach();

            CellEvents.RaiseStockEmpty("A-1", 1);
            CellEvents.RaiseAtConveyorEnd(Judged(1, isPass: true));

            Assert.That(_tally.StockShortageCount, Is.EqualTo(0));
            Assert.That(_tally.CompletedCount, Is.EqualTo(0));
        }

        [Test]
        public void BeforeAttaching_NothingIsCounted()
        {
            CellEvents.RaiseStockEmpty("A-1", 1);

            Assert.That(_tally.StockShortageCount, Is.EqualTo(0));
        }
    }
}
