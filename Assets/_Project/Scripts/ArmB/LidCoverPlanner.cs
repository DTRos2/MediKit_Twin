using UnityEngine;

namespace MediKitTwin.ArmB
{
    /// <summary>
    /// 로봇암 B가 뚜껑을 집어 키트 박스에 덮을 때 쓰는 자세 계산. Unity 오브젝트를 모르고 숫자만 다루므로 단독으로 시험할 수 있다.
    /// 도구는 +Z가 아래를 향하고, 손가락이 닫히는 방향(도구 X)이 물체의 짧은 변(x 양면)이 되도록 도구 Y를 물체의 긴 변에 맞춘다.
    /// 뚜껑과 키트 박스는 긴 변이 같은 방향으로 놓이므로 쥔 채로 덮어도 손목을 따로 돌릴 필요가 없다.
    /// </summary>
    public static class LidCoverPlanner
    {
        /// <summary>방향을 바닥 평면에 눕힌 단위 벡터. 거의 수직이면 월드 forward를 돌려준다.</summary>
        public static Vector3 GroundDirection(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            return flat.sqrMagnitude < 1e-4f ? Vector3.forward : flat.normalized;
        }

        /// <summary>도구 +Z는 아래, 도구 +Y는 물체의 긴 변 방향이 되는 도구 회전.</summary>
        public static Quaternion ToolRotation(Vector3 longAxis)
        {
            return Quaternion.LookRotation(Vector3.down, GroundDirection(longAxis));
        }

        /// <summary>
        /// 긴 변은 앞뒤가 같은 모양이라 방향이 둘이다. preferred에 가까운 쪽을 먼저 돌려줘서 손목이 덜 돌게 한다.
        /// 먼저 닿는 쪽을 쓰면 되므로 호출하는 쪽은 앞에서부터 IK로 확인한다.
        /// </summary>
        public static Vector3[] CandidateAxes(Vector3 longAxis, Vector3 preferred)
        {
            Vector3 axis = GroundDirection(longAxis);
            Vector3 toward = GroundDirection(preferred);
            return Vector3.Dot(axis, toward) >= 0f ? new[] { axis, -axis } : new[] { -axis, axis };
        }

        /// <summary>
        /// 뚜껑이 키트 박스에 덮였을 때 뚜껑 중심의 높이. 뚜껑 윗판(두께 lidWall)이 박스 윗면에 얹히고 테두리가 아래로 내려온다.
        /// 그리퍼 끝(TCP)은 뚜껑 중심을 쥐므로 덮을 때 TCP 높이도 이 값이다.
        /// </summary>
        public static float CoverHeight(float boxTopY, float lidHeight, float lidWall)
        {
            return boxTopY + lidWall - lidHeight * 0.5f;
        }

        /// <summary>position 위 height 만큼 떨어진 자세.</summary>
        public static Pose Above(Vector3 position, float height, Quaternion rotation)
        {
            return new Pose(position + Vector3.up * height, rotation);
        }

        /// <summary>
        /// 뚜껑이 박스에 안착했는지. 수평 오차가 틈(박스와 뚜껑 안쪽 사이) 이내이고 높이가 덮은 높이에서 toleranceM 이내이면 true.
        /// </summary>
        public static bool IsSeated(Vector3 lidCenter, Vector3 boxCenter, float coverHeight, float toleranceM)
        {
            float dx = lidCenter.x - boxCenter.x;
            float dz = lidCenter.z - boxCenter.z;
            float horizontal = Mathf.Sqrt(dx * dx + dz * dz);
            return horizontal <= toleranceM && Mathf.Abs(lidCenter.y - coverHeight) <= toleranceM;
        }
    }
}
