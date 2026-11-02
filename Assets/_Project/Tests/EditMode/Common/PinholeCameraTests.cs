using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Common.Tests
{
    public sealed class PinholeCameraTests
    {
        // 세로 시야각 90도, 정사각형 화면: 앞으로 d만큼 간 곳에서 위아래 ±d, 좌우 ±d까지 보인다.
        private static readonly PinholeCamera Square = new PinholeCamera(90f, 1f, 0.1f, 5f);
        private static readonly Pose AtOriginLookingForward = new Pose(Vector3.zero, Quaternion.identity);

        [Test]
        public void APointStraightAhead_ProjectsToTheCenterOfTheScreen()
        {
            bool isVisible = Square.TryProject(AtOriginLookingForward, new Vector3(0f, 0f, 2f), out Vector2 viewport, out float depth);

            Assert.That(isVisible, Is.True);
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(depth, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void APointToTheRightAndAbove_LandsRightOfCenterAndAboveIt()
        {
            // 깊이 2에서 화면 절반 폭은 2. 오른쪽 1m는 화면 오른쪽 절반의 한가운데(0.75), 위쪽 1m는 위쪽 절반의 한가운데(0.75).
            bool isVisible = Square.TryProject(AtOriginLookingForward, new Vector3(1f, 1f, 2f), out Vector2 viewport, out _);

            Assert.That(isVisible, Is.True);
            Assert.That(viewport.x, Is.EqualTo(0.75f).Within(1e-4f));
            Assert.That(viewport.y, Is.EqualTo(0.75f).Within(1e-4f));
        }

        [Test]
        public void APointOutsideTheFieldOfView_IsNotVisible_ButStillGetsAScreenPosition()
        {
            bool isVisible = Square.TryProject(AtOriginLookingForward, new Vector3(3f, 0f, 2f), out Vector2 viewport, out _);

            Assert.That(isVisible, Is.False);
            Assert.That(viewport.x, Is.GreaterThan(1f));
        }

        [Test]
        public void APointBehindTheCamera_IsNotVisible()
        {
            Assert.That(Square.TryProject(AtOriginLookingForward, new Vector3(0f, 0f, -1f), out _, out _), Is.False);
        }

        [Test]
        public void APointNearerThanTheNearLimit_OrFartherThanTheFarLimit_IsNotVisible()
        {
            Assert.That(Square.TryProject(AtOriginLookingForward, new Vector3(0f, 0f, 0.05f), out _, out _), Is.False);
            Assert.That(Square.TryProject(AtOriginLookingForward, new Vector3(0f, 0f, 6f), out _, out _), Is.False);
        }

        [Test]
        public void ACameraThatIsTurned_SeesTheWorldInItsOwnForwardDirection()
        {
            var lookingRight = new Pose(Vector3.zero, Quaternion.LookRotation(Vector3.right, Vector3.up));

            Assert.That(Square.TryProject(lookingRight, new Vector3(2f, 0f, 0f), out Vector2 viewport, out _), Is.True);
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(Square.TryProject(lookingRight, new Vector3(0f, 0f, 2f), out _, out _), Is.False, "정면 앞쪽은 이제 카메라의 왼쪽 옆이다");
        }

        [Test]
        public void AWideScreen_SeesFurtherSidewaysThanItDoesUpward()
        {
            var wide = new PinholeCamera(90f, 2f, 0.1f, 5f);

            Assert.That(wide.TryProject(AtOriginLookingForward, new Vector3(1.5f, 0f, 1f), out _, out _), Is.True);
            Assert.That(wide.TryProject(AtOriginLookingForward, new Vector3(0f, 1.5f, 1f), out _, out _), Is.False);
        }
    }
}
