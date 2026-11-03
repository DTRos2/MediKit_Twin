using System;
using NUnit.Framework;

namespace MediKitTwin.Inventory.Tests
{
    public sealed class KitBoxPartitionLayoutTests
    {
        // 레시피의 슬롯 좌표(박스 긴 변 방향). 세 레시피가 같은 값을 쓴다.
        private static readonly float[] RecipeSlots = { 0.082f, 0.0273f, -0.0273f, -0.082f };

        private static KitBoxPartitionLayout Layout(float thickness = 0.003f, float gap = 0.002f) =>
            new KitBoxPartitionLayout(RecipeSlots, thickness, gap);

        [Test]
        public void DividerCenters_AreBetweenNeighbouringSlots_SmallestFirst()
        {
            var layout = Layout();

            Assert.That(layout.DividerCenters.Count, Is.EqualTo(3));
            Assert.That(layout.DividerCenters[0], Is.EqualTo(-0.05465f).Within(1e-5f));
            Assert.That(layout.DividerCenters[1], Is.EqualTo(0f).Within(1e-5f));
            Assert.That(layout.DividerCenters[2], Is.EqualTo(0.05465f).Within(1e-5f));
        }

        [Test]
        public void SlotCenters_AreSortedAscending()
        {
            var layout = Layout();

            Assert.That(layout.SlotCenters, Is.EqualTo(new[] { -0.082f, -0.0273f, 0.0273f, 0.082f }));
        }

        [Test]
        public void CellLength_IsNarrowestPitchMinusThickness()
        {
            // 가장 좁은 간격은 가운데 두 슬롯 사이 0.0546m.
            Assert.That(Layout(thickness: 0.003f).CellLength, Is.EqualTo(0.0516f).Within(1e-5f));
        }

        [Test]
        public void MaxItemFaceDiagonal_LeavesAGapOnBothSides()
        {
            // 0.0516 - 2 x 0.002 = 0.0476
            Assert.That(Layout(thickness: 0.003f, gap: 0.002f).MaxItemFaceDiagonal, Is.EqualTo(0.0476f).Within(1e-5f));
        }

        [Test]
        public void ThickerDividers_MakeTheCellsShorter()
        {
            Assert.That(Layout(thickness: 0.006f).MaxItemFaceDiagonal, Is.LessThan(Layout(thickness: 0.003f).MaxItemFaceDiagonal));
        }

        [Test]
        public void DuplicateSlotCoordinates_FromSeveralRecipes_AreMerged()
        {
            // 레시피 셋이 같은 슬롯 좌표를 부동소수점으로 따로 적은 경우
            var slots = new[] { 0.082f, 0.082f, 0.0273f, 0.0273f, 0.08200001f, -0.0273f, -0.082f, -0.0273f };

            var layout = new KitBoxPartitionLayout(slots, 0.003f, 0.002f);

            Assert.That(layout.SlotCenters.Count, Is.EqualTo(4));
            Assert.That(layout.DividerCenters.Count, Is.EqualTo(3));
        }

        [Test]
        public void SingleSlot_HasNoDividersAndNoSizeLimit()
        {
            var layout = new KitBoxPartitionLayout(new[] { 0f }, 0.003f, 0.002f);

            Assert.That(layout.DividerCenters, Is.Empty);
            Assert.That(float.IsPositiveInfinity(layout.MaxItemFaceDiagonal), Is.True);
        }

        [Test]
        public void NoSlots_HasNoDividers()
        {
            var layout = new KitBoxPartitionLayout(new float[0], 0.003f, 0.002f);

            Assert.That(layout.DividerCenters, Is.Empty);
        }

        [Test]
        public void UnevenSlotPitch_PutsEachDividerHalfwayAndLimitsByTheNarrowestCell()
        {
            var layout = new KitBoxPartitionLayout(new[] { 0f, 0.04f, 0.10f }, 0.002f, 0f);

            Assert.That(layout.DividerCenters[0], Is.EqualTo(0.02f).Within(1e-6f));
            Assert.That(layout.DividerCenters[1], Is.EqualTo(0.07f).Within(1e-6f));
            Assert.That(layout.CellLength, Is.EqualTo(0.038f).Within(1e-6f));
        }

        [Test]
        public void CellTooNarrowForAnyItem_Throws()
        {
            // 간격 0.0546 - 두께 0.003 = 0.0516인데 양쪽 틈이 0.03씩이면 물건이 들어갈 자리가 없다.
            Assert.Throws<ArgumentException>(() => Layout(thickness: 0.003f, gap: 0.03f));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new KitBoxPartitionLayout(null, 0.003f, 0.002f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Layout(thickness: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Layout(gap: -0.001f));
        }
    }
}
