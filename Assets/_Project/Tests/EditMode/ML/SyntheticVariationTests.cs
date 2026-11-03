using System;
using NUnit.Framework;

namespace MediKitTwin.ML.Tests
{
    public sealed class SyntheticVariationTests
    {
        private static RandomizationRanges Ranges() => new RandomizationRanges(
            itemOffsetMeters: 0.006f, itemYawDegrees: 20f,
            boxOffsetMeters: 0.01f, boxYawDegrees: 5f,
            lightIntensityDeviation: 0.3f, lightYawDegrees: 25f, lightPitchDegrees: 10f,
            hueShift: 0.03f, saturationDeviation: 0.15f, valueDeviation: 0.15f);

        [Test]
        public void SameSeed_GivesSameValues()
        {
            var a = SyntheticVariation.Create(11, Ranges(), 4);
            var b = SyntheticVariation.Create(11, Ranges(), 4);

            Assert.AreEqual(a.BoxYawDegrees, b.BoxYawDegrees);
            Assert.AreEqual(a.LightIntensityScale, b.LightIntensityScale);
            Assert.AreEqual(a.GetItemOffset(3), b.GetItemOffset(3));
            Assert.AreEqual(a.GetItemYawDegrees(4), b.GetItemYawDegrees(4));
        }

        [Test]
        public void DifferentSeed_GivesDifferentValues()
        {
            var a = SyntheticVariation.Create(11, Ranges(), 4);
            var b = SyntheticVariation.Create(12, Ranges(), 4);

            Assert.AreNotEqual(a.BoxYawDegrees, b.BoxYawDegrees);
        }

        [Test]
        public void AllValues_StayWithinRanges()
        {
            RandomizationRanges ranges = Ranges();
            for (int seed = 0; seed < 500; seed++)
            {
                var v = SyntheticVariation.Create(seed, ranges, 4);

                Assert.LessOrEqual(Math.Abs(v.BoxOffset.x), ranges.BoxOffsetMeters + 1e-6f);
                Assert.LessOrEqual(Math.Abs(v.BoxOffset.y), ranges.BoxOffsetMeters + 1e-6f);
                Assert.LessOrEqual(Math.Abs(v.BoxYawDegrees), ranges.BoxYawDegrees + 1e-4f);
                Assert.That(v.LightIntensityScale, Is.InRange(0.7f - 1e-4f, 1.3f + 1e-4f));
                Assert.LessOrEqual(Math.Abs(v.LightYawOffsetDegrees), ranges.LightYawDegrees + 1e-4f);
                Assert.LessOrEqual(Math.Abs(v.LightPitchOffsetDegrees), ranges.LightPitchDegrees + 1e-4f);
                Assert.LessOrEqual(Math.Abs(v.HueShift), ranges.HueShift + 1e-6f);
                Assert.That(v.SaturationScale, Is.InRange(0.85f - 1e-4f, 1.15f + 1e-4f));
                Assert.That(v.ValueScale, Is.InRange(0.85f - 1e-4f, 1.15f + 1e-4f));
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.LessOrEqual(Math.Abs(v.GetItemOffset(slot).x), ranges.ItemOffsetMeters + 1e-6f);
                    Assert.LessOrEqual(Math.Abs(v.GetItemOffset(slot).y), ranges.ItemOffsetMeters + 1e-6f);
                    Assert.LessOrEqual(Math.Abs(v.GetItemYawDegrees(slot)), ranges.ItemYawDegrees + 1e-4f);
                }
            }
        }

        [Test]
        public void ZeroRanges_ProduceNoVariation()
        {
            var none = new RandomizationRanges(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var v = SyntheticVariation.Create(3, none, 4);

            Assert.AreEqual(0f, v.BoxYawDegrees);
            Assert.AreEqual(1f, v.LightIntensityScale);
            Assert.AreEqual(1f, v.SaturationScale);
            Assert.AreEqual(0f, v.GetItemOffset(2).x);
            Assert.AreEqual(0f, v.GetItemYawDegrees(2));
        }

        [Test]
        public void ChangingOneRange_DoesNotChangeOtherValues()
        {
            RandomizationRanges full = Ranges();
            var noBoxYaw = new RandomizationRanges(
                full.ItemOffsetMeters, full.ItemYawDegrees, full.BoxOffsetMeters, 0f,
                full.LightIntensityDeviation, full.LightYawDegrees, full.LightPitchDegrees,
                full.HueShift, full.SaturationDeviation, full.ValueDeviation);

            var a = SyntheticVariation.Create(21, full, 4);
            var b = SyntheticVariation.Create(21, noBoxYaw, 4);

            Assert.AreEqual(0f, b.BoxYawDegrees);
            Assert.AreEqual(a.LightIntensityScale, b.LightIntensityScale);
            Assert.AreEqual(a.GetItemOffset(2), b.GetItemOffset(2));
        }

        [Test]
        public void LightWarmth_StaysInRange_AndDoesNotShiftOtherValues()
        {
            RandomizationRanges full = Ranges();
            var withWarmth = new RandomizationRanges(
                full.ItemOffsetMeters, full.ItemYawDegrees, full.BoxOffsetMeters, full.BoxYawDegrees,
                full.LightIntensityDeviation, full.LightYawDegrees, full.LightPitchDegrees,
                full.HueShift, full.SaturationDeviation, full.ValueDeviation, lightWarmthDeviation: 1f);

            for (int seed = 0; seed < 200; seed++)
            {
                var a = SyntheticVariation.Create(seed, full, 4);
                var b = SyntheticVariation.Create(seed, withWarmth, 4);

                Assert.AreEqual(0f, a.LightWarmth);
                Assert.That(b.LightWarmth, Is.InRange(-1f - 1e-5f, 1f + 1e-5f));
                Assert.AreEqual(a.LightIntensityScale, b.LightIntensityScale);
                Assert.AreEqual(a.GetItemYawDegrees(4), b.GetItemYawDegrees(4));
            }
        }

        [Test]
        public void ZeroSlotCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticVariation.Create(1, Ranges(), 0));
        }
    }
}
