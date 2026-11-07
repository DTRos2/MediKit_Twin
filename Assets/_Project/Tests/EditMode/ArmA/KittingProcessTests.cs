using System.Linq;
using MediKitTwin.Common;
using MediKitTwin.Inventory;
using MediKitTwin.Recipes;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class KittingProcessTests
    {
        private const float BoxWaitTimeout = 5f;

        private float _time;
        private FakeBox _box;
        private FakeShelf _shelf;
        private FakeTransfer _transfer;
        private RecordingStockSink _stockEvents;
        private ShelfStock _stock;

        [SetUp]
        public void SetUp()
        {
            _time = 0f;
            _box = new FakeBox();
            _shelf = new FakeShelf();
            _transfer = new FakeTransfer();
            _stockEvents = new RecordingStockSink();
            _stock = NewStock(3, 3, 3);
        }

        private ShelfStock NewStock(int a1, int a2, int a3)
        {
            return new ShelfStock(
                new[] { new ShelfStockInit("A-1", a1), new ShelfStockInit("A-2", a2), new ShelfStockInit("A-3", a3) },
                refillDelaySeconds: 30f, refillQuantity: 5, now: () => _time, events: _stockEvents);
        }

        private KittingOutcome Run(int? missingSlot = null)
        {
            var kit = new KitData(1, 7, "LOT-0001", "병원", "EM");
            var plan = new KittingPlanner(new SlotRule(missingSlot)).Plan(kit, KittingTestRecipes.Em());
            var process = new KittingProcess(_stock, _box, _box, _shelf, _transfer, () => _time, BoxWaitTimeout);
            var outcome = new KittingOutcome();

            CoroutineRunner.Drain(process.Run(plan, outcome), () => _time += 1f);
            return outcome;
        }

        private sealed class SlotRule : IMissingRule
        {
            private readonly int? _slot;

            internal SlotRule(int? slot)
            {
                _slot = slot;
            }

            public int? SelectMissingSlot(KitData kit, RecipeSO recipe) => _slot;
        }

        [Test]
        public void Run_AllItems_CompletesPlacingEveryComponentAndTakingStock()
        {
            var outcome = Run();

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.Completed));
            Assert.That(_transfer.PickCount, Is.EqualTo(3));
            Assert.That(_transfer.PlaceCount, Is.EqualTo(3));
            Assert.That(new[] { "A-1", "A-2", "A-3" }.Select(_stock.GetQuantity), Is.EqualTo(new[] { 2, 2, 2 }));
        }

        [Test]
        public void Run_PlacesTheBoxFirst()
        {
            Run();

            Assert.That(_box.PlaceCount, Is.EqualTo(1));
            Assert.That(_box.RemoveCount, Is.EqualTo(0));
        }

        [Test]
        public void Run_PicksAndPlacesAlternatelyInRecipeOrder()
        {
            Run();

            Assert.That(_transfer.Events, Is.EqualTo(new[] { "pick", "place", "pick", "place", "pick", "place" }));
        }

        [Test]
        public void Run_SkippedItem_IsNeitherPickedNorTakenFromStock()
        {
            var outcome = Run(missingSlot: 2);

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.Completed));
            Assert.That(_transfer.PickCount, Is.EqualTo(2));
            Assert.That(_stock.GetQuantity("A-2"), Is.EqualTo(3));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(2));
            Assert.That(_stock.GetQuantity("A-3"), Is.EqualTo(2));
        }

        [Test]
        public void Run_StockEmptyAtSecondItem_StopsAndReturnsWhatWasTaken()
        {
            _stock = NewStock(3, 0, 3);

            var outcome = Run();

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.StockShortage));
            Assert.That(outcome.FailedShelfSlotId, Is.EqualTo("A-2"));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(3), "이미 꺼낸 구성품은 재고로 돌아와야 한다");
            Assert.That(_transfer.PickCount, Is.EqualTo(1), "빈 선반까지 헛걸음하지 않는다");
        }

        [Test]
        public void Run_StockEmpty_RaisesStockEmptyWithTheOrderSequenceAndRemovesTheBox()
        {
            _stock = NewStock(3, 0, 3);

            Run();

            Assert.That(_stockEvents.Empty, Is.EqualTo(new[] { ("A-2", 7) }));
            Assert.That(_box.RemoveCount, Is.EqualTo(1));
            Assert.That(_transfer.DiscardCount, Is.EqualTo(1));
        }

        [Test]
        public void Run_StockEmptyRaceDuringGrasp_ReportsStockShortageAndReturnsTaken()
        {
            // 로봇이 선반으로 가는 사이 다른 곳에서 마지막 하나를 가져가 쥐는 순간 꺼내기에 실패하는 상황.
            _stock = NewStock(3, 1, 3);
            _transfer.BeforeCommit = () =>
            {
                if (_transfer.PickCount == 2)
                {
                    _stock.TryTake("A-2", 99);
                }
            };

            var outcome = Run();

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.StockShortage));
            Assert.That(outcome.FailedShelfSlotId, Is.EqualTo("A-2"));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(3));
        }

        [Test]
        public void Run_PickMotionFails_IsAFaultAndTakesNothing()
        {
            _transfer.FailPickNumber = 1;

            var outcome = Run();

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.Fault));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(3));
            Assert.That(_box.RemoveCount, Is.EqualTo(1));
        }

        [Test]
        public void Run_PlaceMotionFails_IsAFaultAndReturnsEveryTakenComponent()
        {
            _transfer.FailPlaceNumber = 2;

            var outcome = Run();

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.Fault));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(3));
            Assert.That(_stock.GetQuantity("A-2"), Is.EqualTo(3), "집었지만 놓지 못한 구성품도 돌려놓는다");
            Assert.That(_transfer.DiscardCount, Is.EqualTo(1));
        }

        [Test]
        public void Run_BoxNeverAppears_IsAFaultAfterTheTimeout()
        {
            _box.NeverAppears = true;

            var outcome = Run();

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.Fault));
            Assert.That(_transfer.PickCount, Is.EqualTo(0));
            Assert.That(_time, Is.GreaterThan(BoxWaitTimeout));
        }

        [Test]
        public void Run_ItemMissingOnShelfDespiteStock_IsAFault()
        {
            _shelf.EmptySlots.Add("A-1");

            var outcome = Run();

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.Fault));
            Assert.That(outcome.FailedShelfSlotId, Is.EqualTo("A-1"));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(3));
        }

        [Test]
        public void Run_AfterAbortInTheMiddle_OutcomeCarriesAMessage()
        {
            _stock = NewStock(0, 3, 3);

            var outcome = Run();

            Assert.That(outcome.IsFinished, Is.True);
            Assert.That(outcome.Message, Does.Contain("A-1"));
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new KittingProcess(null, _box, _box, _shelf, _transfer, () => 0f, 1f));
        }
    }
}
