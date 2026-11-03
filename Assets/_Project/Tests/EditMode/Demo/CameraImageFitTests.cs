using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class CameraImageFitTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void WhenTheAreaHasTheSameShapeAsTheImage_NothingIsCropped()
        {
            Rect uv = CameraImageFit.VisibleUv(1280f, 720f, 320f, 180f);

            Assert.That((uv.x, uv.y, uv.width, uv.height), Is.EqualTo((0f, 0f, 1f, 1f)));
        }

        [Test]
        public void AWiderArea_CropsTheTopAndBottomEqually()
        {
            // 4:3 그림을 2:1 칸에 채우면 세로의 (4/3)/2 = 2/3만 보인다.
            Rect uv = CameraImageFit.VisibleUv(400f, 300f, 200f, 100f);

            Assert.That(uv.width, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(uv.height, Is.EqualTo(2f / 3f).Within(Tolerance));
            Assert.That(uv.y, Is.EqualTo(1f / 6f).Within(Tolerance), "위아래를 같게 자른다");
        }

        [Test]
        public void ATallerArea_CropsTheLeftAndRightEqually()
        {
            // 2:1 그림을 1:1 칸에 채우면 가로의 1/2만 보인다.
            Rect uv = CameraImageFit.VisibleUv(200f, 100f, 100f, 100f);

            Assert.That(uv.height, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(uv.width, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(uv.x, Is.EqualTo(0.25f).Within(Tolerance));
        }

        [TestCase(0f, 100f, 100f, 100f)]
        [TestCase(100f, 0f, 100f, 100f)]
        [TestCase(100f, 100f, 0f, 100f)]
        [TestCase(100f, 100f, 100f, 0f)]
        public void AnInvalidSize_ShowsTheWholeImage(float imageWidth, float imageHeight, float areaWidth, float areaHeight)
        {
            Rect uv = CameraImageFit.VisibleUv(imageWidth, imageHeight, areaWidth, areaHeight);

            Assert.That((uv.x, uv.y, uv.width, uv.height), Is.EqualTo((0f, 0f, 1f, 1f)));
        }

        [Test]
        public void WithNoCrop_TheViewportCornersLandOnTheAreaCorners()
        {
            var area = new Rect(10f, 20f, 200f, 100f);
            var whole = new Rect(0f, 0f, 1f, 1f);

            CameraImageFit.TryToGuiPoint(area, whole, new Vector2(0f, 0f), out Vector2 bottomLeft);
            CameraImageFit.TryToGuiPoint(area, whole, new Vector2(1f, 1f), out Vector2 topRight);

            Assert.That(bottomLeft, Is.EqualTo(new Vector2(10f, 120f)), "그림의 왼쪽 아래는 화면 좌표로 칸의 왼쪽 아래");
            Assert.That(topRight, Is.EqualTo(new Vector2(210f, 20f)));
        }

        [Test]
        public void WithACrop_ThePointIsMeasuredInsideTheVisiblePart()
        {
            // 위아래를 잘라 가운데 절반만 보이면, 그림 가운데(0.5, 0.5)는 칸 가운데에 온다.
            var area = new Rect(0f, 0f, 200f, 100f);
            var visible = new Rect(0f, 0.25f, 1f, 0.5f);

            bool isVisible = CameraImageFit.TryToGuiPoint(area, visible, new Vector2(0.5f, 0.5f), out Vector2 point);

            Assert.That(isVisible, Is.True);
            Assert.That(point, Is.EqualTo(new Vector2(100f, 50f)));
        }

        [Test]
        public void APointInTheCroppedAwayPart_IsNotVisible()
        {
            var area = new Rect(0f, 0f, 200f, 100f);
            var visible = new Rect(0f, 0.25f, 1f, 0.5f);

            Assert.That(CameraImageFit.TryToGuiPoint(area, visible, new Vector2(0.5f, 0.9f), out _), Is.False);
            Assert.That(CameraImageFit.TryToGuiPoint(area, visible, new Vector2(0.5f, 0.1f), out _), Is.False);
        }

        [Test]
        public void APointOnTheEdgeOfTheVisiblePart_IsStillVisible()
        {
            var area = new Rect(0f, 0f, 200f, 100f);
            var visible = new Rect(0f, 0.25f, 1f, 0.5f);

            Assert.That(CameraImageFit.TryToGuiPoint(area, visible, new Vector2(1f, 0.75f), out _), Is.True);
        }
    }
}
