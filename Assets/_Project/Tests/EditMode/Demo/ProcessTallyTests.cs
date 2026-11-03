using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ProcessTallyTests
    {
        private ProcessTally _tally;

        [SetUp]
        public void SetUp()
        {
            _tally = new ProcessTally();
        }

        private static KitData Kit(int sequence) => new KitData(sequence, sequence, $"LOT-{sequence}", "병원", "BL");

        private static KitData Judged(int sequence, bool isPass)
        {
            KitData kit = Kit(sequence);
            kit.Verdict = new InspectionVerdict(isPass, null, 1f, JudgeMethod.Cnn);
            return kit;
        }

        [Test]
        public void AtTheStart_NothingIsCountedAndEveryStationIsIdle()
        {
            Assert.That(_tally.CompletedCount, Is.EqualTo(0));
            Assert.That(_tally.RejectedCount, Is.EqualTo(0));
            Assert.That(_tally.StockShortageCount, Is.EqualTo(0));
            Assert.That(_tally.FaultCount, Is.EqualTo(0));
            Assert.That(_tally.CurrentKit, Is.Null);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.None));
            Assert.That(_tally.LastWarning, Is.Empty);
            Assert.That(_tally.StateOf(StationId.ArmA), Is.EqualTo(StationState.Idle));
        }

        [Test]
        public void AKitThatPasses_WalksTheLineAndCountsAsCompletedOnlyAtTheEnd()
        {
            KitData kit = Judged(1, isPass: true);

            _tally.OnKitReady(kit);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.ToInspection));
            _tally.OnAtInspection(kit);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.Inspecting));
            _tally.OnInspectionResult(kit);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.ToPacking));
            _tally.OnAtPacking(kit);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.Packing));
            _tally.OnLidClosed(kit);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.ToEnd));
            Assert.That(_tally.CompletedCount, Is.EqualTo(0), "끝에 닿기 전에는 완료가 아니다");

            _tally.OnAtConveyorEnd(kit);

            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.Done));
            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
            Assert.That(_tally.RejectedCount, Is.EqualTo(0));
        }

        [Test]
        public void TheSameKitReachingTheEndAndShipping_IsCountedOnce()
        {
            KitData kit = Judged(1, isPass: true);

            _tally.OnAtConveyorEnd(kit);
            _tally.OnKitShipped(kit);

            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
        }

        [Test]
        public void AKitThatFailsInspection_CountsAsRejectedAndNotCompleted()
        {
            KitData kit = Judged(1, isPass: false);

            _tally.OnInspectionResult(kit);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.Rejecting));
            Assert.That(_tally.RejectedCount, Is.EqualTo(1));

            _tally.OnKitRejected(kit);

            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.Rejected));
            Assert.That(_tally.RejectedCount, Is.EqualTo(1), "판정과 밀어냄 신호가 다 와도 한 번만 센다");
            Assert.That(_tally.CompletedCount, Is.EqualTo(0));
        }

        [Test]
        public void ARejectSignalWithoutAVerdictSignal_StillCountsOnce()
        {
            _tally.OnKitRejected(Kit(1));

            Assert.That(_tally.RejectedCount, Is.EqualTo(1));
        }

        [Test]
        public void TwoPassesAndOneFail_GiveTwoCompletedAndOneRejected()
        {
            KitData first = Judged(1, isPass: true);
            KitData second = Judged(2, isPass: false);
            KitData third = Judged(3, isPass: true);

            _tally.OnInspectionResult(first);
            _tally.OnAtConveyorEnd(first);
            _tally.OnInspectionResult(second);
            _tally.OnKitRejected(second);
            _tally.OnInspectionResult(third);
            _tally.OnAtConveyorEnd(third);

            Assert.That(_tally.CompletedCount, Is.EqualTo(2));
            Assert.That(_tally.RejectedCount, Is.EqualTo(1));
        }

        [Test]
        public void AnInspectionResultWithoutAVerdict_ChangesNothing()
        {
            _tally.OnInspectionResult(Kit(1));

            Assert.That(_tally.CurrentKit, Is.Null);
            Assert.That(_tally.RejectedCount, Is.EqualTo(0));
        }

        [Test]
        public void ALateSignalOfAnEarlierKit_CountsButDoesNotMoveTheDisplayBack()
        {
            KitData first = Judged(1, isPass: true);
            KitData second = Judged(2, isPass: true);
            _tally.OnKitReady(first);
            _tally.OnKitReady(second);

            _tally.OnAtConveyorEnd(first);

            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
            Assert.That(_tally.CurrentKit, Is.SameAs(second));
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.ToInspection));
        }

        [Test]
        public void ANewerKit_TakesOverTheDisplay()
        {
            _tally.OnAtConveyorEnd(Judged(1, isPass: true));
            KitData second = Judged(2, isPass: true);

            _tally.OnKitReady(second);

            Assert.That(_tally.CurrentKit, Is.SameAs(second));
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.ToInspection));
        }

        [Test]
        public void EachStockEmptySignal_IsCounted()
        {
            _tally.OnStockEmpty();
            _tally.OnStockEmpty();

            Assert.That(_tally.StockShortageCount, Is.EqualTo(2));
        }

        [Test]
        public void EnteringFault_IsCountedOncePerEntry()
        {
            _tally.OnStationStateChanged(StationId.Conveyor, StationState.Fault);
            _tally.OnStationStateChanged(StationId.Conveyor, StationState.Fault);
            Assert.That(_tally.FaultCount, Is.EqualTo(1), "이미 정지인 설비가 다시 알려도 늘지 않는다");

            _tally.OnStationStateChanged(StationId.Conveyor, StationState.Running);
            _tally.OnStationStateChanged(StationId.Conveyor, StationState.Fault);

            Assert.That(_tally.FaultCount, Is.EqualTo(2));
        }

        [Test]
        public void FaultsOfDifferentStations_AreCountedSeparately()
        {
            _tally.OnStationStateChanged(StationId.ArmA, StationState.Fault);
            _tally.OnStationStateChanged(StationId.ArmB, StationState.Fault);

            Assert.That(_tally.FaultCount, Is.EqualTo(2));
            Assert.That(_tally.StateOf(StationId.ArmA), Is.EqualTo(StationState.Fault));
            Assert.That(_tally.StateOf(StationId.Inspection), Is.EqualTo(StationState.Idle));
        }

        [Test]
        public void GoingIdleOrRunning_IsNotAFault()
        {
            _tally.OnStationStateChanged(StationId.ArmA, StationState.Running);
            _tally.OnStationStateChanged(StationId.ArmA, StationState.Idle);

            Assert.That(_tally.FaultCount, Is.EqualTo(0));
        }

        [Test]
        public void TheLastWarning_ShowsTheCodeAndTheMessage_AndTheNewestWins()
        {
            _tally.OnWarning("SENSOR_TIMEOUT", "구역 센서 10초 미감지");
            Assert.That(_tally.LastWarning, Is.EqualTo("SENSOR_TIMEOUT: 구역 센서 10초 미감지"));

            _tally.OnWarning(string.Empty, "다른 경고");

            Assert.That(_tally.LastWarning, Is.EqualTo("다른 경고"));
        }

        [Test]
        public void ANullKit_IsIgnoredByEverySignal()
        {
            Assert.DoesNotThrow(() =>
            {
                _tally.OnKitReady(null);
                _tally.OnAtInspection(null);
                _tally.OnInspectionResult(null);
                _tally.OnKitRejected(null);
                _tally.OnAtPacking(null);
                _tally.OnLidClosed(null);
                _tally.OnAtConveyorEnd(null);
                _tally.OnKitShipped(null);
            });
            Assert.That(_tally.CurrentKit, Is.Null);
            Assert.That(_tally.CompletedCount, Is.EqualTo(0));
        }
    }
}
