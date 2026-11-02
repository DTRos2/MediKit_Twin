using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BoxPlaceDiagnosticsBaseMotionTests
    {
        private const float Step = 0.02f;
        private static readonly Pose Tool = new Pose(Vector3.zero, Quaternion.identity);

        private static BoxPlaceDiagnostics Create()
        {
            return new BoxPlaceDiagnostics(pose => new IkResult(true, 0f, 0f, 1));
        }

        private static HeldItemFrame HeldAt(float xMeters)
        {
            return HeldItemFrame.Measure(Tool, new Vector3(xMeters, 0f, 0.04f), Quaternion.identity);
        }

        // 스텝마다 (베이스 x, 방향, 물건 x)를 받아 요약을 만든다.
        private static BaseMotionSummary Summary(float onsetSlipM, params (float baseX, float yaw, float heldX)[] steps)
        {
            var trace = new BaseMotionTrace(onsetSlipM);
            for (int i = 0; i < steps.Length; i++)
            {
                trace.Add(new BaseMotionSample(i * Step, new Vector3(steps[i].baseX, 0f, 0f), steps[i].yaw, HeldAt(steps[i].heldX)));
            }

            return trace.Summarize();
        }

        [Test]
        public void TheSummary_NamesTheMove_TheEnding_AndTheDistance()
        {
            BaseMotionSummary summary = Summary(0.02f, (0f, 0f, 0f), (0.024f, 0f, 0f), (0.048f, 0f, 0f));

            string arrived = Create().DescribeBaseMotion("3번째", summary, arrived: true, 0.02f);
            string aborted = Create().DescribeBaseMotion("3번째", summary, arrived: false, 0.02f);

            Assert.That(arrived, Does.StartWith("베이스 이동 [3번째] 도착, 0.04초 동안 4.8cm 이동 0도 회전"));
            Assert.That(arrived, Does.Contain("최고 1.20m/s"));
            Assert.That(aborted, Does.Contain("[3번째] 도착하지 못하고 끝남"));
        }

        [Test]
        public void TheSummary_ReportsTheSlipDirectionInToolAxes()
        {
            BaseMotionSummary summary = Summary(0.05f, (0f, 0f, 0f), (0.024f, 0f, -0.03f));

            string text = Create().DescribeBaseMotion("1번째", summary, true, 0.05f);

            Assert.That(text, Does.Contain("밀림 최대 3.0cm, 끝 3.0cm 도구 기준 (-3.0, 0.0, 0.0)cm"));
        }

        [Test]
        public void WhenTheSlipPassesTheThresholdWhileMoving_SaysSoWithTheSpeed()
        {
            BaseMotionSummary summary = Summary(0.02f, (0f, 0f, 0f), (0.024f, 0f, -0.005f), (0.048f, 0f, -0.025f));

            string text = Create().DescribeBaseMotion("1번째", summary, true, 0.02f);

            Assert.That(text, Does.Contain("밀림이 2.0cm를 처음 넘은 때는 이동 시작 0.04초 뒤, 베이스가 움직이는 중(속도 1.20m/s"));
            Assert.That(text, Does.Contain("그때까지 4.8cm 이동"));
        }

        [Test]
        public void WhenTheSlipPassesTheThresholdAfterTheStop_SaysHowManyStepsLater()
        {
            BaseMotionSummary summary = Summary(
                0.02f, (0f, 0f, 0f), (0.024f, 0f, 0f), (0.024f, 0f, -0.005f), (0.024f, 0f, -0.03f));

            string text = Create().DescribeBaseMotion("1번째", summary, true, 0.02f);

            Assert.That(text, Does.Contain("베이스가 멈추고 2스텝 뒤"));
        }

        [Test]
        public void WhenTheSlipPassesTheThresholdBeforeTheBaseMoves_SaysSo()
        {
            BaseMotionSummary summary = Summary(0.02f, (0f, 0f, 0f), (0f, 0f, -0.03f));

            string text = Create().DescribeBaseMotion("1번째", summary, true, 0.02f);

            Assert.That(text, Does.Contain("베이스가 움직이기 전"));
        }

        [Test]
        public void WhenTheSlipStaysUnderTheThreshold_SaysItDidNotPassIt()
        {
            BaseMotionSummary summary = Summary(0.02f, (0f, 0f, 0f), (0.024f, 0f, -0.005f));

            string text = Create().DescribeBaseMotion("1번째", summary, true, 0.02f);

            Assert.That(text, Does.Contain("밀림이 2.0cm를 넘지 않았다."));
        }

        [Test]
        public void WhenTheBaseDoesNotMove_SaysSoInsteadOfSpeeds()
        {
            BaseMotionSummary summary = Summary(0.02f, (1f, 0f, 0f), (1f, 0f, 0f));

            string text = Create().DescribeBaseMotion("2번째", summary, true, 0.02f);

            Assert.That(text, Does.Contain("베이스는 움직이지 않았다(0.0cm, 0.0도)"));
            Assert.That(text, Does.Not.Contain("최고"));
        }

        [Test]
        public void TheSlipAtTheLastMotion_IsShownOnlyWhenTheBaseMoved()
        {
            BaseMotionSummary moved = Summary(0.02f, (0f, 0f, 0f), (0.024f, 0f, -0.01f), (0.024f, 0f, -0.03f));
            BaseMotionSummary still = Summary(0.02f, (0f, 0f, 0f), (0f, 0f, -0.03f));

            Assert.That(Create().DescribeBaseMotion("1번째", moved, true, 0.02f), Does.Contain("베이스가 마지막으로 움직인 때 1.0cm"));
            Assert.That(Create().DescribeBaseMotion("1번째", still, true, 0.02f), Does.Not.Contain("마지막으로 움직인"));
        }

        [Test]
        public void TheTrace_ListsSpeedsAndSlipsStepByStep()
        {
            BaseMotionSummary summary = Summary(0.02f, (0f, 0f, 0f), (0.024f, 0f, -0.01f), (0.048f, 0f, -0.03f));

            string text = Create().DescribeBaseMotionTrace("1번째", summary);

            Assert.That(text, Does.StartWith("베이스 이동 [1번째] 추이(3스텝, 간격 0.020초) "));
            Assert.That(text, Does.Contain("속도(m/s) [0.00 1.20 1.20]"));
            Assert.That(text, Does.Contain("밀림(cm) [0.0 1.0 3.0]"));
            Assert.That(text, Does.Not.Contain("회전"), "돌지 않은 이동은 회전 속도를 적지 않는다");
        }

        [Test]
        public void TheTrace_AddsTheTurnRate_WhenTheBaseTurned()
        {
            BaseMotionSummary summary = Summary(0.02f, (0f, 0f, 0f), (0f, 3.6f, 0f), (0f, 7.2f, 0f));

            string text = Create().DescribeBaseMotionTrace("1번째", summary);

            Assert.That(text, Does.Contain("회전(도/초) [0 180 180]"));
        }

        [Test]
        public void ALongTrace_IsThinnedOut_ButKeepsTheLastStep()
        {
            // 251스텝이면 간격 3으로 뽑을 때 마지막(250번)이 간격에 걸리지 않아, 마지막 값을 따로 넣는 쪽을 시험한다.
            var steps = new (float, float, float)[251];
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = (i * 0.001f, 0f, i == steps.Length - 1 ? -0.05f : 0f);
            }

            BaseMotionSummary summary = Summary(0.02f, steps);
            string text = Create().DescribeBaseMotionTrace("1번째", summary);

            Assert.That(text, Does.Contain("251스텝"));
            Assert.That(text, Does.Contain("3스텝마다"));
            Assert.That(text, Does.EndWith("5.0]"), "건너뛰어도 마지막 값은 적는다");
            string slipList = text.Substring(text.IndexOf("밀림(cm) [") + "밀림(cm) [".Length);
            Assert.That(slipList.Split(' ').Length, Is.LessThanOrEqualTo(101));
        }
    }
}
