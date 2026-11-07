using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class OrderBoardTests
    {
        private static OrderBoard Board()
        {
            var orders = new[]
            {
                new OrderRecord(1, "A", "EM", 2),
                new OrderRecord(2, "B", "DX", 1),
                new OrderRecord(3, "C", "BL", 1),
            };
            var requests = new KitRequestPlanner(new SequentialLotNumberGenerator("LOT-", 4)).Plan(orders);
            return new OrderBoard(orders, requests);
        }

        [Test]
        public void Constructor_DistributesKitRequestsToTheirOrdersInFileOrder()
        {
            var board = Board();

            Assert.That(board.FirstReady.Sequence, Is.EqualTo(1));
            Assert.That(board.FirstReady.PendingCount, Is.EqualTo(2));
            Assert.That(board.PendingKitCount, Is.EqualTo(4));
        }

        [Test]
        public void MoveToHeld_RemovesFromReadyAndCountsItsPendingKits()
        {
            var board = Board();
            var first = board.FirstReady;

            board.MoveToHeld(first);

            Assert.That(board.FirstReady.Sequence, Is.EqualTo(2));
            Assert.That(board.HeldCount, Is.EqualTo(1));
            Assert.That(board.PendingKitCount, Is.EqualTo(4));
        }

        [Test]
        public void ResumeToFront_PutsOrdersBeforeReadyKeepingGivenOrder()
        {
            var board = Board();
            board.TryGetOrder(2, out var second);
            board.TryGetOrder(3, out var third);
            board.MoveToHeld(second);
            board.MoveToHeld(third);

            board.ResumeToFront(new[] { second, third });

            Assert.That(board.HeldCount, Is.EqualTo(0));
            Assert.That(board.FirstReady.Sequence, Is.EqualTo(2));
        }

        [Test]
        public void TryGetOrder_UnknownSequence_ReturnsFalse()
        {
            Assert.That(Board().TryGetOrder(99, out _), Is.False);
        }

        [Test]
        public void Held_ListsInHeldOrder()
        {
            var board = Board();
            board.TryGetOrder(3, out var third);
            board.TryGetOrder(1, out var first);
            board.MoveToHeld(third);
            board.MoveToHeld(first);

            Assert.That(board.Held.Select(o => o.Sequence), Is.EqualTo(new[] { 3, 1 }));
        }
    }
}
