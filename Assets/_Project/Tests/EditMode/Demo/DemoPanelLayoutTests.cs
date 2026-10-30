using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class DemoPanelLayoutTests
    {
        private const float ScreenWidth = 1920f;
        private const float ScreenHeight = 1080f;

        private static Rect Place(DemoPanelAnchor anchor, float marginX, float marginY, float width, float height, float screenWidth = ScreenWidth)
        {
            return DemoPanelLayout.Place(anchor, new Vector2(marginX, marginY), new Vector2(width, height), screenWidth, ScreenHeight);
        }

        [Test]
        public void TopLeft_StartsAtTheMargin()
        {
            Rect rect = Place(DemoPanelAnchor.TopLeft, 10f, 20f, 300f, 200f);

            Assert.That((rect.x, rect.y, rect.width, rect.height), Is.EqualTo((10f, 20f, 300f, 200f)));
        }

        [Test]
        public void TopRight_SitsAgainstTheRightEdgeKeepingTheMargin()
        {
            Rect rect = Place(DemoPanelAnchor.TopRight, 10f, 20f, 300f, 200f);

            Assert.That(rect.x, Is.EqualTo(ScreenWidth - 10f - 300f));
            Assert.That(rect.y, Is.EqualTo(20f));
        }

        [Test]
        public void BottomLeft_SitsAgainstTheBottomEdge()
        {
            Rect rect = Place(DemoPanelAnchor.BottomLeft, 10f, 20f, 300f, 200f);

            Assert.That(rect.x, Is.EqualTo(10f));
            Assert.That(rect.y, Is.EqualTo(ScreenHeight - 20f - 200f));
        }

        [Test]
        public void BottomRight_SitsAgainstBothEdges()
        {
            Rect rect = Place(DemoPanelAnchor.BottomRight, 10f, 20f, 300f, 200f);

            Assert.That((rect.x, rect.y), Is.EqualTo((ScreenWidth - 310f, ScreenHeight - 220f)));
        }

        [Test]
        public void RightAnchored_FollowsTheRightEdgeWhenTheScreenWidthChanges()
        {
            float wide = Place(DemoPanelAnchor.TopRight, 10f, 10f, 360f, 380f, screenWidth: 1920f).x;
            float narrow = Place(DemoPanelAnchor.TopRight, 10f, 10f, 360f, 380f, screenWidth: 1280f).x;

            Assert.That(wide - narrow, Is.EqualTo(640f));
        }

        [Test]
        public void LeftAnchored_DoesNotMoveWhenTheScreenWidthChanges()
        {
            float wide = Place(DemoPanelAnchor.TopLeft, 10f, 10f, 340f, 400f, screenWidth: 1920f).x;
            float narrow = Place(DemoPanelAnchor.TopLeft, 10f, 10f, 340f, 400f, screenWidth: 1280f).x;

            Assert.That(wide, Is.EqualTo(narrow));
        }

        [Test]
        public void DefaultPanels_AreSplitLeftAndRightWithoutOverlapping()
        {
            // 각 패널 클래스가 기본으로 쓰는 값이다. 왼쪽에 주문과 선반을 쌓고, 오른쪽에 키팅 현황을 둔다.
            Rect orders = Place(DemoPanelAnchor.TopLeft, 10f, 10f, 340f, 400f);
            Rect shelf = Place(DemoPanelAnchor.TopLeft, 10f, 420f, 340f, 230f);
            Rect kitting = Place(DemoPanelAnchor.TopRight, 10f, 10f, 360f, 380f);

            Assert.That(orders.Overlaps(shelf), Is.False);
            Assert.That(orders.Overlaps(kitting), Is.False);
            Assert.That(shelf.Overlaps(kitting), Is.False);
            Assert.That(kitting.xMin, Is.GreaterThan(ScreenWidth / 2f), "키팅 패널은 화면 오른쪽 절반에 있어야 한다");
            Assert.That(orders.xMax, Is.LessThan(ScreenWidth / 2f), "주문 패널은 화면 왼쪽 절반에 있어야 한다");
        }
    }
}
