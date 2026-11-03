using System.Collections.Generic;
using MediKitTwin.ArmA;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class KittingProgressTests
    {
        private static KittingItem Item(int slot, bool isSkipped = false)
        {
            return new KittingItem($"C{slot}", $"A-{slot}", slot, Vector3.zero, isSkipped);
        }

        [Test]
        public void ItCountsThePlacedItems_AndTheOnesThatMustBePlaced()
        {
            var items = new List<KittingItem> { Item(1), Item(2), Item(3), Item(4) };
            var states = new List<KittingItemState>
            {
                KittingItemState.Placed, KittingItemState.Placed, KittingItemState.Picking, KittingItemState.Waiting,
            };

            KittingProgress progress = KittingProgress.From(items, states);

            Assert.That((progress.Placed, progress.ToPlace), Is.EqualTo((2, 4)));
        }

        [Test]
        public void ASkippedItem_IsNotCountedAsSomethingToPlace()
        {
            var items = new List<KittingItem> { Item(1), Item(2, isSkipped: true), Item(3) };
            var states = new List<KittingItemState> { KittingItemState.Placed, KittingItemState.Skipped, KittingItemState.Placed };

            KittingProgress progress = KittingProgress.From(items, states);

            Assert.That((progress.Placed, progress.ToPlace), Is.EqualTo((2, 2)));
        }

        [Test]
        public void WhenTheStatesAreShorterThanTheItems_TheMissingOnesCountAsNotPlaced()
        {
            var items = new List<KittingItem> { Item(1), Item(2), Item(3) };
            var states = new List<KittingItemState> { KittingItemState.Placed };

            KittingProgress progress = KittingProgress.From(items, states);

            Assert.That((progress.Placed, progress.ToPlace), Is.EqualTo((1, 3)));
        }

        [Test]
        public void NoItems_MeansNothingPlacedAndNothingToPlace()
        {
            KittingProgress progress = KittingProgress.From(new List<KittingItem>(), new List<KittingItemState>());

            Assert.That((progress.Placed, progress.ToPlace), Is.EqualTo((0, 0)));
        }
    }
}
