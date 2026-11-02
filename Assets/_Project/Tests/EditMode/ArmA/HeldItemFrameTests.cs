using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class HeldItemFrameTests
    {
        // 도구는 수평으로 +Z를 보고, 물건 중심은 도구 앞(+Z) 2cm, 아래(-Y) 1cm에 있으며 물건은 눕혀(위쪽이 도구의 위쪽) 있다.
        private static readonly Pose Tool = new Pose(new Vector3(1f, 1f, 0f), Quaternion.identity);
        private static readonly Vector3 ItemCenter = new Vector3(1f, 0.99f, 0.02f);

        [Test]
        public void MeasuresTheItemRelativeToTheTool()
        {
            HeldItemFrame frame = HeldItemFrame.Measure(Tool, ItemCenter, Quaternion.identity);

            Assert.That(frame.CenterInTool.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(frame.CenterInTool.y, Is.EqualTo(-0.01f).Within(1e-5f));
            Assert.That(frame.CenterInTool.z, Is.EqualTo(0.02f).Within(1e-5f));
        }

        [Test]
        public void TheToolThatCarriedTheItemHereGivesTheSameItemCenterAgain()
        {
            // 도구를 이리저리 돌려 놓았다가 같은 도구 자세로 되돌리면 물건 중심도 그대로여야 한다.
            Quaternion tilted = Quaternion.Euler(20f, 70f, 35f);
            Pose tool = new Pose(new Vector3(0.4f, 1.2f, -0.3f), tilted);
            Vector3 center = tool.position + tilted * new Vector3(0.01f, -0.02f, 0.03f);
            Quaternion itemRotation = tilted * Quaternion.Euler(0f, 90f, 0f);

            HeldItemFrame frame = HeldItemFrame.Measure(tool, center, itemRotation);

            Assert.That(Vector3.Distance(frame.ToolPositionFor(center, tilted), tool.position), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(frame.ToolRotationFor(itemRotation), tilted), Is.LessThan(1e-3f));
        }

        [Test]
        public void TheToolRotationForAWantedItemRotationTurnsTheItemIntoThatRotation()
        {
            HeldItemFrame frame = HeldItemFrame.Measure(Tool, ItemCenter, Quaternion.Euler(0f, 0f, 90f));
            Quaternion wanted = Quaternion.Euler(0f, 30f, 0f);

            Quaternion toolRotation = frame.ToolRotationFor(wanted);

            // 그 도구 방향으로 쥐고 있으면 물건 회전이 원하는 회전이 된다.
            Quaternion itemRotation = toolRotation * frame.RotationInTool;
            Assert.That(Quaternion.Angle(itemRotation, wanted), Is.LessThan(1e-3f));
        }

        [Test]
        public void TurningTheToolMovesTheToolPositionByTheOffsetOfTheItem()
        {
            HeldItemFrame frame = HeldItemFrame.Measure(Tool, ItemCenter, Quaternion.identity);
            Vector3 slot = new Vector3(2f, 1f, 0f);

            // 도구를 아래로 향하게(+Z → -Y) 돌리면 물건은 도구 앞이 아니라 도구 아래쪽으로 옮겨 가므로 도구 자리도 달라진다.
            Quaternion down = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            Vector3 flat = frame.ToolPositionFor(slot, Quaternion.identity);
            Vector3 pointingDown = frame.ToolPositionFor(slot, down);

            Assert.That(Vector3.Distance(flat, pointingDown), Is.GreaterThan(0.01f));
            Assert.That(Vector3.Distance(pointingDown + down * frame.CenterInTool, slot), Is.LessThan(1e-5f), "어느 방향이든 물건 중심은 슬롯에 와야 한다");
        }
    }
}
