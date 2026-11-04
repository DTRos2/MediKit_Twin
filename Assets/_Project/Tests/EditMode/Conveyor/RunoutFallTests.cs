using NUnit.Framework;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class RunoutFallTests
    {
        private const float Gravity = -9.81f;
        private const float StartHeight = 0.96f;
        private const float LandingHeight = 0.90f;
        private const float FrameSeconds = 0.02f;

        [Test]
        public void Step_MovesDownward_AndGainsSpeed_BeforeLanding()
        {
            float speed = 0f;

            float height = RunoutFall.Step(StartHeight, ref speed, LandingHeight, Gravity, FrameSeconds);

            Assert.That(height, Is.LessThan(StartHeight));
            Assert.That(speed, Is.EqualTo(Gravity * FrameSeconds).Within(1e-5f));
        }

        [Test]
        public void Step_LandsExactlyOnTheLandingHeight_AndNeverGoesBelow()
        {
            float speed = 0f;
            float height = StartHeight;

            for (int frame = 0; frame < 100 && height > LandingHeight; frame++)
            {
                height = RunoutFall.Step(height, ref speed, LandingHeight, Gravity, FrameSeconds);
                Assert.That(height, Is.GreaterThanOrEqualTo(LandingHeight));
            }

            Assert.That(height, Is.EqualTo(LandingHeight));
            Assert.That(speed, Is.EqualTo(0f));
        }

        [Test]
        public void Step_DoesNotPassThrough_WhenTheFrameIsLong()
        {
            float speed = 0f;

            float height = RunoutFall.Step(StartHeight, ref speed, LandingHeight, Gravity, 1f);

            Assert.That(height, Is.EqualTo(LandingHeight));
        }

        [Test]
        public void Step_TakesLongerWhenGravityIsWeaker()
        {
            Assert.That(FramesToLand(Gravity), Is.LessThan(FramesToLand(Gravity * 0.25f)));
        }

        private static int FramesToLand(float gravity)
        {
            float speed = 0f;
            float height = StartHeight;
            int frames = 0;
            while (height > LandingHeight && frames < 10000)
            {
                height = RunoutFall.Step(height, ref speed, LandingHeight, gravity, FrameSeconds);
                frames++;
            }

            return frames;
        }
    }
}
