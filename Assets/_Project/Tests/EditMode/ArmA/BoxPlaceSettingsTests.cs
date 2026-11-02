using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BoxPlaceSettingsTests
    {
        private static BoxPlaceSettings With(Vector3 gripYawForward)
        {
            return new BoxPlaceSettings(0.15f, 0.2f, gripYawForward, new OrientationSearchSettings());
        }

        [Test]
        public void KeepsTheGivenHeights()
        {
            BoxPlaceSettings settings = With(Vector3.forward);

            Assert.That(settings.ApproachHeightM, Is.EqualTo(0.15f));
            Assert.That(settings.LiftHeightM, Is.EqualTo(0.2f));
        }

        [Test]
        public void WithoutReleaseHeights_TheItemIsReleasedAtTheSlot()
        {
            ReleaseHeights heights = With(Vector3.forward).ReleaseHeights;

            Assert.That(heights.Count, Is.EqualTo(1));
            Assert.That(heights[0], Is.EqualTo(0f));
        }

        [Test]
        public void KeepsTheGivenReleaseHeights_LowestFirst()
        {
            var settings = new BoxPlaceSettings(
                0.15f, 0.2f, Vector3.forward, new OrientationSearchSettings(), null, new ReleaseHeights(new[] { 0.11f, 0f, 0.04f }));

            Assert.That(settings.ReleaseHeights, Is.EqualTo(new[] { 0f, 0.04f, 0.11f }));
        }

        [Test]
        public void ADefaultSettings_StillHasTheSlotCenterAsReleaseHeight()
        {
            // default(struct)는 생성자를 거치지 않아 높이 후보가 비어 있다. 그래도 슬롯 중심 하나로 본다.
            BoxPlaceSettings settings = default;

            Assert.That(settings.ReleaseHeights.Count, Is.EqualTo(1));
        }

        [Test]
        public void NominalToolPointsStraightDown()
        {
            Quaternion rotation = With(Vector3.forward).NominalToolRotation();

            Assert.That((rotation * Vector3.forward).y, Is.EqualTo(-1f).Within(1e-5f));
        }

        [Test]
        public void TheFingerDirectionFollowsTheGripYawReference()
        {
            // 도구가 아래를 볼 때 위쪽 축이 기준 수평 방향을 향한다.
            Quaternion rotation = With(Vector3.right).NominalToolRotation();

            Vector3 up = rotation * Vector3.up;
            Assert.That(up.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(up.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void IgnoresTheHeightOfTheReference_AndFallsBackWhenItIsVertical()
        {
            Quaternion tilted = With(new Vector3(0f, 3f, 1f)).NominalToolRotation();
            Quaternion vertical = With(Vector3.up).NominalToolRotation();

            Assert.That((tilted * Vector3.up).z, Is.EqualTo(1f).Within(1e-5f));
            Assert.That((vertical * Vector3.up).z, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void WithoutARestingSearch_ItemsAreAlwaysPlacedFromAbove()
        {
            Assert.That(With(Vector3.forward).PlacesInRestingOrientation, Is.False);
        }

        [Test]
        public void WithARestingSearch_ItemsAreFirstPlacedInTheirRestingOrientation()
        {
            var settings = new BoxPlaceSettings(0.15f, 0.2f, Vector3.forward, new OrientationSearchSettings(), new OrientationSearchSettings());

            Assert.That(settings.PlacesInRestingOrientation, Is.True);
        }
    }
}
