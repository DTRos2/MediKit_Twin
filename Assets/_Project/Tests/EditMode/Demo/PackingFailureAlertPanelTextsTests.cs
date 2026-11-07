using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class PackingFailureAlertPanelTextsTests
    {
        [Test]
        public void Headline_NamesTheKitSequence()
        {
            Assert.AreEqual("키트 3번 뚜껑을 덮지 못했습니다.", PackingFailureAlertPanelTexts.Headline(3));
            Assert.AreEqual("키트 12번 뚜껑을 덮지 못했습니다.", PackingFailureAlertPanelTexts.Headline(12));
        }

        [Test]
        public void Headline_WithoutASequence_WritesZero()
        {
            Assert.AreEqual("키트 0번 뚜껑을 덮지 못했습니다.", PackingFailureAlertPanelTexts.Headline(0));
        }

        [Test]
        public void TheFixedTexts_AreNotEmpty()
        {
            Assert.That(PackingFailureAlertPanelTexts.Title, Is.Not.Empty);
            Assert.That(PackingFailureAlertPanelTexts.Subtitle, Is.Not.Empty);
            Assert.That(PackingFailureAlertPanelTexts.Guidance, Is.Not.Empty);
            Assert.That(PackingFailureAlertPanelTexts.RetryButton, Is.Not.Empty);
        }
    }
}
