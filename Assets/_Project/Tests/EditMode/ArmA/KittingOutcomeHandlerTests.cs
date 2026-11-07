using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class KittingOutcomeHandlerTests
    {
        private const float FaultRecoverySeconds = 5f;

        private FakeSink _events;
        private FakeStation _station;
        private KittingOutcomeHandler _handler;
        private KitData _kit;

        [SetUp]
        public void SetUp()
        {
            _events = new FakeSink();
            _station = new FakeStation();
            _handler = new KittingOutcomeHandler(_events, _station, FaultRecoverySeconds);
            _kit = new KitData(1, 1, "LOT-0001", "A병원", "EM");
        }

        private sealed class FakeSink : IKittingEventSink
        {
            internal List<KitData> Ready { get; } = new List<KitData>();

            internal List<(string code, string message)> Warnings { get; } = new List<(string, string)>();

            public void KitReady(KitData kit) => Ready.Add(kit);

            public void WarningRaised(string code, string message) => Warnings.Add((code, message));
        }

        private sealed class FakeStation : IStationStateReporter
        {
            internal List<StationState> Changes { get; } = new List<StationState>();

            public StationState State { get; private set; }

            public void Set(StationState state)
            {
                State = state;
                Changes.Add(state);
            }

            public void Tick(float unscaledNow)
            {
            }
        }

        private static KittingOutcome Completed()
        {
            var outcome = new KittingOutcome();
            outcome.Complete();
            return outcome;
        }

        private static KittingOutcome Failed(KittingResult result, string message)
        {
            var outcome = new KittingOutcome();
            outcome.Fail(result, "A-2", message);
            return outcome;
        }

        [Test]
        public void Completed_RaisesKitReadyWithTheKitAndDoesNotWait()
        {
            float wait = _handler.Handle(_kit, Completed());

            Assert.That(_events.Ready, Is.EqualTo(new[] { _kit }));
            Assert.That(_events.Warnings, Is.Empty);
            Assert.That(wait, Is.EqualTo(0f));
            Assert.That(_station.Changes, Is.Empty);
        }

        [Test]
        public void StockShortage_RaisesNothingAndDoesNotWait()
        {
            float wait = _handler.Handle(_kit, Failed(KittingResult.StockShortage, "재고 없음"));

            Assert.That(_events.Ready, Is.Empty);
            Assert.That(_events.Warnings, Is.Empty);
            Assert.That(wait, Is.EqualTo(0f));
            Assert.That(_station.Changes, Is.Empty, "주문 보류는 주문 관리가 StockEmpty로 이미 했다");
        }

        [Test]
        public void Fault_SetsTheStationToFaultWarnsWithTheMessageAndAsksToWait()
        {
            float wait = _handler.Handle(_kit, Failed(KittingResult.Fault, "닿지 못함"));

            Assert.That(_station.State, Is.EqualTo(StationState.Fault));
            Assert.That(_events.Warnings, Is.EqualTo(new[] { (KittingIssueCodes.ArmFault, "닿지 못함") }));
            Assert.That(_events.Ready, Is.Empty);
            Assert.That(wait, Is.EqualTo(FaultRecoverySeconds));
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new KittingOutcomeHandler(null, _station, 1f));
            Assert.Throws<System.ArgumentNullException>(() => new KittingOutcomeHandler(_events, null, 1f));
        }
    }
}
