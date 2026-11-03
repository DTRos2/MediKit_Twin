using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class DemoPanelSlotsTests
    {
        private const float ScreenWidth = 1920f;
        private const float ScreenHeight = 1080f;

        // 가운데 띠: 로봇이 보여야 하는 자리. 펼친 패널 가운데에서는 로그만 이 띠에 들어올 수 있다.
        private const float CenterBandLeft = 388f;
        private const float CenterBandRight = 1512f;
        private const float CenterBandBottom = 820f;

        private static Rect PlaceOnReferenceScreen(DemoPanelSlot slot)
        {
            return DemoPanelLayout.Place(slot.Anchor, slot.Margin, slot.Size, ScreenWidth, ScreenHeight);
        }

        private static Dictionary<string, DemoPanelSlot> AllSlots()
        {
            return new Dictionary<string, DemoPanelSlot>
            {
                { "Orders", DemoPanelSlots.Orders },
                { "Shelf", DemoPanelSlots.Shelf },
                { "Camera", DemoPanelSlots.Camera },
                { "Status", DemoPanelSlots.Status },
                { "Conveyor", DemoPanelSlots.Conveyor },
                { "Joints", DemoPanelSlots.Joints },
                { "Log", DemoPanelSlots.Log },
            };
        }

        [Test]
        public void TheExpandedPanels_NeverOverlapEachOther()
        {
            var rects = new List<KeyValuePair<string, Rect>>();
            foreach (KeyValuePair<string, DemoPanelSlot> slot in AllSlots())
            {
                rects.Add(new KeyValuePair<string, Rect>(slot.Key, PlaceOnReferenceScreen(slot.Value)));
            }

            for (int i = 0; i < rects.Count; i++)
            {
                for (int j = i + 1; j < rects.Count; j++)
                {
                    Assert.That(rects[i].Value.Overlaps(rects[j].Value), Is.False, $"{rects[i].Key}와 {rects[j].Key}가 겹친다");
                }
            }
        }

        [Test]
        public void TheExpandedPanels_StayInsideTheReferenceScreen()
        {
            foreach (KeyValuePair<string, DemoPanelSlot> slot in AllSlots())
            {
                Rect rect = PlaceOnReferenceScreen(slot.Value);

                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f), slot.Key);
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f), slot.Key);
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(ScreenWidth), slot.Key);
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(ScreenHeight), slot.Key);
                Assert.That(rect.size, Is.EqualTo(slot.Value.Size), $"{slot.Key}: 기준 화면 안에서는 크기가 줄지 않아야 한다");
            }
        }

        [Test]
        public void TheCenterBand_IsLeftForTheRobot_ExceptForTheLogAtTheBottom()
        {
            foreach (KeyValuePair<string, DemoPanelSlot> slot in AllSlots())
            {
                if (slot.Key == "Log")
                {
                    continue;
                }

                Rect rect = PlaceOnReferenceScreen(slot.Value);
                bool isInsideBand = rect.xMax > CenterBandLeft && rect.xMin < CenterBandRight && rect.yMin < CenterBandBottom;

                Assert.That(isInsideBand, Is.False, $"{slot.Key}가 로봇이 보여야 하는 가운데 띠를 가린다");
            }
        }

        [Test]
        public void TheLog_SitsAtTheBottomCenter_BelowTheBandTheRobotNeeds()
        {
            Rect log = PlaceOnReferenceScreen(DemoPanelSlots.Log);

            Assert.That(log.center.x, Is.EqualTo(ScreenWidth * 0.5f).Within(0.001f));
            Assert.That(log.yMin, Is.GreaterThanOrEqualTo(CenterBandBottom), "로그가 로봇이 보여야 하는 띠 아래에 있어야 한다");
        }

        [Test]
        public void TheLeftColumn_HoldsOrdersShelfAndCamera_AndTheRightColumnHoldsStatusConveyorAndJoints()
        {
            Assert.That(PlaceOnReferenceScreen(DemoPanelSlots.Orders).xMax, Is.LessThan(ScreenWidth * 0.5f));
            Assert.That(PlaceOnReferenceScreen(DemoPanelSlots.Shelf).xMax, Is.LessThan(ScreenWidth * 0.5f));
            Assert.That(PlaceOnReferenceScreen(DemoPanelSlots.Camera).xMax, Is.LessThan(ScreenWidth * 0.5f));
            Assert.That(PlaceOnReferenceScreen(DemoPanelSlots.Status).xMin, Is.GreaterThan(ScreenWidth * 0.5f));
            Assert.That(PlaceOnReferenceScreen(DemoPanelSlots.Conveyor).xMin, Is.GreaterThan(ScreenWidth * 0.5f));
            Assert.That(PlaceOnReferenceScreen(DemoPanelSlots.Joints).xMin, Is.GreaterThan(ScreenWidth * 0.5f));
        }

        [Test]
        public void TheConveyorPanel_SitsRightBelowTheStatusPanel_InTheSameColumn()
        {
            Rect status = PlaceOnReferenceScreen(DemoPanelSlots.Status);
            Rect conveyor = PlaceOnReferenceScreen(DemoPanelSlots.Conveyor);

            Assert.That(conveyor.xMin, Is.EqualTo(status.xMin));
            Assert.That(conveyor.width, Is.EqualTo(status.width));
            Assert.That(conveyor.yMin, Is.GreaterThanOrEqualTo(status.yMax));
        }

        [Test]
        public void TheRightColumn_EndsAtTheSameBottomMarginAsTheLeftColumn()
        {
            Rect joints = PlaceOnReferenceScreen(DemoPanelSlots.Joints);
            Rect camera = PlaceOnReferenceScreen(DemoPanelSlots.Camera);

            Assert.That(joints.yMax, Is.EqualTo(camera.yMax));
        }

        [Test]
        public void TheRarelyUsedPanels_StartCollapsed_AndTheEverydayOnesDoNot()
        {
            Assert.That(DemoPanelSlots.Joints.StartsCollapsed, Is.True);
            Assert.That(DemoPanelSlots.Log.StartsCollapsed, Is.True);

            Assert.That(DemoPanelSlots.Orders.StartsCollapsed, Is.False);
            Assert.That(DemoPanelSlots.Shelf.StartsCollapsed, Is.False);
            Assert.That(DemoPanelSlots.Camera.StartsCollapsed, Is.False);
            Assert.That(DemoPanelSlots.Status.StartsCollapsed, Is.False);
            Assert.That(DemoPanelSlots.Conveyor.StartsCollapsed, Is.False);
        }

        [Test]
        public void TheGapBetweenStackedPanels_IsTheSameEverywhere()
        {
            Rect orders = PlaceOnReferenceScreen(DemoPanelSlots.Orders);
            Rect shelf = PlaceOnReferenceScreen(DemoPanelSlots.Shelf);
            Rect status = PlaceOnReferenceScreen(DemoPanelSlots.Status);
            Rect conveyor = PlaceOnReferenceScreen(DemoPanelSlots.Conveyor);
            Rect joints = PlaceOnReferenceScreen(DemoPanelSlots.Joints);

            float gap = shelf.yMin - orders.yMax;
            Assert.That(conveyor.yMin - status.yMax, Is.EqualTo(gap));
            Assert.That(joints.yMin - conveyor.yMax, Is.EqualTo(gap));
        }
    }
}
