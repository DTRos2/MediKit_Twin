using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ProcessChipTonesTests
    {
        private static KitData Kit(int sequence) => new KitData(sequence, sequence, $"LOT-{sequence}", "병원", "BL");

        [Test]
        public void TheHeadline_IsNeutral_WhileNoKitIsOnTheLine()
        {
            var tally = new ProcessTally();

            Assert.AreEqual(ChipTone.Neutral, ProcessChipTones.Headline(tally));
        }

        [Test]
        public void TheHeadline_FollowsTheStageOfTheCurrentKit()
        {
            var tally = new ProcessTally();
            tally.OnKitReady(Kit(1));

            Assert.AreEqual(ChipTone.Accent, ProcessChipTones.Headline(tally));

            tally.OnKitRejected(Kit(1));
            Assert.AreEqual(ChipTone.Danger, ProcessChipTones.Headline(tally));

            tally.OnKitShipped(Kit(2));
            Assert.AreEqual(ChipTone.Success, ProcessChipTones.Headline(tally));
        }

        [TestCase(ProcessStage.None, ChipTone.Neutral)]
        [TestCase(ProcessStage.Inspecting, ChipTone.Accent)]
        [TestCase(ProcessStage.Rejecting, ChipTone.Danger)]
        [TestCase(ProcessStage.Rejected, ChipTone.Danger)]
        [TestCase(ProcessStage.Done, ChipTone.Success)]
        public void EachStage_HasItsOwnTone(ProcessStage stage, ChipTone expected)
        {
            Assert.AreEqual(expected, ProcessChipTones.Stage(stage));
        }
    }
}
