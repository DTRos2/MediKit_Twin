using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class BaseMoveInputTests
    {
        [Test]
        public void NoDirection_IsIdle()
        {
            Assert.That(new BaseMoveInput(0f, 0f, 0f, false).IsIdle, Is.True);
        }

        [Test]
        public void AnyDirection_IsNotIdle()
        {
            Assert.That(new BaseMoveInput(1f, 0f, 0f, false).IsIdle, Is.False);
            Assert.That(new BaseMoveInput(0f, -1f, 0f, false).IsIdle, Is.False);
            Assert.That(new BaseMoveInput(0f, 0f, 1f, false).IsIdle, Is.False);
        }

        [Test]
        public void HoldingOnlyTheFastKey_IsStillIdle()
        {
            Assert.That(new BaseMoveInput(0f, 0f, 0f, true).IsIdle, Is.True);
        }
    }
}
