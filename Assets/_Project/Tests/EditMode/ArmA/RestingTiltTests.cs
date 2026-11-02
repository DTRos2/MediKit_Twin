using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class RestingTiltTests
    {
        [Test]
        public void ItemPlacedTheSameWayAsOnTheShelfIsNotTilted()
        {
            Quaternion resting = Quaternion.Euler(-90f, 0f, 0f);

            Assert.That(RestingTilt.Degrees(resting, resting, Vector3.up), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void TurningTheItemAroundTheVerticalAxisIsNotATilt()
        {
            Quaternion resting = Quaternion.Euler(-90f, 0f, 0f);
            Quaternion turned = Quaternion.AngleAxis(137f, Vector3.up) * resting;

            Assert.That(RestingTilt.Degrees(resting, turned, Vector3.up), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void ALyingItemStandingOnItsEdgeIsTiltedNinetyDegrees()
        {
            Quaternion resting = Quaternion.identity;
            Quaternion onEdge = Quaternion.Euler(90f, 0f, 0f);

            Assert.That(RestingTilt.Degrees(resting, onEdge, Vector3.up), Is.EqualTo(90f).Within(1e-3f));
        }

        [Test]
        public void TurningTheItemUpsideDownIsTiltedOneHundredEightyDegrees()
        {
            Quaternion resting = Quaternion.Euler(-90f, 30f, 0f);
            Quaternion flipped = Quaternion.AngleAxis(180f, Vector3.right) * resting;

            Assert.That(RestingTilt.Degrees(resting, flipped, Vector3.up), Is.EqualTo(180f).Within(1e-3f));
        }
    }
}
