using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class JointTravelTests
    {
        [Test]
        public void Largest_IsTheBiggestAbsoluteChange_AmongTheJoints()
        {
            float largest = JointTravel.Largest(new[] { 0f, 1f, -1f }, new[] { 0.5f, 0.8f, 1.5f });

            Assert.That(largest, Is.EqualTo(2.5f).Within(1e-6f), "음수 쪽으로 돌아도 크기로 센다");
        }

        [Test]
        public void Largest_IsZero_WhenThePosesAreTheSame()
        {
            Assert.That(JointTravel.Largest(new[] { 0.3f, -0.2f }, new[] { 0.3f, -0.2f }), Is.EqualTo(0f));
        }

        [Test]
        public void Largest_IsZero_WhenThereAreNoJoints()
        {
            Assert.That(JointTravel.Largest(new float[0], new float[0]), Is.EqualTo(0f));
        }
    }
}
