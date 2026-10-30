using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MediKitTwin.Inventory.Tests
{
    public sealed class ShelfStockTests
    {
        private float _now;
        private RecordingStockSink _events;

        [SetUp]
        public void SetUp()
        {
            _now = 0f;
            _events = new RecordingStockSink();
        }

        private ShelfStock Create(params ShelfStockInit[] initial)
        {
            return new ShelfStock(initial, refillDelaySeconds: 30f, refillQuantity: 5, now: () => _now, events: _events);
        }

        [Test]
        public void TryTake_WithStock_DecreasesQuantityAndRaisesQuantityChanged()
        {
            var stock = Create(new ShelfStockInit("C-3", 2));
            var changes = new List<(string, int)>();
            stock.QuantityChanged += (id, quantity) => changes.Add((id, quantity));

            Assert.That(stock.TryTake("C-3", 1), Is.True);

            Assert.That(stock.GetQuantity("C-3"), Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(new[] { ("C-3", 1) }));
        }

        [Test]
        public void TryTake_AtZero_FailsAndRaisesStockEmptyWithOrderSequence()
        {
            var stock = Create(new ShelfStockInit("C-3", 0));

            Assert.That(stock.TryTake("C-3", 7), Is.False);

            Assert.That(_events.Empty, Is.EqualTo(new[] { ("C-3", 7) }));
        }

        [Test]
        public void TryTake_RepeatedFailuresWhileWaiting_RaiseStockEmptyEachTimeButStartTimerOnce()
        {
            var stock = Create(new ShelfStockInit("C-3", 0));

            stock.TryTake("C-3", 1);
            _now = 10f;
            stock.TryTake("C-3", 2);

            Assert.That(_events.Empty, Is.EqualTo(new[] { ("C-3", 1), ("C-3", 2) }));
            Assert.That(stock.GetRefillRemainingSeconds("C-3"), Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void TryTake_UnknownSlot_FailsWithoutSignal()
        {
            var stock = Create(new ShelfStockInit("C-3", 1));

            Assert.That(stock.TryTake("Z-9", 1), Is.False);
            Assert.That(stock.TryTake(null, 1), Is.False);

            Assert.That(_events.Empty, Is.Empty);
        }

        [Test]
        public void Tick_BeforeDelay_DoesNotRefill()
        {
            var stock = Create(new ShelfStockInit("C-3", 0));
            stock.TryTake("C-3", 1);

            _now = 29.9f;
            stock.Tick();

            Assert.That(stock.GetQuantity("C-3"), Is.EqualTo(0));
            Assert.That(_events.Refilled, Is.Empty);
        }

        [Test]
        public void Tick_AfterDelay_SetsQuantityToRefillQuantityAndRaisesStockRefilled()
        {
            var stock = Create(new ShelfStockInit("C-3", 0));
            stock.TryTake("C-3", 1);

            _now = 30f;
            stock.Tick();

            Assert.That(stock.GetQuantity("C-3"), Is.EqualTo(5));
            Assert.That(_events.Refilled, Is.EqualTo(new[] { "C-3" }));
            Assert.That(stock.GetRefillRemainingSeconds("C-3"), Is.Null);
        }

        [Test]
        public void Tick_RefillSetsQuantityRatherThanAdding()
        {
            var stock = Create(new ShelfStockInit("C-3", 1));
            stock.TryTake("C-3", 1);
            stock.TryTake("C-3", 1);
            _now = 30f;

            stock.Tick();

            Assert.That(stock.GetQuantity("C-3"), Is.EqualTo(5));
        }

        [Test]
        public void Tick_RefillRaisesQuantityChangedSoTheViewCanFillTheCell()
        {
            var stock = Create(new ShelfStockInit("C-3", 0));
            int last = -1;
            stock.QuantityChanged += (id, quantity) => last = quantity;
            stock.TryTake("C-3", 1);

            _now = 30f;
            stock.Tick();

            Assert.That(last, Is.EqualTo(5));
        }

        [Test]
        public void Tick_AfterRefill_AllowsNewEmptyCycle()
        {
            var stock = Create(new ShelfStockInit("C-3", 0));
            stock.TryTake("C-3", 1);
            _now = 30f;
            stock.Tick();
            for (int i = 0; i < 5; i++)
            {
                stock.TryTake("C-3", 1);
            }

            stock.TryTake("C-3", 1);

            Assert.That(stock.GetRefillRemainingSeconds("C-3"), Is.EqualTo(30f).Within(1e-4f));
        }

        [Test]
        public void Return_IncreasesQuantity()
        {
            var stock = Create(new ShelfStockInit("C-1", 2));

            stock.Return("C-1");

            Assert.That(stock.GetQuantity("C-1"), Is.EqualTo(3));
        }

        [Test]
        public void Return_WhileWaitingForRefill_IsIgnored()
        {
            var stock = Create(new ShelfStockInit("C-3", 0));
            stock.TryTake("C-3", 1);

            stock.Return("C-3");

            Assert.That(stock.GetQuantity("C-3"), Is.EqualTo(0));
        }

        [Test]
        public void GetRefillRemainingSeconds_NotWaiting_IsNull()
        {
            Assert.That(Create(new ShelfStockInit("C-3", 1)).GetRefillRemainingSeconds("C-3"), Is.Null);
        }

        [Test]
        public void ShelfSlotIds_KeepsGivenOrder()
        {
            var stock = Create(new ShelfStockInit("B-1", 1), new ShelfStockInit("A-1", 1));

            Assert.That(stock.ShelfSlotIds, Is.EqualTo(new[] { "B-1", "A-1" }));
        }

        [Test]
        public void Constructor_InvalidArguments_Throw()
        {
            var none = new ShelfStockInit[0];

            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfStock(none, -1f, 5, () => 0f, _events));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfStock(none, 30f, 0, () => 0f, _events));
            Assert.Throws<ArgumentException>(() =>
                new ShelfStock(new[] { new ShelfStockInit("A-1", 1), new ShelfStockInit("A-1", 2) }, 30f, 5, () => 0f, _events));
        }

        private sealed class RecordingStockSink : IStockEventSink
        {
            internal List<(string, int)> Empty { get; } = new List<(string, int)>();

            internal List<string> Refilled { get; } = new List<string>();

            public void StockEmpty(string shelfSlotId, int orderSequence) => Empty.Add((shelfSlotId, orderSequence));

            public void StockRefilled(string shelfSlotId) => Refilled.Add(shelfSlotId);
        }
    }
}
