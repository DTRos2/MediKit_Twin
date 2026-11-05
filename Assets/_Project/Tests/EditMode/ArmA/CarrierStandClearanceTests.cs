using System;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    /// <summary>팔 베이스 링크는 레일에 닿지 않는데 팔을 싣고 다니는 차체가 파고드는 자리를, 차체 상자를 더하면 피해 서는지 따진다.</summary>
    public sealed class CarrierStandClearanceTests
    {
        private const float RailInnerZ = 0.46f;
        private const float RailOuterZ = 0.5f;
        private const float RailBottomY = -0.025f;
        private const float RailTopY = 0.975f;
        private const float Margin = 0.01f;

        // 레일 한 줄: z 0.46 ~ 0.5, 높이 -0.025 ~ 0.975, x로는 끝없이 길다. 상자는 4면이 축에 맞게 서 있다고 보고 겹침만 따진다.
        private sealed class RailWall : IObstacleQuery
        {
            public bool Overlaps(Vector3 center, Vector3 halfExtents, Quaternion rotation, out string obstacleName)
            {
                Matrix4x4 r = Matrix4x4.Rotate(rotation);
                float extentY = Mathf.Abs(r.m10) * halfExtents.x + Mathf.Abs(r.m11) * halfExtents.y + Mathf.Abs(r.m12) * halfExtents.z;
                float extentZ = Mathf.Abs(r.m20) * halfExtents.x + Mathf.Abs(r.m21) * halfExtents.y + Mathf.Abs(r.m22) * halfExtents.z;

                bool overlaps = center.z - extentZ < RailOuterZ && center.z + extentZ > RailInnerZ
                    && center.y - extentY < RailTopY && center.y + extentY > RailBottomY;
                obstacleName = overlaps ? "Rail" : string.Empty;
                return overlaps;
            }
        }

        private sealed class FixedBaseState : IArmBaseStateSource
        {
            public bool TryGetState(out Pose armBaseWorldPose, out float rootYawDeg)
            {
                armBaseWorldPose = new Pose(new Vector3(0f, 0.75f, 0f), Quaternion.identity);
                rootYawDeg = 0f;
                return true;
            }
        }

        // 팔 베이스 링크 아래 작은 상자(서는 자리 z 0.55에서는 레일에 닿지 않는다)와, 링크 기준 뒤로 0.1 치우친 0.5m 차체.
        private static readonly LinkVolume ArmBaseBox = new LinkVolume(0, new Vector3(0f, 0.05f, 0f), Quaternion.identity, new Vector3(0.04f, 0.05f, 0.03f));
        private static readonly LinkVolume CarrierBox = new LinkVolume(0, new Vector3(0f, -0.5f, 0.1f), Quaternion.identity, new Vector3(0.25f, 0.25f, 0.25f));

        // 서는 자리: 슬롯(0,0,0) 뒤(+z)로 0.55m + 물러난 거리.
        private static Vector3? StandBehindSlot(float extra, float lateral) => new Vector3(lateral, 0f, 0.55f + extra);

        private static Func<float, float, Vector3?> StandWith(params LinkVolume[] volumes)
        {
            var checker = new BaseClearanceChecker(volumes, new RailWall(), Margin);
            var clearance = new BaseStandClearance(
                new FixedBaseState(),
                pose => checker.TryFindCollision(pose, out string description) ? description : string.Empty,
                0.01f,
                0.5f);

            // 레일 쪽(-z)으로 돌아 서므로 차체의 링크 기준 +z가 월드 -z, 곧 레일 쪽이다.
            return clearance.Apply(StandBehindSlot, 180f, "박스");
        }

        [Test]
        public void TheArmBaseAlone_DoesNotKnowTheBodyIsSinkingIntoTheRail()
        {
            Vector3? stand = StandWith(ArmBaseBox)(0f, 0f);

            Assert.That(stand.Value.z, Is.EqualTo(0.55f).Within(1e-5f));
        }

        [Test]
        public void WithTheBodyBox_TheStandBacksAwayUntilTheBodyClearsTheRail()
        {
            Vector3? stand = StandWith(ArmBaseBox, CarrierBox)(0f, 0f);

            // 차체 앞 끝 = 서는 자리 z - 0.1 - 0.25, 여유 1cm까지 레일 바깥 면(0.5)을 넘지 않으려면 z >= 0.86이다.
            Assert.That(stand.Value.z, Is.InRange(0.859f, 0.871f));
        }

        [Test]
        public void ABodyThatIsAlreadyClear_LeavesTheStandAlone()
        {
            Vector3? stand = StandWith(ArmBaseBox, CarrierBox)(0.5f, 0f);

            Assert.That(stand.Value.z, Is.EqualTo(1.05f).Within(1e-5f));
        }
    }
}
