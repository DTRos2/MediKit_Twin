using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BaseMotionTraceTests
    {
        private const float Step = 0.02f;
        private static readonly Pose Tool = new Pose(Vector3.zero, Quaternion.identity);

        // 도구 앞(+z)에 4cm, 옆(x)으로 xMeters 떨어진 곳에 물건 중심이 있는 쥔 자세.
        private static HeldItemFrame HeldAt(float xMeters)
        {
            return HeldItemFrame.Measure(Tool, new Vector3(xMeters, 0f, 0.04f), Quaternion.identity);
        }

        private static BaseMotionSample Sample(int index, float baseX, float yawDeg, HeldItemFrame? held)
        {
            return new BaseMotionSample(index * Step, new Vector3(baseX, 0.1f, 0f), yawDeg, held);
        }

        [Test]
        public void StraightDrive_ReportsDistanceAndTopSpeed()
        {
            var trace = new BaseMotionTrace(0.02f);
            // 스텝마다 2.4cm: 1.2m/s.
            for (int i = 0; i < 5; i++)
            {
                trace.Add(Sample(i, i * 0.024f, 0f, HeldAt(0f)));
            }

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.SampleCount, Is.EqualTo(5));
            Assert.That(summary.DistanceM, Is.EqualTo(0.096f).Within(1e-5f));
            Assert.That(summary.MaxSpeedMps, Is.EqualTo(1.2f).Within(1e-3f));
            Assert.That(summary.DurationSeconds, Is.EqualTo(4 * Step).Within(1e-6f));
            Assert.That(summary.StepSeconds, Is.EqualTo(Step).Within(1e-6f));
            Assert.That(summary.HasMotion, Is.True);
            Assert.That(summary.SpeedsMps[0], Is.EqualTo(0f));
        }

        [Test]
        public void HeightChangesAreNotCountedAsBaseMotion()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(new BaseMotionSample(0f, new Vector3(0f, 0.0f, 0f), 0f, null));
            trace.Add(new BaseMotionSample(Step, new Vector3(0f, 0.5f, 0f), 0f, null));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.DistanceM, Is.EqualTo(0f));
            Assert.That(summary.HasMotion, Is.False);
        }

        [Test]
        public void TurningInPlace_CountsTheAngleTurned_AndTreatsItAsMotion()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, null));
            trace.Add(Sample(1, 0f, 3.6f, null));
            trace.Add(Sample(2, 0f, 7.2f, null));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.YawTravelDeg, Is.EqualTo(7.2f).Within(1e-4f));
            Assert.That(summary.MaxYawRateDegPerSec, Is.EqualTo(180f).Within(0.1f));
            Assert.That(summary.DistanceM, Is.EqualTo(0f));
            Assert.That(summary.HasMotion, Is.True);
        }

        [Test]
        public void TurningAcrossTheWrapAround_IsASmallTurn()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 359f, null));
            trace.Add(Sample(1, 0f, 1f, null));

            Assert.That(trace.Summarize().YawTravelDeg, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void ABaseThatStaysPut_HasNoMotion()
        {
            var trace = new BaseMotionTrace(0.02f);
            for (int i = 0; i < 4; i++)
            {
                trace.Add(Sample(i, 1f, 90f, HeldAt(0f)));
            }

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.HasMotion, Is.False);
            Assert.That(summary.DistanceM, Is.EqualTo(0f));
            Assert.That(summary.MaxSlipM, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(summary.HasOnset, Is.False);
            Assert.That(summary.OnsetStepsSinceMotion, Is.EqualTo(-1));
        }

        [Test]
        public void SlipIsMeasuredAgainstTheFirstHeldFrame()
        {
            var trace = new BaseMotionTrace(0.05f);
            trace.Add(Sample(0, 0f, 0f, HeldAt(0f)));
            trace.Add(Sample(1, 0f, 0f, HeldAt(-0.01f)));
            trace.Add(Sample(2, 0f, 0f, HeldAt(-0.03f)));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.SlipsM[0], Is.EqualTo(0f).Within(1e-6f));
            Assert.That(summary.SlipsM[1], Is.EqualTo(0.01f).Within(1e-5f));
            Assert.That(summary.SlipAtEndM, Is.EqualTo(0.03f).Within(1e-5f));
            Assert.That(summary.MaxSlipM, Is.EqualTo(0.03f).Within(1e-5f));
            Assert.That(summary.SlipAtEndInTool.x, Is.EqualTo(-0.03f).Within(1e-5f), "밀린 방향은 도구 기준 부호를 지킨다");
        }

        [Test]
        public void TheOnset_IsTheFirstStepWhereSlipReachesTheThreshold()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, HeldAt(0f)));
            trace.Add(Sample(1, 0.024f, 0f, HeldAt(-0.005f)));
            trace.Add(Sample(2, 0.048f, 0f, HeldAt(-0.021f)));
            trace.Add(Sample(3, 0.072f, 0f, HeldAt(-0.040f)));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.HasOnset, Is.True);
            Assert.That(summary.OnsetSeconds, Is.EqualTo(2 * Step).Within(1e-6f));
            Assert.That(summary.OnsetSpeedMps, Is.EqualTo(1.2f).Within(1e-3f));
            Assert.That(summary.OnsetDistanceM, Is.EqualTo(0.048f).Within(1e-5f));
            Assert.That(summary.OnsetWhileMoving, Is.True);
            Assert.That(summary.OnsetStepsSinceMotion, Is.EqualTo(0));
        }

        [Test]
        public void SlipThatAppearsOnlyAfterTheBaseStops_IsReportedAsAfterMotion()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, HeldAt(0f)));
            trace.Add(Sample(1, 0.024f, 0f, HeldAt(0f)));
            trace.Add(Sample(2, 0.048f, 0f, HeldAt(-0.002f)));
            // 여기서 멈춘다.
            trace.Add(Sample(3, 0.048f, 0f, HeldAt(-0.010f)));
            trace.Add(Sample(4, 0.048f, 0f, HeldAt(-0.030f)));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.HasOnset, Is.True);
            Assert.That(summary.OnsetWhileMoving, Is.False);
            Assert.That(summary.OnsetStepsSinceMotion, Is.EqualTo(2), "마지막으로 움직인 스텝(2) 뒤 두 스텝");
            Assert.That(summary.SlipAtLastMotionM, Is.EqualTo(0.002f).Within(1e-5f));
            Assert.That(summary.SlipAtEndM, Is.EqualTo(0.030f).Within(1e-5f));
        }

        [Test]
        public void SlipThatAppearsBeforeTheBaseEverMoves_IsReportedAsBeforeMotion()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, HeldAt(0f)));
            trace.Add(Sample(1, 0f, 0f, HeldAt(-0.03f)));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.HasOnset, Is.True);
            Assert.That(summary.OnsetStepsSinceMotion, Is.EqualTo(-1));
            Assert.That(summary.OnsetWhileMoving, Is.False);
        }

        [Test]
        public void AStepWithoutAHeldFrame_KeepsThePreviousSlip()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, HeldAt(0f)));
            trace.Add(Sample(1, 0f, 0f, HeldAt(-0.03f)));
            trace.Add(Sample(2, 0f, 0f, null));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.SlipsM[2], Is.EqualTo(0.03f).Within(1e-5f));
            Assert.That(summary.SlipAtEndM, Is.EqualTo(0.03f).Within(1e-5f));
        }

        [Test]
        public void WithoutAnyHeldFrame_ThereIsNoSlipAndNoException()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, null));
            trace.Add(Sample(1, 0.024f, 0f, null));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.MaxSlipM, Is.EqualTo(0f));
            Assert.That(summary.HasOnset, Is.False);
            Assert.That(summary.HasMotion, Is.True);
        }

        [Test]
        public void AFirstStepWithoutAHeldFrame_UsesTheFirstMeasuredFrameAsTheReference()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, null));
            trace.Add(Sample(1, 0f, 0f, HeldAt(0f)));
            trace.Add(Sample(2, 0f, 0f, HeldAt(-0.03f)));

            Assert.That(trace.Summarize().SlipAtEndM, Is.EqualTo(0.03f).Within(1e-5f));
        }

        [Test]
        public void ASingleSample_DoesNotDivideByZero()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, HeldAt(0f)));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.SampleCount, Is.EqualTo(1));
            Assert.That(summary.DurationSeconds, Is.EqualTo(0f));
            Assert.That(summary.StepSeconds, Is.EqualTo(0f));
            Assert.That(summary.MaxSpeedMps, Is.EqualTo(0f));
        }

        [Test]
        public void AnEmptyTrace_SummarizesToZeros()
        {
            BaseMotionSummary summary = new BaseMotionTrace(0.02f).Summarize();

            Assert.That(summary.SampleCount, Is.EqualTo(0));
            Assert.That(summary.HasMotion, Is.False);
            Assert.That(summary.HasOnset, Is.False);
            Assert.That(summary.SpeedsMps, Is.Empty);
        }

        [Test]
        public void TwoStepsAtTheSameTime_CountTheDistanceButHaveNoSpeed()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(new BaseMotionSample(1f, new Vector3(0f, 0f, 0f), 0f, null));
            trace.Add(new BaseMotionSample(1f, new Vector3(0.05f, 0f, 0f), 0f, null));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.DistanceM, Is.EqualTo(0.05f).Within(1e-5f));
            Assert.That(summary.SpeedsMps[1], Is.EqualTo(0f));
            Assert.That(summary.HasMotion, Is.False);
        }

        [Test]
        public void ARotationOfTheItemAlone_DoesNotCountAsCenterSlipButIsReportedInDegrees()
        {
            var trace = new BaseMotionTrace(0.02f);
            trace.Add(Sample(0, 0f, 0f, HeldItemFrame.Measure(Tool, new Vector3(0f, 0f, 0.04f), Quaternion.identity)));
            trace.Add(Sample(1, 0f, 0f, HeldItemFrame.Measure(Tool, new Vector3(0f, 0f, 0.04f), Quaternion.Euler(0f, 0f, 20f))));

            BaseMotionSummary summary = trace.Summarize();

            Assert.That(summary.SlipAtEndM, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(summary.SlipAtEndDeg, Is.EqualTo(20f).Within(1e-3f));
        }

        [Test]
        public void AHigherStillSpeed_TreatsASlowDriftAsStanding()
        {
            // 스텝마다 0.96mm: 0.048m/s. 기본 기준(0.02m/s)이면 움직임이고 0.5m/s 기준이면 서 있는 것이다.
            var slow = new BaseMotionTrace(0.02f, stillSpeedMps: 0.5f);
            var normal = new BaseMotionTrace(0.02f);
            for (int i = 0; i < 4; i++)
            {
                slow.Add(Sample(i, i * 0.00096f, 0f, null));
                normal.Add(Sample(i, i * 0.00096f, 0f, null));
            }

            Assert.That(normal.Summarize().HasMotion, Is.True);
            Assert.That(slow.Summarize().HasMotion, Is.False);
        }

        [Test]
        public void AHigherStillYawRate_TreatsASlowTurnAsStanding()
        {
            // 스텝마다 0.2도: 10도/초. 기본 기준(2도/초)이면 도는 것이고 20도/초 기준이면 서 있는 것이다.
            var slow = new BaseMotionTrace(0.02f, stillYawRateDegPerSec: 20f);
            var normal = new BaseMotionTrace(0.02f);
            for (int i = 0; i < 4; i++)
            {
                slow.Add(Sample(i, 0f, i * 0.2f, null));
                normal.Add(Sample(i, 0f, i * 0.2f, null));
            }

            Assert.That(normal.Summarize().HasMotion, Is.True);
            Assert.That(slow.Summarize().HasMotion, Is.False);
        }
    }
}
