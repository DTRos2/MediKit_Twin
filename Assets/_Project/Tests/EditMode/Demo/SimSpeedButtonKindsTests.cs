using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class SimSpeedButtonKindsTests
    {
        [Test]
        public void TheButtonOfTheCurrentSpeed_LooksSelected()
        {
            Assert.AreEqual(ButtonKind.Selected, SimSpeedButtonKinds.For(2f, 2f));
        }

        [Test]
        public void TheOtherButtons_LookPlain()
        {
            Assert.AreEqual(ButtonKind.Secondary, SimSpeedButtonKinds.For(0.5f, 2f));
            Assert.AreEqual(ButtonKind.Secondary, SimSpeedButtonKinds.For(4f, 2f));
        }

        [Test]
        public void SpeedsThatDifferOnlyByRounding_AreTheSame()
        {
            Assert.AreEqual(ButtonKind.Selected, SimSpeedButtonKinds.For(0.5f, 0.1f + 0.4f));
        }
    }
}
