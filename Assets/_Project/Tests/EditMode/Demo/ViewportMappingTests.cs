using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ViewportMappingTests
    {
        private static readonly Rect Area = new Rect(100f, 50f, 320f, 240f);

        [Test]
        public void TheBottomLeftOfTheCameraView_IsTheBottomLeftCornerOfTheArea()
        {
            Vector2 point = ViewportMapping.ToGuiPoint(Area, new Vector2(0f, 0f));

            Assert.That((point.x, point.y), Is.EqualTo((100f, 290f)), "IMGUI는 위가 0이라 카메라 화면의 아래가 영역의 아래(yMax)에 닿는다");
        }

        [Test]
        public void TheTopRightOfTheCameraView_IsTheTopRightCornerOfTheArea()
        {
            Vector2 point = ViewportMapping.ToGuiPoint(Area, new Vector2(1f, 1f));

            Assert.That((point.x, point.y), Is.EqualTo((420f, 50f)));
        }

        [Test]
        public void TheCenterOfTheCameraView_IsTheCenterOfTheArea()
        {
            Vector2 point = ViewportMapping.ToGuiPoint(Area, new Vector2(0.5f, 0.5f));

            Assert.That(point, Is.EqualTo(Area.center));
        }
    }
}
