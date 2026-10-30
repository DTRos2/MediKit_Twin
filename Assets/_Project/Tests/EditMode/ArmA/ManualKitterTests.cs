using System.Linq;
using MediKitTwin.Inventory;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ManualKitterTests
    {
        private const float BoxWaitTimeout = 5f;

        private float _time;
        private FakeBox _box;
        private FakeShelf _shelf;
        private FakeTransfer _transfer;
        private ShelfStock _stock;
        private ManualKittingSession _session;

        [SetUp]
        public void SetUp()
        {
            _time = 0f;
            _box = new FakeBox();
            _shelf = new FakeShelf();
            _transfer = new FakeTransfer();
            _stock = NewStock(3, 3, 3);
            _session = new ManualKittingSession();
            _session.SelectRecipe(KittingTestRecipes.Em());
        }

        private ShelfStock NewStock(int a1, int a2, int a3)
        {
            return new ShelfStock(
                new[] { new ShelfStockInit("A-1", a1), new ShelfStockInit("A-2", a2), new ShelfStockInit("A-3", a3) },
                refillDelaySeconds: 30f, refillQuantity: 5, now: () => _time, events: new RecordingStockSink());
        }

        private ManualFillResult Fill(string shelfSlotId)
        {
            var awaiter = new BoxAwaiter(_box, _box, () => _time, BoxWaitTimeout);
            var filler = new ItemFiller(_stock, _box, _shelf, _transfer);
            var kitter = new ManualKitter(_session, _stock, _box, awaiter, filler, _transfer, orderSequence: 0);
            var result = new ManualFillResult();

            CoroutineRunner.Drain(kitter.Fill(shelfSlotId, result), () => _time += 1f);
            return result;
        }

        [Test]
        public void Fill_ARecipeSlot_PicksPlacesAndTakesOneFromStock()
        {
            ManualFillResult result = Fill("A-1");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_transfer.Events, Is.EqualTo(new[] { "pick", "place" }));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(2));
        }

        [Test]
        public void Fill_PutsABoxOnTheJigFirst()
        {
            Fill("A-1");

            Assert.That(_box.PlaceCount, Is.EqualTo(1));
        }

        [Test]
        public void Fill_ShelfSlotNotInTheRecipe_DoesNotMoveTheRobotOrTouchStock()
        {
            ManualFillResult result = Fill("C-1");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_transfer.PickCount, Is.EqualTo(0));
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(3));
        }

        [Test]
        public void Fill_TheSameSlotTwice_RefusesTheSecondAndTakesStockOnce()
        {
            Fill("A-2");

            ManualFillResult second = Fill("A-2");

            Assert.That(second.Succeeded, Is.False);
            Assert.That(_transfer.PickCount, Is.EqualTo(1));
            Assert.That(_stock.GetQuantity("A-2"), Is.EqualTo(2));
        }

        [Test]
        public void Fill_EveryRecipeSlot_CompletesTheKit()
        {
            ManualFillResult last = null;
            foreach (string slot in new[] { "A-1", "A-2", "A-3" })
            {
                last = Fill(slot);
            }

            Assert.That(last.Succeeded, Is.True);
            Assert.That(last.CompletedKit, Is.True);
            Assert.That(_box.RemoveCount, Is.EqualTo(0), "박스는 치우지 않는다");
        }

        [Test]
        public void Fill_PickFails_ReturnsNothingTakenDiscardsAndLeavesTheSlotFree()
        {
            _transfer.FailPickNumber = 1;

            ManualFillResult failed = Fill("A-1");
            _transfer.FailPickNumber = 0;
            ManualFillResult retried = Fill("A-1");

            Assert.That(failed.Succeeded, Is.False);
            Assert.That(_transfer.DiscardCount, Is.EqualTo(1));
            Assert.That(retried.Succeeded, Is.True, "실패한 슬롯은 다시 시도할 수 있다");
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(2));
        }

        [Test]
        public void Fill_PlaceFailsAfterGrasp_ReturnsTheTakenStock()
        {
            _transfer.FailPlaceNumber = 1;

            ManualFillResult result = Fill("A-1");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_stock.GetQuantity("A-1"), Is.EqualTo(3), "꺼낸 재고는 돌려놓는다");
            Assert.That(_transfer.DiscardCount, Is.EqualTo(1));
        }

        [Test]
        public void Fill_EmptyStock_RefusesWithoutMovingTheRobot()
        {
            _stock = NewStock(0, 3, 3);

            ManualFillResult result = Fill("A-1");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("재고"));
            Assert.That(_transfer.PickCount, Is.EqualTo(0));
        }

        [Test]
        public void Fill_NoRecipeSelected_Refuses()
        {
            _session = new ManualKittingSession();

            ManualFillResult result = Fill("A-1");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(new[] { _box.PlaceCount, _transfer.PickCount }.Sum(), Is.EqualTo(0));
        }

        [Test]
        public void Fill_BoxNeverAppears_FailsWithoutMovingTheRobot()
        {
            _box.NeverAppears = true;

            ManualFillResult result = Fill("A-1");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_transfer.PickCount, Is.EqualTo(0));
        }
    }
}
