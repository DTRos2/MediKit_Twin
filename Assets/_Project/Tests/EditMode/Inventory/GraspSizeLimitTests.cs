using System;
using NUnit.Framework;

namespace MediKitTwin.Inventory.Tests
{
    public sealed class GraspSizeLimitTests
    {
        // FR5 그리퍼: 손가락 간격 0.077m, 여유 비율 0.8 → 한 변 최대 약 0.0616m.
        private static GraspSizeLimit Limit() => new GraspSizeLimit(0.077f, 0.8f);

        [Test]
        public void MaxSize_IsOpeningWidthTimesClearance()
        {
            Assert.That(Limit().MaxSize, Is.EqualTo(0.0616f).Within(1e-5f));
        }

        [Test]
        public void ScaleFor_ItemAlreadySmallEnough_IsOne()
        {
            Assert.That(Limit().ScaleFor(0.04f, 0.03f, 0.05f), Is.EqualTo(1f));
        }

        [Test]
        public void ScaleFor_LongestSideExactlyAtLimit_IsOne()
        {
            var limit = Limit();

            Assert.That(limit.ScaleFor(limit.MaxSize, 0.01f, 0.01f), Is.EqualTo(1f));
        }

        [Test]
        public void ScaleFor_TooBigItem_ShrinksLongestSideToLimit()
        {
            var limit = Limit();

            float scale = limit.ScaleFor(0.22f, 0.10f, 0.14f);

            Assert.That(0.22f * scale, Is.EqualTo(limit.MaxSize).Within(1e-5f));
        }

        [Test]
        public void ScaleFor_KeepsProportionsBecauseOneScaleAppliesToAllSides()
        {
            float scale = Limit().ScaleFor(0.02f, 0.02f, 0.22f);

            Assert.That(scale, Is.LessThan(1f));
            Assert.That(0.02f * scale / (0.22f * scale), Is.EqualTo(0.02f / 0.22f).Within(1e-5f));
        }

        [Test]
        public void ScaleFor_AnyScaledItemFitsBetweenTheFingers()
        {
            var limit = Limit();
            var sizes = new[] { (0.2f, 0.06f, 0.2f), (0.12f, 0.06f, 0.12f), (0.08f, 0.2f, 0.08f), (0.22f, 0.1f, 0.14f) };

            foreach (var (w, h, d) in sizes)
            {
                float scale = limit.ScaleFor(w, h, d);
                Assert.That(Math.Max(w, Math.Max(h, d)) * scale, Is.LessThanOrEqualTo(limit.MaxSize + 1e-5f));
            }
        }

        [Test]
        public void ScaleFor_ZeroSizedItem_IsOne()
        {
            Assert.That(Limit().ScaleFor(0f, 0f, 0f), Is.EqualTo(1f));
        }

        [Test]
        public void Constructor_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GraspSizeLimit(0f, 0.8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GraspSizeLimit(0.077f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GraspSizeLimit(0.077f, 1.1f));
        }

        // 키트 박스 칸: 슬롯 간격 5.47cm - 칸막이 3mm - 양쪽 틈 2mm씩 = 4.76cm.
        private const float CellDiagonal = 0.0476f;

        [Test]
        public void FaceDiagonal_IsTheDiagonalOfTheTwoLongestSides()
        {
            // 가장 짧은 변(0.0071)은 빼고 0.0476, 0.0333으로 계산한다.
            Assert.That(GraspSizeLimit.FaceDiagonal(0.0476f, 0.0071f, 0.0333f), Is.EqualTo(0.05809f).Within(1e-4f));
            Assert.That(GraspSizeLimit.FaceDiagonal(0.0333f, 0.0476f, 0.0071f), Is.EqualTo(0.05809f).Within(1e-4f));
        }

        [Test]
        public void MaxFaceDiagonal_WithoutACellLimit_IsInfinite()
        {
            Assert.That(float.IsPositiveInfinity(Limit().MaxFaceDiagonal), Is.True);
        }

