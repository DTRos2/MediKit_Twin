using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ShelfStockActionsTests
    {
        private const string Slot = "A-1";
        private const int OrderSequence = 7;

        private sealed class FakeStock : IStock
        {
            public int Quantity { get; set; }

            public int LastOrderSequence { get; private set; } = -1;

            public IReadOnlyList<string> ShelfSlotIds { get; } = new[] { Slot };

            public bool TryTake(string shelfSlotId, int orderSequence)
            {
                LastOrderSequence = orderSequence;
                if (Quantity == 0)
                {
                    return false;
                }

                Quantity--;
                return true;
            }

            public void Return(string shelfSlotId)
            {
                Quantity++;
            }

            public int GetQuantity(string shelfSlotId) => Quantity;

            public float? GetRefillRemainingSeconds(string shelfSlotId) => null;
        }

        [Test]
        public void Take_RemovesOneAndReportsTheNewQuantity()
        {
            var stock = new FakeStock { Quantity = 3 };
            var actions = new ShelfStockActions(stock, OrderSequence);

            string message = actions.Take(Slot);

            Assert.AreEqual(2, stock.Quantity);
            Assert.AreEqual(ShelfStockPanelTexts.Taken(Slot, 2), message);
            Assert.AreEqual(OrderSequence, stock.LastOrderSequence, "집을 때 설정한 주문 순번을 알려야 한다");
        }

        [Test]
        public void Take_FromAnEmptySlot_SaysThereIsNoStock()
        {
            var stock = new FakeStock { Quantity = 0 };
            var actions = new ShelfStockActions(stock, OrderSequence);

            string message = actions.Take(Slot);

            Assert.AreEqual(0, stock.Quantity);
            Assert.AreEqual(ShelfStockPanelTexts.OutOfStock(Slot), message);
        }

        [Test]
        public void Return_PutsOneBackAndReportsTheNewQuantity()
        {
            var stock = new FakeStock { Quantity = 1 };
            var actions = new ShelfStockActions(stock, OrderSequence);

            string message = actions.Return(Slot);

            Assert.AreEqual(2, stock.Quantity);
            Assert.AreEqual(ShelfStockPanelTexts.Returned(Slot, 2), message);
        }

        [Test]
        public void Constructor_RejectsAMissingStock()
        {
            Assert.Throws<System.ArgumentNullException>(() => new ShelfStockActions(null, OrderSequence));
        }
    }
}
