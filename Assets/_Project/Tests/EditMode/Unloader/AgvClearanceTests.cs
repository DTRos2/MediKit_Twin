using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Unloader.Tests
{
    public sealed class AgvClearanceTests
    {
        private const float Margin = 0.03f;

        // 컨베이어 끝 받침대: 월드 x -0.406 ~ -0.046, z 6.87 ~ 7.13.
        private static readonly Bounds Pedestal = new Bounds(new Vector3(-0.226f, 0.425f, 7.0f), new Vector3(0.36f, 0.85f, 0.26f));

        private static List<Bounds> Obstacles(params Bounds[] bounds) => new List<Bounds>(bounds);

        [Test]
        public void PushOut_LeavesThePoint_WhenNothingOverlaps()
        {
            var center = new Vector3(-2f, 0f, 7f);

            Vector3 result = AgvClearance.PushOut(center, new Vector2(0.25f, 0.25f), Obstacles(Pedestal), Margin);

            Assert.That(result, Is.EqualTo(center));
        }

        [Test]
        public void PushOut_PushesAlongTheSmallestOverlap_WhenAWaypointHugsTheEdge()
        {
            // 받침대 북쪽 가장자리(z 7.13)에서 7cm 떨어진 웨이포인트. 로봇 반 크기 0.25m이면 북쪽(+z)으로만 조금 밀면 된다.
            var waypoint = new Vector3(-0.34f, 0f, 7.20f);

            Vector3 result = AgvClearance.PushOut(waypoint, new Vector2(0.25f, 0.25f), Obstacles(Pedestal), Margin);

            Assert.That(result.x, Is.EqualTo(waypoint.x).Within(1e-4f), "x는 그대로여야 한다");
            Assert.That(result.z, Is.EqualTo(7.0f + 0.13f + 0.25f + Margin).Within(1e-4f), "받침대 북쪽 가장자리에서 로봇 반 크기와 여유만큼 떨어진다");
        }

        [Test]
        public void PushOut_ResultNoLongerOverlaps()
        {
            var half = new Vector2(0.25f, 0.25f);

            Vector3 result = AgvClearance.PushOut(new Vector3(-0.34f, 0f, 7.20f), half, Obstacles(Pedestal), Margin);
            Vector3 again = AgvClearance.PushOut(result, half, Obstacles(Pedestal), Margin);

            Assert.That(again, Is.EqualTo(result));
        }

        [Test]
        public void PushOut_IgnoresHeight()
        {
            Vector3 result = AgvClearance.PushOut(new Vector3(-0.34f, 5f, 7.20f), new Vector2(0.25f, 0.25f), Obstacles(Pedestal), Margin);

            Assert.That(result.y, Is.EqualTo(5f));
            Assert.That(result.z, Is.GreaterThan(7.20f));
        }

        [Test]
        public void PushOut_PushesToTheSideTheCenterIsOn()
        {
            Vector3 south = AgvClearance.PushOut(new Vector3(-0.226f, 0f, 6.9f), new Vector2(0.25f, 0.25f), Obstacles(Pedestal), Margin);

            Assert.That(south.z, Is.LessThan(6.9f), "받침대 중심보다 남쪽에 있으면 남쪽으로 밀려야 한다");
        }

        [Test]
        public void PushOut_PushesBothObstacles_WhenTwoAreInTheWay()
        {
            var wall = new Bounds(new Vector3(0f, 0.5f, 7.6f), new Vector3(4f, 1f, 0.2f));

            Vector3 result = AgvClearance.PushOut(new Vector3(-0.34f, 0f, 7.20f), new Vector2(0.25f, 0.25f), Obstacles(Pedestal, wall), Margin);

            Assert.That(result.z, Is.GreaterThan(7.20f));
        }

        [Test]
        public void FootprintHalf_SwapsWidthAndLength_WithTheFacing()
        {
            // yaw 0도는 +z를 보므로 길이가 z로, 폭이 x로 간다. 90도에서는 반대이다.
            Vector2 facingZ = AgvClearance.FootprintHalf(0.15f, 0.25f, 0f);
            Vector2 facingX = AgvClearance.FootprintHalf(0.15f, 0.25f, 90f);

            Assert.That(facingZ.x, Is.EqualTo(0.15f).Within(1e-4f));
            Assert.That(facingZ.y, Is.EqualTo(0.25f).Within(1e-4f));
            Assert.That(facingX.x, Is.EqualTo(0.25f).Within(1e-4f));
            Assert.That(facingX.y, Is.EqualTo(0.15f).Within(1e-4f));
        }

        [Test]
        public void FootprintHalf_IsLargestOnTheDiagonal()
        {
            Vector2 diagonal = AgvClearance.FootprintHalf(0.15f, 0.25f, 45f);

            Assert.That(diagonal.x, Is.EqualTo(diagonal.y).Within(1e-4f));
            Assert.That(diagonal.x, Is.GreaterThan(0.25f));
        }
    }
}
