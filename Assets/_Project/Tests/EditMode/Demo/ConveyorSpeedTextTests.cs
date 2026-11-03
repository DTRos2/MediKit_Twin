using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ConveyorSpeedTextTests
    {
        [TestCase(0.8f, "0.80 m/s")]
        [TestCase(2f, "2.00 m/s")]
        [TestCase(0.25f, "0.25 m/s")]
        public void Format_ShowsTwoDecimalsAndTheUnit(float speed, string expected)
        {
            Assert.That(ConveyorSpeedText.Format(speed), Is.EqualTo(expected));
        }
    }
}
