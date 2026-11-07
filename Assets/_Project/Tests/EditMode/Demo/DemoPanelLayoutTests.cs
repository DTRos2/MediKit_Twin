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
        public void WhenTheScreenIsShorterThanThePanel_ShrinksTheHeightToStayOnScreen()
        {
            // 위쪽 여백 420에서 시작하는 높이 230 패널을 높이 600 화면에 놓으면 180만 쓸 수 있다.
            Rect rect = DemoPanelLayout.Place(DemoPanelAnchor.TopLeft, new Vector2(10f, 420f), new Vector2(340f, 230f), ScreenWidth, 600f);

            Assert.That(rect.yMax, Is.LessThanOrEqualTo(600f));
            Assert.That(rect.height, Is.EqualTo(180f));
        }

        [Test]
        public void WhenTheScreenIsNarrowerThanThePanel_ShrinksTheWidthAndKeepsTheRightMargin()
        {
            Rect rect = DemoPanelLayout.Place(DemoPanelAnchor.TopRight, new Vector2(10f, 10f), new Vector2(360f, 380f), 300f, ScreenHeight);

            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rect.xMax, Is.EqualTo(290f), "오른쪽 여백 10을 지킨다");
        }

        [Test]
        public void WhenTheMarginAlreadyPassesTheScreenEdge_ReturnsAnEmptyPanelInsteadOfNegativeSize()
        {
            Rect rect = DemoPanelLayout.Place(DemoPanelAnchor.TopLeft, new Vector2(10f, 700f), new Vector2(340f, 230f), ScreenWidth, 600f);

            Assert.That(rect.height, Is.EqualTo(0f));
        }

        [Test]
        public void BottomCenter_WithNoOffset_IsCenteredAndSitsAgainstTheBottomEdge()
        {
            Rect rect = Place(DemoPanelAnchor.BottomCenter, 0f, 16f, 760f, 232f);

            Assert.That(rect.x, Is.EqualTo((ScreenWidth - 760f) * 0.5f));
            Assert.That(rect.y, Is.EqualTo(ScreenHeight - 16f - 232f));
            Assert.That(rect.center.x, Is.EqualTo(ScreenWidth * 0.5f).Within(0.001f));
        }

        [Test]
        public void BottomCenter_HorizontalMargin_MovesThePanelRightOfCenter()
        {
            Rect centered = Place(DemoPanelAnchor.BottomCenter, 0f, 16f, 760f, 232f);
            Rect shifted = Place(DemoPanelAnchor.BottomCenter, 50f, 16f, 760f, 232f);

            Assert.That(shifted.x - centered.x, Is.EqualTo(50f));
        }

        [Test]
        public void BottomCenter_FollowsTheCenterWhenTheScreenWidthChanges()
        {
            Rect wide = Place(DemoPanelAnchor.BottomCenter, 0f, 16f, 760f, 232f, screenWidth: 1920f);
            Rect narrow = Place(DemoPanelAnchor.BottomCenter, 0f, 16f, 760f, 232f, screenWidth: 1280f);

            Assert.That(wide.center.x - narrow.center.x, Is.EqualTo(320f).Within(0.001f));
        }

        [Test]
        public void BottomCenter_WhenTheScreenIsNarrowerThanThePanel_ShrinksToTheScreenWidth()
        {
            Rect rect = Place(DemoPanelAnchor.BottomCenter, 0f, 16f, 760f, 232f, screenWidth: 600f);

            Assert.That(rect.width, Is.EqualTo(600f));
            Assert.That(rect.x, Is.EqualTo(0f));
        }
    }
}
