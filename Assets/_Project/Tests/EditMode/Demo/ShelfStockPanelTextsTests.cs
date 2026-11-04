using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ShelfStockPanelTextsTests
    {
        [Test]
        public void Total_SaysTheNumberOfPiecesOnTheShelf()
        {
            Assert.AreEqual("총 36개", ShelfStockPanelTexts.Total(36));
        }

        [Test]
        public void RefillNote_IsTheWholeSecondsLeft()
        {
            Assert.AreEqual("12초", ShelfStockPanelTexts.RefillNote(12.2f));
            Assert.AreEqual("0초", ShelfStockPanelTexts.RefillNote(0.3f));
        }

        [Test]
        public void TheResultSentences_NameTheSlotAndTheQuantityNow()
        {
            Assert.AreEqual("B-2: 집음 → 4개", ShelfStockPanelTexts.Taken("B-2", 4));
            Assert.AreEqual("B-2: 되돌림 → 5개", ShelfStockPanelTexts.Returned("B-2", 5));
            Assert.That(ShelfStockPanelTexts.OutOfStock("B-2"), Does.StartWith("B-2:"));
        }
    }
}
