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

        private static RandomizationRanges WithLying(float chance, float yawDegrees)
        {
            RandomizationRanges full = Ranges();
            return new RandomizationRanges(
                full.ItemOffsetMeters, full.ItemYawDegrees, full.BoxOffsetMeters, full.BoxYawDegrees,
                full.LightIntensityDeviation, full.LightYawDegrees, full.LightPitchDegrees,
                full.HueShift, full.SaturationDeviation, full.ValueDeviation,
                itemLyingChance: chance, itemLyingYawDegrees: yawDegrees);
        }

        [Test]
        public void WithoutLyingChance_NoItemLies()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var v = SyntheticVariation.Create(seed, Ranges(), 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.IsFalse(v.IsItemLying(slot));
                }
            }
        }

        [Test]
        public void LyingChanceOne_MakesEveryItemLie_WithYawInTheLyingRange()
        {
            RandomizationRanges ranges = WithLying(1f, 90f);
            for (int seed = 0; seed < 200; seed++)
            {
                var v = SyntheticVariation.Create(seed, ranges, 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.IsTrue(v.IsItemLying(slot));
                    Assert.LessOrEqual(Math.Abs(v.GetItemLyingYawDegrees(slot)), 90f + 1e-4f);
                }
            }
        }

        [Test]
        public void LyingItems_AreAboutTheGivenShareOfAllItems()
        {
            RandomizationRanges ranges = WithLying(0.85f, 90f);
            int lying = 0;
            const int boxes = 2000;
            for (int seed = 0; seed < boxes; seed++)
            {
                var v = SyntheticVariation.Create(seed, ranges, 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    lying += v.IsItemLying(slot) ? 1 : 0;
                }
            }

            Assert.That(lying / (boxes * 4f), Is.EqualTo(0.85f).Within(0.02f));
        }

        [Test]
        public void ItemYaw_StaysInTheNormalRange_EvenWhenTheItemIsDrawnToLie()
        {
            // 눕는 확률에 걸린 구성품도 서 있을 때의 각도는 그대로다. 눕지 않는 판과 상자가 ±90도로 돌면 직사각형을 거즈와 구별하기 어렵다.
            RandomizationRanges ranges = WithLying(1f, 90f);
            for (int seed = 0; seed < 300; seed++)
            {
                var v = SyntheticVariation.Create(seed, ranges, 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.LessOrEqual(Math.Abs(v.GetItemYawDegrees(slot)), ranges.ItemYawDegrees + 1e-4f);
                }
            }
        }

        [Test]
        public void LyingYaw_UsesTheWiderRange()
        {
            RandomizationRanges ranges = WithLying(1f, 90f);
            float widest = 0f;
            for (int seed = 0; seed < 300; seed++)
            {
                var v = SyntheticVariation.Create(seed, ranges, 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    widest = Math.Max(widest, Math.Abs(v.GetItemLyingYawDegrees(slot)));
                }
            }

            Assert.Greater(widest, ranges.ItemYawDegrees * 2f, "누운 각도가 서 있을 때의 범위보다 넓게 퍼져야 한다");
        }

        [Test]
        public void LyingChance_DoesNotShiftTheOtherValues()
        {
            // 학습 데이터를 다시 만들어도 조명, 박스, 구성품 위치는 같은 시드에서 같게 나와야 한다. (NFR-06)
            for (int seed = 0; seed < 100; seed++)
            {
                var standing = SyntheticVariation.Create(seed, Ranges(), 4);
                var lying = SyntheticVariation.Create(seed, WithLying(0.85f, 90f), 4);

                Assert.AreEqual(standing.BoxYawDegrees, lying.BoxYawDegrees);
                Assert.AreEqual(standing.LightIntensityScale, lying.LightIntensityScale);
                Assert.AreEqual(standing.LightWarmth, lying.LightWarmth);
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.AreEqual(standing.GetItemOffset(slot), lying.GetItemOffset(slot));
                }
            }
        }

        private static RandomizationRanges WithTilt(float tiltDegrees, float edgeStandChance, float flatYawDegrees)
        {
            RandomizationRanges full = Ranges();
            return new RandomizationRanges(
                full.ItemOffsetMeters, full.ItemYawDegrees, full.BoxOffsetMeters, full.BoxYawDegrees,
                full.LightIntensityDeviation, full.LightYawDegrees, full.LightPitchDegrees,
                full.HueShift, full.SaturationDeviation, full.ValueDeviation,
                itemTiltDegrees: tiltDegrees, itemEdgeStandChance: edgeStandChance, itemFlatYawDegrees: flatYawDegrees);
        }

        [Test]
        public void WithoutTiltRanges_ItemsStayFlat_AndFlatYawFollowsTheNormalYaw()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var v = SyntheticVariation.Create(seed, Ranges(), 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.AreEqual(0f, v.GetItemTiltPitchDegrees(slot));
                    Assert.AreEqual(0f, v.GetItemTiltRollDegrees(slot));
                    Assert.IsFalse(v.IsItemOnEdge(slot));
                    Assert.AreEqual(v.GetItemYawDegrees(slot), v.GetItemFlatYawDegrees(slot));
                }
            }
        }

        [Test]
        public void TiltAndFlatYaw_StayWithinTheirRanges()
        {
            RandomizationRanges ranges = WithTilt(25f, 0.2f, 45f);
            float widestYaw = 0f;
            for (int seed = 0; seed < 500; seed++)
            {
                var v = SyntheticVariation.Create(seed, ranges, 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.LessOrEqual(Math.Abs(v.GetItemTiltPitchDegrees(slot)), 25f + 1e-4f);
                    Assert.LessOrEqual(Math.Abs(v.GetItemTiltRollDegrees(slot)), 25f + 1e-4f);
                    Assert.LessOrEqual(Math.Abs(v.GetItemFlatYawDegrees(slot)), 45f + 1e-4f);
                    Assert.That(Math.Abs(v.GetItemEdgeSign(slot)), Is.EqualTo(1f));
                    widestYaw = Math.Max(widestYaw, Math.Abs(v.GetItemFlatYawDegrees(slot)));
                }
            }

            Assert.Greater(widestYaw, ranges.ItemYawDegrees * 1.5f, "판과 막대의 각도가 일반 범위보다 넓게 퍼져야 한다");
        }

        [Test]
        public void EdgeStandChance_GivesAboutTheGivenShare()
        {
            RandomizationRanges ranges = WithTilt(25f, 0.2f, 45f);
            int onEdge = 0;
            const int boxes = 2000;
            for (int seed = 0; seed < boxes; seed++)
            {
                var v = SyntheticVariation.Create(seed, ranges, 4);
                for (int slot = 1; slot <= 4; slot++)
                {
                    onEdge += v.IsItemOnEdge(slot) ? 1 : 0;
                }
            }

            Assert.That(onEdge / (boxes * 4f), Is.EqualTo(0.2f).Within(0.02f));
        }

        [Test]
        public void TiltRanges_DoNotShiftTheValuesDrawnBefore()
        {
            // 학습 데이터를 다시 만들어도 조명, 박스, 구성품 위치, 눕는 자세는 같은 시드에서 같게 나와야 한다. (NFR-06)
            for (int seed = 0; seed < 100; seed++)
            {
                var flat = SyntheticVariation.Create(seed, WithLying(0.85f, 90f), 4);
                RandomizationRanges full = WithLying(0.85f, 90f);
                var tilted = SyntheticVariation.Create(seed, new RandomizationRanges(
                    full.ItemOffsetMeters, full.ItemYawDegrees, full.BoxOffsetMeters, full.BoxYawDegrees,
                    full.LightIntensityDeviation, full.LightYawDegrees, full.LightPitchDegrees,
                    full.HueShift, full.SaturationDeviation, full.ValueDeviation,
                    itemLyingChance: full.ItemLyingChance, itemLyingYawDegrees: full.ItemLyingYawDegrees,
                    itemTiltDegrees: 25f, itemEdgeStandChance: 0.2f, itemFlatYawDegrees: 45f), 4);

                Assert.AreEqual(flat.BoxYawDegrees, tilted.BoxYawDegrees);
                Assert.AreEqual(flat.LightWarmth, tilted.LightWarmth);
                for (int slot = 1; slot <= 4; slot++)
                {
                    Assert.AreEqual(flat.GetItemOffset(slot), tilted.GetItemOffset(slot));
                    Assert.AreEqual(flat.GetItemYawDegrees(slot), tilted.GetItemYawDegrees(slot));
                    Assert.AreEqual(flat.IsItemLying(slot), tilted.IsItemLying(slot));
                    Assert.AreEqual(flat.GetItemLyingYawDegrees(slot), tilted.GetItemLyingYawDegrees(slot));
                }
            }
        }

        [Test]
        public void ZeroSlotCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticVariation.Create(1, Ranges(), 0));
        }
    }
}
