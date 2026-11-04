using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class OrderPanelTextsTests
    {
        [Test]
        public void Summary_ShowsOrderCountAndKitCount()
        {
            Assert.AreEqual("3건 · 키트 7개", OrderPanelTexts.Summary(3, 7));
        }

        [Test]
        public void Held_AndPending_ShowTheirCounts()
        {
            Assert.AreEqual("보류 2건", OrderPanelTexts.Held(2));
            Assert.AreEqual("대기 키트 5개", OrderPanelTexts.Pending(5));
        }

        [Test]
        public void Overflow_SaysHowManyDidNotFit()
        {
            Assert.AreEqual("외 3건 더", OrderPanelTexts.Overflow(3));
        }

        [Test]
        public void Issues_SaysNoWarningWhenThereAreNone()
        {
            Assert.AreEqual("경고 없음", OrderPanelTexts.Issues(0));
            Assert.AreEqual("경고 1건", OrderPanelTexts.Issues(1));
            Assert.AreEqual("경고 4건", OrderPanelTexts.Issues(4));
        }

        [Test]
        public void Issue_PutsABulletBeforeTheMessage()
        {
            Assert.AreEqual("• 알 수 없는 키트 코드", OrderPanelTexts.Issue("알 수 없는 키트 코드"));
        }

        [Test]
        public void Quantity_ShowsATimesMark()
        {
            Assert.AreEqual("×3", OrderPanelTexts.Quantity(3));
        }
    }
}
