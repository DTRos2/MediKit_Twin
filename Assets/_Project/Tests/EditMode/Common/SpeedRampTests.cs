using System;
using NUnit.Framework;

namespace MediKitTwin.Common.Tests
{
    public sealed class SpeedRampTests
    {
        private const float Dt = 0.02f;

        [Test]
        public void FirstFrame_DoesNotJumpToTopSpeed()
        {
            var ramp = new SpeedRamp(1.2f, 2f);

            float travel = ramp.Advance(10f, Dt);

            // 가속도 2m/s²로 한 프레임이면 0.04m/s. 계단처럼 1.2m/s가 되지 않는다.
            Assert.That(ramp.Speed, Is.EqualTo(0.04f).Within(1e-5f));
            Assert.That(travel, Is.EqualTo(0.04f * Dt).Within(1e-6f));
        }

        [Test]
        public void SpeedChangePerFrame_NeverExceedsTheAcceleration()
        {
            var ramp = new SpeedRamp(1.2f, 2f);
            float remaining = 2f;
            float previous = 0f;

            for (int i = 0; i < 300 && remaining > 0f; i++)
            {
                remaining -= ramp.Advance(remaining, Dt);
                Assert.That(Math.Abs(ramp.Speed - previous), Is.LessThanOrEqualTo(2f * Dt + 1e-5f), $"프레임 {i}");
                previous = ramp.Speed;
            }
        }

        [Test]
        public void ReachesTopSpeed_ThenSlowsToStopAtTheTarget()
        {
            var ramp = new SpeedRamp(1.2f, 2f);
            float remaining = 5f;
            float peak = 0f;

            for (int i = 0; i < 1000 && remaining > 0f; i++)
            {
                remaining -= ramp.Advance(remaining, Dt);
                peak = Math.Max(peak, ramp.Speed);
            }

            Assert.That(peak, Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(remaining, Is.EqualTo(0f).Within(1e-5f), "목표를 넘지도 모자라지도 않는다");
        }

        [Test]
        public void ShortTrip_NeverReachesTopSpeed()
        {
            var ramp = new SpeedRamp(1.2f, 2f);
            float remaining = 0.3f;
            float peak = 0f;

            for (int i = 0; i < 1000 && remaining > 0f; i++)
            {
                remaining -= ramp.Advance(remaining, Dt);
                peak = Math.Max(peak, ramp.Speed);
            }

            // 0.3m 안에서 멈추려면 속도는 √(2·2·0.3) = 1.1m/s를 넘지 못한다.
            Assert.That(peak, Is.LessThan(1.2f));
            Assert.That(remaining, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Travel_NeverExceedsTheRemainingDistance()
        {
            var ramp = new SpeedRamp(1.2f, 2f);

            Assert.That(ramp.Advance(0.0001f, Dt), Is.LessThanOrEqualTo(0.0001f));
        }

        [Test]
        public void StoppingPoint_IsTheSameForDifferentFrameIntervals()
        {
            // 프레임이 길어도 멈추는 자리는 남은 거리로 정해진다. 에디터와 시험의 프레임 간격이 달라도 서는 자리가 같다.
            Assert.That(DistanceTravelled(0.02f), Is.EqualTo(3f).Within(1e-4f));
            Assert.That(DistanceTravelled(0.1f), Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void ZeroAcceleration_GoesStraightToTopSpeed()
        {
            var ramp = new SpeedRamp(1.2f, 0f);

            float travel = ramp.Advance(10f, Dt);

            Assert.That(ramp.Speed, Is.EqualTo(1.2f));
            Assert.That(travel, Is.EqualTo(1.2f * Dt).Within(1e-6f));
        }

        [Test]
        public void NothingRemaining_StopsAndTravelsNothing()
        {
            var ramp = new SpeedRamp(1.2f, 2f);
            ramp.Advance(10f, Dt);

            float travel = ramp.Advance(0f, Dt);

            Assert.That(travel, Is.EqualTo(0f));
            Assert.That(ramp.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_StopsTheRamp()
        {
            var ramp = new SpeedRamp(1.2f, 2f);
            ramp.Advance(10f, Dt);

            ramp.Reset();

            Assert.That(ramp.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void InvalidSettings_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpeedRamp(0f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpeedRamp(1.2f, -1f));
        }

        private static float DistanceTravelled(float deltaTime)
        {
            var ramp = new SpeedRamp(1.2f, 2f);
            float target = 3f;
            float remaining = target;
            for (int i = 0; i < 100000 && remaining > 0f; i++)
            {
                remaining -= ramp.Advance(remaining, deltaTime);
            }

            return target - remaining;
        }
    }
}
