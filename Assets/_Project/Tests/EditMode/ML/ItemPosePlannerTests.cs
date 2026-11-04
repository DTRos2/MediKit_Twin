using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ML.Tests
{
    public sealed class ItemPosePlannerTests
    {
        // ShelfItemLooks의 크기(X 옆, Y 위, Z 앞).
        private static readonly Vector3 TestCassette = new Vector3(0.16f, 0.02f, 0.10f);
        private static readonly Vector3 SampleBag = new Vector3(0.20f, 0.03f, 0.14f);
        private static readonly Vector3 AlcoholPad = new Vector3(0.12f, 0.02f, 0.12f);
        private static readonly Vector3 Swab = new Vector3(0.02f, 0.02f, 0.22f);
        private static readonly Vector3 Needle = new Vector3(0.04f, 0.04f, 0.20f);
        private static readonly Vector3 Gauze = new Vector3(0.20f, 0.06f, 0.20f);
        private static readonly Vector3 Gloves = new Vector3(0.22f, 0.10f, 0.14f);

        [Test]
        public void Classify_ThinFlatItems_ArePlates()
        {
            Assert.AreEqual(ItemFlatness.Plate, ItemPosePlanner.Classify(TestCassette));
            Assert.AreEqual(ItemFlatness.Plate, ItemPosePlanner.Classify(SampleBag));
            Assert.AreEqual(ItemFlatness.Plate, ItemPosePlanner.Classify(AlcoholPad));
        }

        [Test]
        public void Classify_ThinLongItems_AreRods()
        {
            Assert.AreEqual(ItemFlatness.Rod, ItemPosePlanner.Classify(Swab));
            Assert.AreEqual(ItemFlatness.Rod, ItemPosePlanner.Classify(Needle));
        }

        [Test]
        public void Classify_ThickPlatesAndBoxes_AreBlocks()
        {
            // 거즈는 정사각형이라, 판처럼 마음대로 돌리면 직사각형과 구별하기 어려워진다.
            Assert.AreEqual(ItemFlatness.Block, ItemPosePlanner.Classify(Gauze));
            Assert.AreEqual(ItemFlatness.Block, ItemPosePlanner.Classify(Gloves));
        }

        [Test]
        public void Classify_IgnoresUniformScale()
        {
            Assert.AreEqual(ItemFlatness.Plate, ItemPosePlanner.Classify(TestCassette * 0.5f));
            Assert.AreEqual(ItemFlatness.Rod, ItemPosePlanner.Classify(Swab * 0.5f));
        }

        [Test]
        public void FlatOnTheFloor_RestsAtHalfThickness()
        {
            ItemPose pose = ItemPosePlanner.Plan(TestCassette, ItemFlatness.Plate, 0f, 0f, 0f, false, 1f);

            Assert.That(pose.CenterHeight, Is.EqualTo(0.01f).Within(1e-5f));
            Assert.That(Quaternion.Angle(pose.LocalRotation, Quaternion.identity), Is.LessThan(1e-3f));
        }

        [Test]
        public void TiltedPlate_RestsHigherThanFlat_AndTheLowestPointTouchesTheFloor()
        {
            ItemPose pose = ItemPosePlanner.Plan(TestCassette, ItemFlatness.Plate, 0f, 25f, 0f, false, 1f);

            Assert.Greater(pose.CenterHeight, 0.01f);
            Assert.That(LowestPointHeight(pose, TestCassette), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void EdgeStandingPlate_StandsOnItsLongEdge()
        {
            // 시험 카세트는 X(16cm)가 긴 변이라 그 모서리로 서고, 높이는 짧은 변(Z, 10cm)이다.
            ItemPose pose = ItemPosePlanner.Plan(TestCassette, ItemFlatness.Plate, 0f, 0f, 0f, true, 1f);

            Assert.That(pose.CenterHeight, Is.EqualTo(0.05f).Within(1e-5f));
            Assert.That(LowestPointHeight(pose, TestCassette), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void EdgeStandingPlate_UsesTheLongEdge_WhenZIsTheLongerSide()
        {
            var tall = new Vector3(0.10f, 0.02f, 0.16f);
            ItemPose pose = ItemPosePlanner.Plan(tall, ItemFlatness.Plate, 0f, 0f, 0f, true, -1f);

            Assert.That(pose.CenterHeight, Is.EqualTo(0.05f).Within(1e-5f));
        }

        [Test]
        public void EdgeStand_IsIgnoredForRods_AndForBlocks()
        {
            ItemPose rod = ItemPosePlanner.Plan(Swab, ItemFlatness.Rod, 0f, 0f, 0f, true, 1f);
            ItemPose block = ItemPosePlanner.Plan(Gauze, ItemFlatness.Block, 0f, 10f, 10f, true, 1f);

            Assert.That(rod.CenterHeight, Is.EqualTo(0.01f).Within(1e-5f));
            Assert.That(block.CenterHeight, Is.EqualTo(0.03f).Within(1e-5f), "두꺼운 판은 기울지도 서지도 않는다");
        }

        [Test]
        public void TiltedRod_LiftsOneEnd_AndTouchesTheFloor()
        {
            ItemPose pose = ItemPosePlanner.Plan(Swab, ItemFlatness.Rod, 0f, 20f, 15f, false, 1f);

            Assert.Greater(pose.CenterHeight, 0.01f);
            Assert.That(LowestPointHeight(pose, Swab), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Yaw_TurnsTheItemInPlace_WithoutChangingItsHeight()
        {
            ItemPose a = ItemPosePlanner.Plan(SampleBag, ItemFlatness.Plate, 0f, 15f, 5f, false, 1f);
            ItemPose b = ItemPosePlanner.Plan(SampleBag, ItemFlatness.Plate, 40f, 15f, 5f, false, 1f);

            Assert.That(b.CenterHeight, Is.EqualTo(a.CenterHeight).Within(1e-5f));
        }

        // 회전한 직육면체의 가장 낮은 모서리 높이(중심 높이 기준으로 바닥을 0으로 둔다).
        private static float LowestPointHeight(ItemPose pose, Vector3 size)
        {
            float lowest = float.MaxValue;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 corner = new Vector3(sx * size.x, sy * size.y, sz * size.z) * 0.5f;
                        lowest = Mathf.Min(lowest, (pose.LocalRotation * corner).y);
                    }
                }
            }

            return pose.CenterHeight + lowest;
        }
    }
}
