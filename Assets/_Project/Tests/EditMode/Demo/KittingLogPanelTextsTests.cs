using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class KittingLogPanelTextsTests
    {
        [Test]
        public void Copied_TellsHowManyLinesWereCopied()
        {
            Assert.AreEqual("3줄 복사했다", KittingLogPanelTexts.Copied(3));
        }

        [Test]
        public void Copied_SaysThereIsNothingToCopyWhenEmpty()
        {
            Assert.AreEqual("복사할 로그가 없다", KittingLogPanelTexts.Copied(0));
        }

        [Test]
        public void Elapsed_HasOneDecimalAndTheSecondMark()
        {
            Assert.AreEqual("12.3s", KittingLogPanelTexts.Elapsed(12.34));
            Assert.AreEqual("0.0s", KittingLogPanelTexts.Elapsed(0d));
        }

        [Test]
        public void RowCapacity_IsPositive()
        {
            Assert.That(KittingLogPanelTexts.RowCapacity, Is.GreaterThan(0));
        }
    }
}
