using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class KittingChipTonesTests
    {
        [TestCase(StationState.Idle, ChipTone.Neutral)]
        [TestCase(StationState.Running, ChipTone.Accent)]
        [TestCase(StationState.Fault, ChipTone.Danger)]
        public void TheStationState_MapsToAChipTone(StationState state, ChipTone expected)
        {
            Assert.That(KittingChipTones.Station(state), Is.EqualTo(expected));
        }

        [TestCase(KittingItemState.Waiting, ChipTone.Neutral)]
        [TestCase(KittingItemState.Picking, ChipTone.Accent)]
        [TestCase(KittingItemState.Placing, ChipTone.Accent)]
        [TestCase(KittingItemState.Placed, ChipTone.Success)]
        [TestCase(KittingItemState.Skipped, ChipTone.Warning)]
        public void TheItemState_MapsToAChipTone(KittingItemState state, ChipTone expected)
        {
            Assert.That(KittingChipTones.Item(state), Is.EqualTo(expected));
        }
    }
}
