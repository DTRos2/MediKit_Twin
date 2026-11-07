using System;
using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.Inventory.Tests
{
    public sealed class ShelfCellLayoutTests
    {
        // 선반 칸 안쪽 0.48 x 0.46 m, 뒤쪽 벽 때문에 깊이 중심이 0.02 앞쪽으로 치우친 칸.
        private static ShelfCellLayout Layout(float fillRatio = 0.9f) => new ShelfCellLayout(0.48f, 0.46f, 0.02f, 3, fillRatio);

        [Test]
        public void Compute_ZeroOrNegativeCount_ReturnsEmpty()
        {
            Assert.That(Layout().Compute(0, 0.1f, 0.1f), Is.Empty);
            Assert.That(Layout().Compute(-1, 0.1f, 0.1f), Is.Empty);
        }

        [Test]
        public void Compute_ReturnsOnePlacementPerItem()
        {
            Assert.That(Layout().Compute(5, 0.1f, 0.1f).Count, Is.EqualTo(5));
        }

        [Test]
        public void Compute_SingleItem_IsCenteredAcrossWidth()
        {
            var placement = Layout().Compute(1, 0.1f, 0.1f)[0];

            Assert.That(placement.X, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Compute_FiveItems_FillsFirstRowOfThreeThenTwo()
        {
            var placements = Layout().Compute(5, 0.05f, 0.05f);

            var rowZ = placements.Select(p => p.Z).Distinct().OrderByDescending(z => z).ToList();
            Assert.That(rowZ.Count, Is.EqualTo(2));
            Assert.That(placements.Count(p => p.Z == rowZ[0]), Is.EqualTo(3));
            Assert.That(placements.Count(p => p.Z == rowZ[1]), Is.EqualTo(2));
        }

        [Test]
        public void Compute_FirstSlotsAreTheFrontRow()
        {
            var placements = Layout().Compute(5, 0.05f, 0.05f);

            // 앞쪽(+Z, 열린 쪽) 줄부터 채우므로 앞 3개가 뒤 2개보다 Z가 크다.
            Assert.That(placements[0].Z, Is.GreaterThan(placements[3].Z));
        }

        [Test]
        public void Compute_NoTwoItemsShareAPosition()
        {
            var placements = Layout().Compute(5, 0.05f, 0.05f);

            Assert.That(placements.Select(p => (p.X, p.Z)).Distinct().Count(), Is.EqualTo(5));
        }

        [Test]
        public void Compute_AllItemsStayInsideTheCell()
        {
            var placements = Layout().Compute(5, 0.05f, 0.05f);

            Assert.That(placements.All(p => Math.Abs(p.X) <= 0.48f / 2f), Is.True);
            Assert.That(placements.All(p => p.Z >= 0.02f - 0.46f / 2f && p.Z <= 0.02f + 0.46f / 2f), Is.True);
        }

        [Test]
        public void Compute_SmallItem_KeepsOriginalSize()
        {
            Assert.That(Layout().Compute(5, 0.05f, 0.05f).All(p => p.Scale == 1f), Is.True);
        }

        [Test]
        public void Compute_ItemWiderThanItsSlot_IsShrunkToFit()
        {
            // 한 줄 3개이면 슬롯 가로는 0.16m이므로 0.22m 폭의 구성품은 줄여야 한다.
            var placements = Layout().Compute(5, 0.22f, 0.14f);

            Assert.That(placements.All(p => p.Scale < 1f), Is.True);
            Assert.That(placements[0].Scale * 0.22f, Is.LessThanOrEqualTo(0.48f / 3f));
        }

        [Test]
        public void Compute_LowerFillRatio_ShrinksItemsMoreToLeaveRoomBetweenThem()
        {
            float full = Layout(1f).Compute(5, 0.22f, 0.14f)[0].Scale;
            float roomy = Layout(0.5f).Compute(5, 0.22f, 0.14f)[0].Scale;

            Assert.That(roomy, Is.LessThan(full));
            Assert.That(roomy, Is.EqualTo(full * 0.5f).Within(1e-5f));
        }

        [Test]
        public void Compute_SameScaleForEveryItemOfOneCell()
        {
            var scales = Layout().Compute(5, 0.22f, 0.14f).Select(p => p.Scale).Distinct();

            Assert.That(scales.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Constructor_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfCellLayout(0f, 0.4f, 0f, 3, 0.9f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfCellLayout(0.4f, 0f, 0f, 3, 0.9f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfCellLayout(0.4f, 0.4f, 0f, 0, 0.9f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfCellLayout(0.4f, 0.4f, 0f, 3, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfCellLayout(0.4f, 0.4f, 0f, 3, 1.1f));
        }
    }
}
