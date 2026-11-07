using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class BoxPivotTests
    {
        [Test]
        public void BottomCenter_UsesTheRootForTheHorizontalPosition_AndTheLowestPointForTheHeight()
        {
            var root = new Vector3(1f, 2f, 3f);
            var bounds = new Bounds(new Vector3(1.4f, 2f, 3.6f), new Vector3(0.1f, 0.1f, 0.24f));

            Vector3 pivot = BoxPivot.BottomCenter(root, bounds);

            Assert.That(pivot.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(pivot.y, Is.EqualTo(1.95f).Within(1e-5f));
            Assert.That(pivot.z, Is.EqualTo(3f).Within(1e-5f));
        }

        [Test]
        public void BottomCenter_IgnoresItemsSittingInsideTheBox_BecauseTheyOnlyRaiseTheTop()
        {
            var root = new Vector3(0f, 0.05f, 0f);
            var boxOnly = new Bounds(root, new Vector3(0.1f, 0.1f, 0.24f));
            Bounds withItems = boxOnly;
            withItems.Encapsulate(new Vector3(0f, 0.12f, 0f));

            Assert.That(BoxPivot.BottomCenter(root, withItems).y, Is.EqualTo(BoxPivot.BottomCenter(root, boxOnly).y).Within(1e-5f));
        }
    }
}
