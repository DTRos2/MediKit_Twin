using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmBaseStandPlannerTests
    {
        private const float PickStandoff = 0.4f;
        private const float PlaceStandoff = 0.5f;
        private const float ApproachExtra = 0.3f;

        private static ArmBaseStandPlanner Create(Vector3? boxDirection = null)
        {
            return new ArmBaseStandPlanner(PickStandoff, PlaceStandoff, ApproachExtra, boxDirection ?? new Vector3(0f, 0f, 1f));
        }

        [Test]
        public void ShelfItemFacingYaw_LooksAtTheItem_AndAddsTheMountOffset()
        {
            // 칸에서 -Z 쪽으로 나오므로 선 자리에서 칸을 보려면 +Z(0도)를 봐야 한다.
            float? facing = ArmBaseStandPlanner.ShelfItemFacingYawDeg(new Vector3(0f, 0f, -1f));
            float? offset = ArmBaseStandPlanner.ShelfItemFacingYawDeg(new Vector3(0f, 0f, -1f), 30f);

            Assert.That(facing, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(offset, Is.EqualTo(30f).Within(1e-3f));
        }

        [Test]
        public void ShelfItemFacingYaw_WithNoDirection_IsUnknown()
        {
            Assert.That(ArmBaseStandPlanner.ShelfItemFacingYawDeg(Vector3.zero), Is.Null);
        }

        [Test]
        public void BoxSlotFacingYaw_LooksBackAlongTheStandDirection()
        {
            // 슬롯의 +Z 쪽에 서므로 슬롯을 보려면 -Z(180도)를 봐야 한다.
            float? facing = Create().BoxSlotFacingYawDeg();

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(facing.Value, 180f)), Is.LessThan(1e-3f));
        }

        [Test]
        public void ForShelfItem_ExtraStandoff_StandsFurtherOutOnTheSameLine()
        {
            var item = new Vector3(1f, 1.2f, 2.6f);
            var outward = new Vector3(0f, 0f, -1f);

            Vector3 basic = Create().ForShelfItem(item, outward);
            Vector3 retried = Create().ForShelfItem(item, outward, 0.1f);

            Assert.That(retried.x, Is.EqualTo(basic.x).Within(1e-5f));
            Assert.That(Vector3.Distance(retried, basic), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(retried.z, Is.LessThan(basic.z), "칸에서 더 멀어진다");
        }

        [Test]
        public void ForShelfItemApproach_ExtraStandoff_KeepsTheWaypointBehindTheStandPoint()
        {
            var item = new Vector3(1f, 1.2f, 2.6f);
            var outward = new Vector3(0f, 0f, -1f);

            Vector3 stand = Create().ForShelfItem(item, outward, 0.2f);
            Vector3 approach = Create().ForShelfItemApproach(item, outward, 0.2f);

            Assert.That(Vector3.Distance(approach, stand), Is.EqualTo(ApproachExtra).Within(1e-5f));
        }

        [Test]
        public void ForShelfItemApproach_IsOnTheSameLineAsTheStandPoint_FurtherOutByTheApproachExtra()
        {
            var item = new Vector3(1f, 1.2f, 2.6f);
            var outward = new Vector3(0f, 0f, -1f);

            Vector3 stand = Create().ForShelfItem(item, outward);
            Vector3 approach = Create().ForShelfItemApproach(item, outward);

            Assert.That(approach.x, Is.EqualTo(stand.x).Within(1e-5f));
            Assert.That(approach.y, Is.EqualTo(0f));
            Assert.That(Vector3.Distance(approach, stand), Is.EqualTo(ApproachExtra).Within(1e-5f));
        }

        [Test]
        public void ForShelfItem_StandsOutwardFromTheItemByThePickStandoff()
        {
            Vector3 stand = Create().ForShelfItem(new Vector3(1f, 1.2f, 2.6f), new Vector3(0f, 0f, -1f));

            Assert.That(stand.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(stand.z, Is.EqualTo(2.2f).Within(1e-5f));
        }

        [Test]
        public void ForShelfItem_ReturnsAFloorPoint()
        {
            Vector3 stand = Create().ForShelfItem(new Vector3(1f, 1.2f, 2.6f), new Vector3(0f, 0f, -1f));

            Assert.That(stand.y, Is.EqualTo(0f));
        }

        [Test]
        public void ForShelfItem_IgnoresTheVerticalPartAndNormalizesTheDirection()
        {
            Vector3 stand = Create().ForShelfItem(Vector3.zero, new Vector3(3f, 5f, 0f));

            Assert.That(stand.x, Is.EqualTo(PickStandoff).Within(1e-5f));
            Assert.That(stand.z, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void ForShelfItem_ZeroDirection_StaysAtTheItem()
        {
            Vector3 stand = Create().ForShelfItem(new Vector3(1f, 1f, 1f), Vector3.zero);

            Assert.That(stand.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(stand.z, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void ForBoxSlot_StandsOnTheConfiguredSideByThePlaceStandoff()
        {
            Vector3 stand = Create(new Vector3(0f, 0f, 1f)).ForBoxSlot(new Vector3(1.5f, 0.1f, 0f));

            Assert.That(stand.x, Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(stand.z, Is.EqualTo(PlaceStandoff).Within(1e-5f));
        }

        [Test]
        public void Constructor_NegativeStandoff_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArmBaseStandPlanner(-0.1f, 0.5f, ApproachExtra, Vector3.forward));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArmBaseStandPlanner(0.4f, -0.1f, ApproachExtra, Vector3.forward));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArmBaseStandPlanner(0.4f, 0.5f, -0.1f, Vector3.forward));
        }
    }
}
