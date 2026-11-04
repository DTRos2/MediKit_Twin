using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ConveyorSpeedPanelTextsTests
    {
        [Test]
        public void TheResetButton_NamesTheSpeedItGoesBackTo()
        {
            Assert.AreEqual("처음 속도로 (0.50 m/s)", ConveyorSpeedPanelTexts.Reset(0.5f));
        }

        [Test]
        public void TheResetButton_UsesTheSameSpeedFormatAsTheHeader()
        {
            Assert.That(ConveyorSpeedPanelTexts.Reset(1.2f), Does.Contain(ConveyorSpeedText.Format(1.2f)));
        }
    }
}
