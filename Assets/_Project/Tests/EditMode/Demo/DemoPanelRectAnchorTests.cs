using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class DemoPanelRectAnchorTests
    {
        private const float ScreenWidth = 1920f;
        private const float ScreenHeight = 1080f;
        private const float Tolerance = 0.001f;

        private static readonly Vector2 Margin = new Vector2(16f, 24f);
        private static readonly Vector2 Size = new Vector2(360f, 300f);

        private static DemoPanelSlot SlotAt(DemoPanelAnchor anchor)
        {
            return new DemoPanelSlot(anchor, Margin, Size);
        }

        [Test]
        public void TopLeft_PinsToTheTopLeftCorner_AndMovesDownwardWithNegativeY()
        {
            var anchor = DemoPanelRectAnchor.From(SlotAt(DemoPanelAnchor.TopLeft));

            Assert.AreEqual(new Vector2(0f, 1f), anchor.AnchorMin);
            Assert.AreEqual(new Vector2(0f, 1f), anchor.Pivot);
            Assert.AreEqual(new Vector2(16f, -24f), anchor.AnchoredPosition);
            Assert.AreEqual(Size, anchor.Size);
        }

        [Test]
        public void TopRight_PinsToTheTopRightCorner_AndMovesLeftWithNegativeX()
        {
            var anchor = DemoPanelRectAnchor.From(SlotAt(DemoPanelAnchor.TopRight));

            Assert.AreEqual(new Vector2(1f, 1f), anchor.AnchorMin);
            Assert.AreEqual(new Vector2(-16f, -24f), anchor.AnchoredPosition);
        }

        [Test]
        public void BottomLeft_PinsToTheBottomLeftCorner()
        {
            var anchor = DemoPanelRectAnchor.From(SlotAt(DemoPanelAnchor.BottomLeft));

            Assert.AreEqual(new Vector2(0f, 0f), anchor.AnchorMin);
            Assert.AreEqual(new Vector2(16f, 24f), anchor.AnchoredPosition);
        }

        [Test]
        public void BottomCenter_PinsToTheMiddleOfTheBottomEdge_AndShiftsRightByTheMargin()
        {
            var anchor = DemoPanelRectAnchor.From(SlotAt(DemoPanelAnchor.BottomCenter));

            Assert.AreEqual(new Vector2(0.5f, 0f), anchor.AnchorMin);
            Assert.AreEqual(new Vector2(16f, 24f), anchor.AnchoredPosition);
        }

        [Test]
        public void MinAndMax_AreTheSamePoint_SoThePanelKeepsItsSizeWhenTheScreenChanges()
        {
            foreach (DemoPanelAnchor corner in System.Enum.GetValues(typeof(DemoPanelAnchor)))
            {
                var anchor = DemoPanelRectAnchor.From(SlotAt(corner));

                Assert.AreEqual(anchor.AnchorMin, anchor.AnchorMax, corner.ToString());
            }
        }

        // uGUI 패널이 IMGUI 패널과 같은 자리에 놓이는지 확인한다: 같은 슬롯을 DemoPanelLayout.Place가 계산한 자리와 비교한다.
        [Test]
        public void EverySlot_LandsWhereTheImguiLayoutPutsIt()
        {
            var slots = new Dictionary<string, DemoPanelSlot>
            {
                { "Orders", DemoPanelSlots.Orders },
                { "Shelf", DemoPanelSlots.Shelf },
                { "Camera", DemoPanelSlots.Camera },
                { "Status", DemoPanelSlots.Status },
                { "Conveyor", DemoPanelSlots.Conveyor },
                { "SimSpeed", DemoPanelSlots.SimSpeed },
                { "Joints", DemoPanelSlots.Joints },
                { "Log", DemoPanelSlots.Log },
            };

            foreach (KeyValuePair<string, DemoPanelSlot> entry in slots)
            {
                DemoPanelSlot slot = entry.Value;
                Rect expected = DemoPanelLayout.Place(slot.Anchor, slot.Margin, slot.Size, ScreenWidth, ScreenHeight);

                Rect actual = ToTopDownScreenRect(DemoPanelRectAnchor.From(slot));

                Assert.AreEqual(expected.x, actual.x, Tolerance, entry.Key + " x");
                Assert.AreEqual(expected.y, actual.y, Tolerance, entry.Key + " y");
                Assert.AreEqual(expected.width, actual.width, Tolerance, entry.Key + " width");
                Assert.AreEqual(expected.height, actual.height, Tolerance, entry.Key + " height");
            }
        }

        // 캔버스 좌표(왼쪽 아래가 원점, 위로 갈수록 커짐)로 놓인 패널을 IMGUI와 같은 화면 좌표(왼쪽 위가 원점)의 사각형으로 바꾼다.
        private static Rect ToTopDownScreenRect(DemoPanelRectAnchor anchor)
        {
            float left = anchor.AnchorMin.x * ScreenWidth + anchor.AnchoredPosition.x - anchor.Pivot.x * anchor.Size.x;
            float bottom = anchor.AnchorMin.y * ScreenHeight + anchor.AnchoredPosition.y - anchor.Pivot.y * anchor.Size.y;
            float top = ScreenHeight - (bottom + anchor.Size.y);
            return new Rect(left, top, anchor.Size.x, anchor.Size.y);
        }
    }
}
