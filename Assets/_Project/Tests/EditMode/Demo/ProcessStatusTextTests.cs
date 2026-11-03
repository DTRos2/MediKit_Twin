using System;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ProcessStatusTextTests
    {
        private static KitData Kit() => new KitData(3, 2, "LOT-3", "서울병원", "bl");

        [Test]
        public void EveryStation_HasAName()
        {
            foreach (StationId station in Enum.GetValues(typeof(StationId)))
            {
                Assert.That(ProcessStatusText.StationName(station), Is.Not.Empty, station.ToString());
            }
        }

        [Test]
        public void TheStationChip_JoinsTheNameAndTheKoreanState()
        {
            Assert.That(ProcessStatusText.StationChip(StationId.ArmA, StationState.Running), Is.EqualTo("로봇암 A 가동"));
            Assert.That(ProcessStatusText.StationChip(StationId.Conveyor, StationState.Fault), Is.EqualTo("컨베이어 정지"));
            Assert.That(ProcessStatusText.StationChip(StationId.Inspection, StationState.Idle), Is.EqualTo("검사 대기"));
        }

        [Test]
        public void EveryStage_HasADistinctNonEmptyText()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (ProcessStage stage in Enum.GetValues(typeof(ProcessStage)))
            {
                string text = ProcessStatusText.Stage(stage);
                Assert.That(text, Is.Not.Empty, stage.ToString());
                Assert.That(seen.Add(text), Is.True, $"{stage}의 글이 다른 단계와 같다");
            }
        }

        [Test]
        public void WithoutAKit_TheHeadlineSaysKittingWhileArmAWorks_AndIdleOtherwise()
        {
            var tally = new ProcessTally();
            Assert.That(ProcessStatusText.Headline(tally), Is.EqualTo("대기 중"));

            tally.OnStationStateChanged(StationId.ArmA, StationState.Running);

            Assert.That(ProcessStatusText.Headline(tally), Is.EqualTo("키팅 중"));
        }

        [Test]
        public void WithAKit_TheHeadlineIsItsStage_EvenWhileArmAWorks()
        {
            var tally = new ProcessTally();
            tally.OnStationStateChanged(StationId.ArmA, StationState.Running);
            tally.OnAtInspection(Kit());

            Assert.That(ProcessStatusText.Headline(tally), Is.EqualTo("검사 중"));
        }

        [Test]
        public void TheKitSummary_ShowsCodeSequenceOrderHospitalAndLot()
        {
            Assert.That(ProcessStatusText.KitSummary(Kit()), Is.EqualTo("BL 키트 #3 · 주문 #2 · 서울병원 · 로트 LOT-3"));
        }

        [Test]
        public void WithoutAKit_TheSummaryIsAGuideText()
        {
            Assert.That(ProcessStatusText.KitSummary(null), Is.EqualTo("라인 위의 키트 없음"));
        }

        [TestCase(ProcessStage.None, ChipTone.Neutral)]
        [TestCase(ProcessStage.ToInspection, ChipTone.Accent)]
        [TestCase(ProcessStage.Inspecting, ChipTone.Accent)]
        [TestCase(ProcessStage.Rejecting, ChipTone.Danger)]
        [TestCase(ProcessStage.Rejected, ChipTone.Danger)]
        [TestCase(ProcessStage.ToPacking, ChipTone.Accent)]
        [TestCase(ProcessStage.Packing, ChipTone.Accent)]
        [TestCase(ProcessStage.ToEnd, ChipTone.Accent)]
        [TestCase(ProcessStage.Done, ChipTone.Success)]
        public void EachStage_HasItsTone(ProcessStage stage, ChipTone tone)
        {
            Assert.That(ProcessChipTones.Stage(stage), Is.EqualTo(tone));
        }
    }
}
