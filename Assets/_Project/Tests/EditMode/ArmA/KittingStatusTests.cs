using System.Collections.Generic;
using System.Linq;
using MediKitTwin.Common;
using MediKitTwin.Inventory;
using MediKitTwin.Recipes;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class KittingStatusTests
    {
        private float _time;
        private FakeBox _box;
        private FakeShelf _shelf;
        private FakeTransfer _transfer;
        private ShelfStock _stock;
        private KittingStatusTracker _tracker;
        private RecordingObserver _recorder;

        [SetUp]
        public void SetUp()
        {
            _time = 0f;
            _box = new FakeBox();
            _shelf = new FakeShelf();
            _transfer = new FakeTransfer();
            _tracker = new KittingStatusTracker();
            _recorder = new RecordingObserver(_tracker);
            _stock = NewStock(3, 3, 3);
        }

        private ShelfStock NewStock(int a1, int a2, int a3)
        {
            return new ShelfStock(
                new[] { new ShelfStockInit("A-1", a1), new ShelfStockInit("A-2", a2), new ShelfStockInit("A-3", a3) },
                refillDelaySeconds: 30f, refillQuantity: 5, now: () => _time, events: new RecordingStockSink());
        }

        private KittingPlan Plan(int? missingSlot = null)
        {
            var kit = new KitData(4, 7, "LOT-0004", "A병원", "EM");
            return new KittingPlanner(new SlotRule(missingSlot)).Plan(kit, KittingTestRecipes.Em());
        }

        private KittingOutcome Run(KittingPlan plan)
        {
            var process = new KittingProcess(_stock, _box, _box, _shelf, _transfer, () => _time, 5f, _recorder);
            var outcome = new KittingOutcome();
            CoroutineRunner.Drain(process.Run(plan, outcome), () => _time += 1f);
            return outcome;
        }

        // 진행 알림을 기록하면서 현황 기록기에도 그대로 전달한다.
        private sealed class RecordingObserver : IKittingObserver
        {
            private readonly IKittingObserver _inner;

            internal RecordingObserver(IKittingObserver inner)
            {
                _inner = inner;
            }

            internal List<string> Log { get; } = new List<string>();

            public void PlanStarted(KittingPlan plan)
            {
                Log.Add("plan");
                _inner.PlanStarted(plan);
            }

            public void ItemStateChanged(int itemIndex, KittingItemState state)
            {
                Log.Add($"{itemIndex}:{state}");
                _inner.ItemStateChanged(itemIndex, state);
            }

            public void Finished(KittingPlan plan, KittingOutcome outcome)
            {
                Log.Add("finished:" + outcome.Result);
                _inner.Finished(plan, outcome);
            }
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
        public void Process_Completed_NotifiesEachItemThroughPickingPlacingPlacedThenFinished()
        {
            Run(Plan());

            Assert.That(_recorder.Log, Is.EqualTo(new[]
            {
                "plan",
                "0:Picking", "0:Placing", "0:Placed",
                "1:Picking", "1:Placing", "1:Placed",
                "2:Picking", "2:Placing", "2:Placed",
                "finished:Completed",
            }));
        }

        [Test]
        public void Process_SkippedItem_SendsNoItemNotification()
        {
            Run(Plan(missingSlot: 2));

            Assert.That(_recorder.Log.Any(entry => entry.StartsWith("1:")), Is.False);
            Assert.That(_recorder.Log.Last(), Is.EqualTo("finished:Completed"));
        }

        [Test]
        public void Process_StockEmpty_FinishesWithStockShortage()
        {
            _stock = NewStock(3, 0, 3);

            Run(Plan());

            Assert.That(_recorder.Log.Last(), Is.EqualTo("finished:StockShortage"));
        }

        [Test]
        public void Process_Aborted_NotifiesFinishedExactlyOnce()
        {
            _transfer.FailPickNumber = 1;

            Run(Plan());

            Assert.That(_recorder.Log.Count(entry => entry.StartsWith("finished")), Is.EqualTo(1));
        }

        [Test]
        public void Process_WithoutObserver_StillRuns()
        {
            var process = new KittingProcess(_stock, _box, _box, _shelf, _transfer, () => _time, 5f);
            var outcome = new KittingOutcome();

            CoroutineRunner.Drain(process.Run(Plan(), outcome), () => _time += 1f);

            Assert.That(outcome.Result, Is.EqualTo(KittingResult.Completed));
        }

        [Test]
        public void Tracker_BeforeAnyKit_IsEmpty()
        {
            Assert.That(_tracker.CurrentPlan, Is.Null);
            Assert.That(_tracker.ItemStates, Is.Empty);
            Assert.That(_tracker.LastResult, Is.EqualTo(KittingResult.None));
        }

        [Test]
        public void Tracker_PlanStarted_MarksSkippedItemsAndWaitsForTheRest()
        {
            _tracker.PlanStarted(Plan(missingSlot: 2));

            Assert.That(_tracker.ItemStates, Is.EqualTo(new[]
            {
                KittingItemState.Waiting, KittingItemState.Skipped, KittingItemState.Waiting
            }));
        }

        [Test]
        public void Tracker_ItemStateChanged_UpdatesOnlyThatItemAndIgnoresOutOfRange()
        {
            _tracker.PlanStarted(Plan());

            _tracker.ItemStateChanged(1, KittingItemState.Picking);
            _tracker.ItemStateChanged(9, KittingItemState.Placed);
            _tracker.ItemStateChanged(-1, KittingItemState.Placed);

            Assert.That(_tracker.ItemStates[1], Is.EqualTo(KittingItemState.Picking));
            Assert.That(_tracker.ItemStates[0], Is.EqualTo(KittingItemState.Waiting));
        }

        [Test]
        public void Tracker_Finished_ClearsCurrentKitAndCountsEachResultSeparately()
        {
            var plan = Plan();
            _tracker.PlanStarted(plan);

            var completed = new KittingOutcome();
            completed.Complete();
            _tracker.Finished(plan, completed);

            var shortage = new KittingOutcome();
            shortage.Fail(KittingResult.StockShortage, "A-2", "재고 없음");
            _tracker.Finished(plan, shortage);

            var fault = new KittingOutcome();
            fault.Fail(KittingResult.Fault, null, "닿지 못함");
            _tracker.Finished(plan, fault);

            Assert.That(_tracker.CurrentPlan, Is.Null);
            Assert.That(_tracker.ItemStates, Is.Empty);
            Assert.That((_tracker.CompletedKitCount, _tracker.StockShortageCount, _tracker.FaultCount), Is.EqualTo((1, 1, 1)));
            Assert.That(_tracker.LastResult, Is.EqualTo(KittingResult.Fault));
            Assert.That(_tracker.LastMessage, Is.EqualTo("닿지 못함"));
            Assert.That(_tracker.LastLotNumber, Is.EqualTo("LOT-0004"));
        }

        [Test]
        public void Tracker_FedByARealRun_EndsWithTheCompletedKitCounted()
        {
            Run(Plan());

            Assert.That(_tracker.CompletedKitCount, Is.EqualTo(1));
            Assert.That(_tracker.CurrentPlan, Is.Null);
            Assert.That(_tracker.LastResult, Is.EqualTo(KittingResult.Completed));
        }
    }
}
