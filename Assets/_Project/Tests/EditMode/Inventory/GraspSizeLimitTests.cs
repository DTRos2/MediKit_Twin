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
    }
}
