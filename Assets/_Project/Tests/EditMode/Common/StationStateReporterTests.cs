using System.Collections.Generic;
using NUnit.Framework;

namespace MediKitTwin.Common.Tests
{
    public sealed class StationStateReporterTests
    {
        private readonly List<(StationId, StationState)> _changed = new List<(StationId, StationState)>();
        private readonly List<(StationId, StationState)> _alive = new List<(StationId, StationState)>();

        [SetUp]
        public void SetUp()
        {
            _changed.Clear();
            _alive.Clear();
            CellEvents.StationStateChanged += OnChanged;
            CellEvents.StationAlive += OnAlive;
        }

        [TearDown]
        public void TearDown()
        {
            CellEvents.StationStateChanged -= OnChanged;
            CellEvents.StationAlive -= OnAlive;
        }

        private void OnChanged(StationId station, StationState state) => _changed.Add((station, state));

        private void OnAlive(StationId station, StationState state) => _alive.Add((station, state));

        [Test]
        public void Set_NewState_RaisesStateChangedOnce()
        {
            var reporter = new StationStateReporter(StationId.ArmA);

            reporter.Set(StationState.Running);

            Assert.That(_changed, Is.EqualTo(new[] { (StationId.ArmA, StationState.Running) }));
            Assert.That(reporter.State, Is.EqualTo(StationState.Running));
        }

        [Test]
        public void Set_SameState_RaisesNothing()
        {
            var reporter = new StationStateReporter(StationId.ArmA);

            reporter.Set(StationState.Idle);

            Assert.That(_changed, Is.Empty);
        }

        [Test]
        public void Tick_FirstCall_RaisesAliveImmediately()
        {
            new StationStateReporter(StationId.Conveyor).Tick(100f);

            Assert.That(_alive, Is.EqualTo(new[] { (StationId.Conveyor, StationState.Idle) }));
        }

        [Test]
        public void Tick_WithinTheInterval_DoesNotRepeat()
        {
            var reporter = new StationStateReporter(StationId.ArmA);
            reporter.Tick(0f);

            reporter.Tick(StationHeartbeat.IntervalSeconds * 0.5f);

            Assert.That(_alive.Count, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AfterTheInterval_RaisesAgainWithTheCurrentState()
        {
            var reporter = new StationStateReporter(StationId.ArmA);
            reporter.Tick(0f);
            reporter.Set(StationState.Running);

            reporter.Tick(StationHeartbeat.IntervalSeconds);

            Assert.That(_alive[1], Is.EqualTo((StationId.ArmA, StationState.Running)));
        }

        [Test]
        public void Tick_SameStateStillRaisesAliveEachInterval()
        {
            var reporter = new StationStateReporter(StationId.ArmA);

            reporter.Tick(0f);
            reporter.Tick(StationHeartbeat.IntervalSeconds);
            reporter.Tick(StationHeartbeat.IntervalSeconds * 2f);

            Assert.That(_alive.Count, Is.EqualTo(3));
            Assert.That(_changed, Is.Empty);
        }
    }
}
