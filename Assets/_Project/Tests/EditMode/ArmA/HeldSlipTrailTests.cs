using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class HeldSlipTrailTests
    {
        private static readonly Pose Tool = new Pose(Vector3.zero, Quaternion.identity);

        private static HeldItemFrame Held(Vector3 center, Quaternion rotation)
        {
            return HeldItemFrame.Measure(Tool, center, rotation);
        }

        private static HeldItemFrame HeldAt(float x)
        {
            return Held(new Vector3(x, 0f, 0.04f), Quaternion.identity);
        }

        [Test]
        public void ASteadyGrip_ReportsNoSlip_AtEveryPoint()
        {
            var trail = new HeldSlipTrail(HeldAt(0f));

            SlipMark first = trail.Mark(HeldAt(0f));
            SlipMark second = trail.Mark(HeldAt(0f));

            Assert.That(first.SinceLastInTool.magnitude, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(second.SinceFirstInTool.magnitude, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(second.SinceFirstDeg, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void TheSecondMark_ComparesWithThePreviousPoint_AndWithTheFirst()
        {
            var trail = new HeldSlipTrail(HeldAt(0f));

            trail.Mark(HeldAt(0.02f));
            SlipMark second = trail.Mark(HeldAt(0.03f));

            Assert.That(second.SinceLastInTool.x, Is.EqualTo(0.01f).Within(1e-5f), "직전 지점(2cm)에서 1cm 더 밀렸다");
            Assert.That(second.SinceFirstInTool.x, Is.EqualTo(0.03f).Within(1e-5f), "처음(0)에서는 3cm 밀렸다");
        }

        [Test]
        public void ALaterPointThatDidNotSlip_ReportsZeroSinceTheLastButKeepsTheTotal()
        {
            var trail = new HeldSlipTrail(HeldAt(0f));

            trail.Mark(HeldAt(0.04f));
            SlipMark held = trail.Mark(HeldAt(0.04f));

            Assert.That(held.SinceLastInTool.magnitude, Is.EqualTo(0f).Within(1e-6f), "이 구간에서는 미끄러지지 않았다");
            Assert.That(held.SinceFirstInTool.magnitude, Is.EqualTo(0.04f).Within(1e-5f), "앞 구간에서 미끄러진 것은 합계에 남는다");
        }

        [Test]
        public void TheTurnIsMeasuredInTheToolFrame()
        {
            var trail = new HeldSlipTrail(Held(new Vector3(0f, 0f, 0.04f), Quaternion.identity));

            SlipMark mark = trail.Mark(Held(new Vector3(0f, 0f, 0.04f), Quaternion.Euler(0f, 0f, 25f)));

            Assert.That(mark.SinceLastDeg, Is.EqualTo(25f).Within(1e-3f));
            Assert.That(mark.SinceFirstDeg, Is.EqualTo(25f).Within(1e-3f));
        }

        [Test]
        public void MovingTheWholeToolTogetherWithTheItem_IsNotSlip()
        {
            var first = HeldItemFrame.Measure(Tool, new Vector3(0f, 0f, 0.04f), Quaternion.identity);
            var movedTool = new Pose(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 90f, 0f));
            var movedFrame = HeldItemFrame.Measure(
                movedTool, movedTool.position + movedTool.rotation * new Vector3(0f, 0f, 0.04f), movedTool.rotation);
            var trail = new HeldSlipTrail(first);

            SlipMark mark = trail.Mark(movedFrame);

            Assert.That(mark.SinceFirstInTool.magnitude, Is.EqualTo(0f).Within(1e-5f), "도구와 함께 움직인 것은 미끄러짐이 아니다");
            Assert.That(mark.SinceFirstDeg, Is.EqualTo(0f).Within(1e-3f));
        }
    }
}
