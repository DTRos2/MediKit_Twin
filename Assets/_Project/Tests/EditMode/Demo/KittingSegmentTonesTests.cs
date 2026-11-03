using System.Collections.Generic;
using MediKitTwin.ArmA;
using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class KittingSegmentTonesTests
    {
        [TestCase(KittingItemState.Waiting, SegmentTone.Pending)]
        [TestCase(KittingItemState.Picking, SegmentTone.Active)]
        [TestCase(KittingItemState.Placing, SegmentTone.Active)]
        [TestCase(KittingItemState.Placed, SegmentTone.Done)]
        [TestCase(KittingItemState.Skipped, SegmentTone.Skipped)]
        public void EachItemState_MapsToABarTone(KittingItemState state, SegmentTone expected)
        {
            Assert.That(KittingSegmentTones.From(state), Is.EqualTo(expected));
        }

        [Test]
        public void AListOfStates_MapsOneToOne_KeepingTheOrder()
        {
            var states = new List<KittingItemState>
            {
                KittingItemState.Placed,
                KittingItemState.Picking,
                KittingItemState.Waiting,
                KittingItemState.Skipped,
            };

            IReadOnlyList<SegmentTone> tones = KittingSegmentTones.From(states);

            Assert.That(tones, Is.EqualTo(new[] { SegmentTone.Done, SegmentTone.Active, SegmentTone.Pending, SegmentTone.Skipped }));
        }

        [Test]
        public void AnEmptyList_GivesAnEmptyList()
        {
            Assert.That(KittingSegmentTones.From(new List<KittingItemState>()), Is.Empty);
        }

        [Test]
        public void EveryItemState_HasAMapping()
        {
            foreach (KittingItemState state in System.Enum.GetValues(typeof(KittingItemState)))
            {
                Assert.DoesNotThrow(() => KittingSegmentTones.From(state), state.ToString());
            }
        }
    }
}
