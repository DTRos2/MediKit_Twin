using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmBaseStandPlannerTests
    {
        private const float PickStandoff = 0.4f;
        private const float PlaceStandoff = 0.5f;

        private static ArmBaseStandPlanner Create(Vector3? boxDirection = null)
        {
            return new ArmBaseStandPlanner(PickStandoff, PlaceStandoff, boxDirection ?? new Vector3(0f, 0f, 1f));
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
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArmBaseStandPlanner(-0.1f, 0.5f, Vector3.forward));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArmBaseStandPlanner(0.4f, -0.1f, Vector3.forward));
        }
    }
}
