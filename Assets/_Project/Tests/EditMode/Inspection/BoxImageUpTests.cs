using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class BoxImageUpTests
    {
        private static readonly Vector3 MountUp = Vector3.forward;

        [Test]
        public void ABoxFacingForward_GivesForward()
        {
            Assert.That(BoxImageUp.From(Vector3.forward, MountUp), Is.EqualTo(Vector3.forward));
        }

        [TestCase(90f)]
        [TestCase(-90f)]
        [TestCase(180f)]
        [TestCase(37f)]
        public void ATurnedBox_GivesItsOwnForward_SoTheCropsFollowTheBox(float yawDegrees)
        {
            Vector3 boxForward = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;

            Vector3 up = BoxImageUp.From(boxForward, MountUp);

            Assert.That(Vector3.Angle(up, boxForward), Is.LessThan(0.01f));
        }

        [Test]
        public void ATiltedBox_IsFlattenedOntoTheGround_AndNormalised()
        {
            Vector3 tilted = Quaternion.Euler(-20f, 90f, 0f) * Vector3.forward * 3f;

            Vector3 up = BoxImageUp.From(tilted, MountUp);

            Assert.That(up.y, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(up.magnitude, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Vector3.Angle(up, Vector3.right), Is.LessThan(0.01f));
        }

        [TestCase(0f, 1f, 0f)]
        [TestCase(0f, -1f, 0f)]
        [TestCase(0f, 0f, 0f)]
        public void WhenTheBoxForwardHasNoHorizontalDirection_TheFallbackIsUsed(float x, float y, float z)
        {
            Assert.That(BoxImageUp.From(new Vector3(x, y, z), Vector3.right), Is.EqualTo(Vector3.right));
        }
    }
}
