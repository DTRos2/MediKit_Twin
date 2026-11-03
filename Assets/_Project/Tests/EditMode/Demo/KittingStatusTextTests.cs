using System;
using System.Collections.Generic;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class KittingStatusTextTests
    {
        private sealed class FakeStatus : IKittingStatus
        {
            public KittingPlan CurrentPlan { get; set; }

            public IReadOnlyList<KittingItemState> ItemStates { get; set; } = new KittingItemState[0];

            public KittingResult LastResult { get; set; }

            public string LastMessage { get; set; }

            public string LastLotNumber { get; set; }

            public int CompletedKitCount { get; set; }

            public int StockShortageCount { get; set; }

            public int FaultCount { get; set; }
        }

        [TestCase(StationState.Idle, "대기", "IDLE")]
        [TestCase(StationState.Running, "가동", "RUN")]
        [TestCase(StationState.Fault, "정지", "FAULT")]
        public void TheStationState_HasAKoreanWordAndAnEnglishRingLabel(StationState state, string korean, string ring)
        {
            Assert.That(KittingStatusText.StationText(state), Is.EqualTo(korean));
            Assert.That(KittingStatusText.StationRingLabel(state), Is.EqualTo(ring));
        }

        [TestCase(KittingItemState.Waiting, "대기")]
        [TestCase(KittingItemState.Picking, "집는 중")]
        [TestCase(KittingItemState.Placing, "옮기는 중")]
        [TestCase(KittingItemState.Placed, "완료")]
        [TestCase(KittingItemState.Skipped, "누락")]
        public void EachItemState_HasItsOwnWord(KittingItemState state, string expected)
        {
            Assert.That(KittingStatusText.ItemText(state), Is.EqualTo(expected));
        }

        [Test]
        public void EveryItemState_HasADistinctWord()
        {
            var words = new HashSet<string>();
            foreach (KittingItemState state in Enum.GetValues(typeof(KittingItemState)))
            {
                Assert.That(words.Add(KittingStatusText.ItemText(state)), Is.True, $"{state}의 글이 다른 상태와 같다");
            }
        }

        [Test]
        public void WhenNoKitIsBeingMade_TheSummarySaysSo_AndTheDetailIsEmpty()
        {
            Assert.That(KittingStatusText.KitSummary(null), Is.EqualTo("지금 만드는 키트 없음"));
            Assert.That(KittingStatusText.KitDetail(null), Is.Empty);
        }

        [Test]
        public void TheKitSummary_ShowsKindSequenceOrderAndHospital()
        {
            var kit = new KitData(3, 2, "LOT-1", "A병원", "em");
            var plan = new KittingPlan(kit, null, new List<KittingItem>(), null);

            Assert.That(KittingStatusText.KitSummary(plan), Is.EqualTo("EM 키트 #3 · 주문 #2 · A병원"));
        }

        [Test]
        public void WithNoResultYet_TheSummaryIsNotYet_AndThereIsNoDetail()
        {
            var status = new FakeStatus { LastResult = KittingResult.None };

            Assert.That(KittingStatusText.LastResultSummary(status), Is.EqualTo("아직 없음"));
            Assert.That(KittingStatusText.LastResultDetail(status), Is.Empty);
        }

        [Test]
        public void ACompletedKit_ShowsTheLotAndNoDetail()
        {
            var status = new FakeStatus { LastResult = KittingResult.Completed, LastLotNumber = "LOT-7", LastMessage = "정상" };

            Assert.That(KittingStatusText.LastResultSummary(status), Is.EqualTo("LOT-7 완료"));
            Assert.That(KittingStatusText.LastResultDetail(status), Is.Empty, "성공했을 때는 이유를 따로 보여 주지 않는다");
        }

        [Test]
        public void AStockShortage_ShowsTheLotAndTheReason()
        {
            var status = new FakeStatus { LastResult = KittingResult.StockShortage, LastLotNumber = "LOT-8", LastMessage = "A-1 재고 없음" };

            Assert.That(KittingStatusText.LastResultSummary(status), Is.EqualTo("LOT-8 재고 부족"));
            Assert.That(KittingStatusText.LastResultDetail(status), Is.EqualTo("A-1 재고 없음"));
        }

        [Test]
        public void AFault_ShowsTheLotAndTheReason()
        {
            var status = new FakeStatus { LastResult = KittingResult.Fault, LastLotNumber = "LOT-9", LastMessage = "집기 실패" };

            Assert.That(KittingStatusText.LastResultSummary(status), Is.EqualTo("LOT-9 오류"));
            Assert.That(KittingStatusText.LastResultDetail(status), Is.EqualTo("집기 실패"));
        }

        [Test]
        public void AFailureWithoutAMessage_GivesAnEmptyDetailInsteadOfNull()
        {
            var status = new FakeStatus { LastResult = KittingResult.Fault, LastLotNumber = "LOT-9", LastMessage = null };

            Assert.That(KittingStatusText.LastResultDetail(status), Is.EqualTo(string.Empty));
        }
    }
}
