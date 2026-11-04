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
        public void TheVersion_RisesOnlyWhenWhatTheScreenShowsChanges()
        {
            int start = _tally.Version;

            _tally.OnKitReady(Kit(1));
            int afterKit = _tally.Version;
            _tally.OnStockEmpty();
            int afterStock = _tally.Version;
            _tally.OnStationStateChanged(StationId.ArmA, StationState.Running);
            int afterStation = _tally.Version;
            _tally.OnWarning("W", "경고");
            int afterWarning = _tally.Version;

            Assert.That(afterKit, Is.GreaterThan(start));
            Assert.That(afterStock, Is.GreaterThan(afterKit));
            Assert.That(afterStation, Is.GreaterThan(afterStock));
            Assert.That(afterWarning, Is.GreaterThan(afterStation));
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

        // 주문을 다시 적용하면 키트 순번이 1부터 다시 시작한다. 같은 순번의 다른 키트를 시작 시각으로 구분한다.
        private static KitData Started(int sequence, float startTime, bool isPass = true)
        {
            KitData kit = Judged(sequence, isPass);
            kit.StartTime = startTime;
            return kit;
        }

        [Test]
        public void TheSameSequenceInANewOrder_IsAnotherKit_AndTheCountsKeepAddingUp()
        {
            KitData firstOrderKit = Started(1, startTime: 10f);
            _tally.OnKitReady(firstOrderKit);
            _tally.OnAtConveyorEnd(firstOrderKit);
            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.Done));

            _tally.OnOrdersApplied();
            KitData secondOrderKit = Started(1, startTime: 400f);
            _tally.OnKitReady(secondOrderKit);
            _tally.OnAtConveyorEnd(secondOrderKit);

            Assert.That(_tally.CompletedCount, Is.EqualTo(2), "순번이 같아도 다른 키트이므로 지난 주문의 센 값에 이어서 센다");
            Assert.That(_tally.CurrentKit, Is.SameAs(secondOrderKit));
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.Done));
        }

        [Test]
        public void TheSameKitSeenAgain_IsStillCountedOnce()
        {
            KitData kit = Started(1, startTime: 10f);

            _tally.OnAtConveyorEnd(kit);
            _tally.OnOrdersApplied();
            _tally.OnKitShipped(kit);

            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
        }

        [Test]
        public void WhenNewOrdersAreApplied_OnlyTheHeadlineGoesBackToTheStart()
        {
            KitData done = Started(1, startTime: 10f);
            KitData rejected = Started(2, startTime: 20f, isPass: false);
            _tally.OnAtConveyorEnd(done);
            _tally.OnInspectionResult(rejected);
            _tally.OnKitRejected(rejected);
            _tally.OnStockEmpty();
            _tally.OnStationStateChanged(StationId.Conveyor, StationState.Fault);
            _tally.OnWarning("W", "경고");

            _tally.OnOrdersApplied();

            Assert.That(_tally.CurrentKit, Is.Null);
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.None));
            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
            Assert.That(_tally.RejectedCount, Is.EqualTo(1));
            Assert.That(_tally.StockShortageCount, Is.EqualTo(1));
            Assert.That(_tally.FaultCount, Is.EqualTo(1));
            Assert.That(_tally.LastWarning, Is.EqualTo("W: 경고"));
            Assert.That(_tally.StateOf(StationId.Conveyor), Is.EqualTo(StationState.Fault), "설비 상태는 주문과 상관없이 지금 상태 그대로다");
        }

        [Test]
        public void ApplyingOrdersWhileNothingIsShown_DoesNotRaiseTheVersion()
        {
            int before = _tally.Version;

            _tally.OnOrdersApplied();

            Assert.That(_tally.Version, Is.EqualTo(before), "화면이 바뀌지 않았으면 다시 그리지 않는다");
        }

        [Test]
        public void ApplyingOrdersAfterAKit_RaisesTheVersionSoTheScreenRedraws()
        {
            _tally.OnAtConveyorEnd(Started(1, startTime: 10f));
            int before = _tally.Version;

            _tally.OnOrdersApplied();

            Assert.That(_tally.Version, Is.GreaterThan(before));
        }

        [Test]
        public void AKitOfANewOrder_TakesOverTheDisplayEvenWithALowerSequence()
        {
            KitData old = Started(3, startTime: 100f);
            _tally.OnKitReady(old);

            // 주문을 적용하는 순간 막 끝나던 지난 주문의 키트가 아직 화면에 있는 경우도 새 주문의 키트가 이어받는다.
            KitData fresh = Started(1, startTime: 300f);
            _tally.OnKitReady(fresh);

            Assert.That(_tally.CurrentKit, Is.SameAs(fresh));
        }

        [Test]
        public void ALateSignalOfTheOldOrdersKit_CountsButDoesNotMoveTheDisplayBack()
        {
            KitData old = Started(3, startTime: 100f);
            _tally.OnKitReady(old);
            _tally.OnOrdersApplied();
            KitData fresh = Started(1, startTime: 300f);
            _tally.OnKitReady(fresh);

            _tally.OnAtConveyorEnd(old);

            Assert.That(_tally.CompletedCount, Is.EqualTo(1));
            Assert.That(_tally.CurrentKit, Is.SameAs(fresh));
            Assert.That(_tally.CurrentStage, Is.EqualTo(ProcessStage.ToInspection));
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
