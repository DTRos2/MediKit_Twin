using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ShelfDirectionTests
    {
        [Test]
        public void KeepsOnlyTheHorizontalPart_AndNormalizesIt()
        {
            Vector3 outward = ShelfDirection.Outward(new Vector3(0f, 5f, -3f));

            Assert.That(outward.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(outward.y, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(outward.z, Is.EqualTo(-1f).Within(1e-5f));
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(0f, 4f, 0f)]
        public void GoesInFromTheWorldFront_WhenTheDirectionIsUnknown(float x, float y, float z)
        {
            Assert.That(ShelfDirection.Outward(new Vector3(x, y, z)), Is.EqualTo(Vector3.back));
        }
    }
}
