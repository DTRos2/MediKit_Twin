using System.Globalization;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class SimSpeedPanelTextsTests
    {
        [Test]
        public void Speed_DropsTheDecimalPointOfWholeNumbers()
        {
            Assert.AreEqual("0.5배", SimSpeedPanelTexts.Speed(0.5f));
            Assert.AreEqual("1배", SimSpeedPanelTexts.Speed(1f));
            Assert.AreEqual("2배", SimSpeedPanelTexts.Speed(2f));
            Assert.AreEqual("4배", SimSpeedPanelTexts.Speed(4f));
        }

        [Test]
        public void Current_AndLocked_NameTheSpeed()
        {
            Assert.AreEqual("현재 2배속", SimSpeedPanelTexts.Current(2f));
            Assert.AreEqual("현재 0.5배속", SimSpeedPanelTexts.Current(0.5f));
            Assert.AreEqual("1배속 고정", SimSpeedPanelTexts.Locked(1f));
        }

        [Test]
        public void TheNumbers_DoNotDependOnTheCulture()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                Assert.AreEqual("0.5배", SimSpeedPanelTexts.Speed(0.5f), "독일어 문화권에서도 소수점은 점이다");
                Assert.AreEqual("현재 0.5배속", SimSpeedPanelTexts.Current(0.5f));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