        [Test]
        public void WithFaceDiagonalCeiling_ShrinksAThinPlateWhoseFaceDoesNotFitTheCell()
        {
            GraspSizeLimit capped = Limit().WithFaceDiagonalCeiling(CellDiagonal);

            // 가장 긴 변 4.76cm는 칸 길이 이하지만 면의 대각선은 5.81cm라 칸 안에서 서면 낀다.
            float scale = capped.ScaleFor(0.0476f, 0.0071f, 0.0333f);

            Assert.That(scale, Is.LessThan(1f));
            Assert.That(GraspSizeLimit.FaceDiagonal(0.0476f * scale, 0.0071f * scale, 0.0333f * scale), Is.EqualTo(CellDiagonal).Within(1e-5f));
        }

        [Test]
        public void WithFaceDiagonalCeiling_ShrinksAnOriginalSizedPlateToTheCell()
        {
            GraspSizeLimit capped = Limit().WithFaceDiagonalCeiling(CellDiagonal);

            // 그리퍼 한도로 6.16cm까지 줄인 sample_bag(6.16 x 0.92 x 4.31cm)
            float scale = capped.ScaleFor(0.0616f, 0.0092f, 0.0431f);

            Assert.That(scale, Is.EqualTo(0.633f).Within(1e-3f));
        }

        [Test]
        public void WithFaceDiagonalCeiling_ItemThatAlreadyFits_IsUnchanged()
        {
            GraspSizeLimit capped = Limit().WithFaceDiagonalCeiling(CellDiagonal);

            Assert.That(capped.ScaleFor(0.03f, 0.01f, 0.03f), Is.EqualTo(1f));
        }

        [Test]
        public void WithFaceDiagonalCeiling_KeepsTheStricterOfGraspAndCell()
        {
            GraspSizeLimit capped = Limit().WithFaceDiagonalCeiling(CellDiagonal);

            // 가늘고 긴 막대(20cm): 그리퍼 한도 비율은 0.308, 칸 한도 비율은 0.238이다. 더 엄격한 쪽을 따른다.
            float scale = capped.ScaleFor(0.20f, 0.01f, 0.01f);

            Assert.That(scale, Is.LessThan(Limit().ScaleFor(0.20f, 0.01f, 0.01f)));
            Assert.That(scale, Is.EqualTo(CellDiagonal / GraspSizeLimit.FaceDiagonal(0.20f, 0.01f, 0.01f)).Within(1e-6f));
        }

        [Test]
        public void WithFaceDiagonalCeiling_DoesNotChangeTheGraspLimit()
        {
            GraspSizeLimit capped = Limit().WithFaceDiagonalCeiling(CellDiagonal);

            Assert.That(capped.MaxSize, Is.EqualTo(Limit().MaxSize).Within(1e-6f));
            Assert.That(capped.MaxFaceDiagonal, Is.EqualTo(CellDiagonal).Within(1e-6f));
        }

        [Test]
        public void WithFaceDiagonalCeiling_HigherThanTheCurrentLimit_KeepsIt()
        {
            GraspSizeLimit capped = Limit().WithFaceDiagonalCeiling(CellDiagonal).WithFaceDiagonalCeiling(0.5f);

            Assert.That(capped.MaxFaceDiagonal, Is.EqualTo(CellDiagonal).Within(1e-6f));
        }

        [Test]
        public void WithFaceDiagonalCeiling_DoesNotChangeTheOriginalLimit()
        {
            GraspSizeLimit original = Limit();

            original.WithFaceDiagonalCeiling(CellDiagonal);

            Assert.That(float.IsPositiveInfinity(original.MaxFaceDiagonal), Is.True);
        }

        [Test]
        public void WithFaceDiagonalCeiling_NonPositive_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Limit().WithFaceDiagonalCeiling(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Limit().WithFaceDiagonalCeiling(-0.01f));
        }
    }
}
