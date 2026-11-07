using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>선반 칸이 열린 방향에 관한 계산.</summary>
    public static class ShelfDirection
    {
        /// <summary>칸 밖으로 향하는 수평 방향. 방향을 알 수 없으면(영벡터) 월드 앞쪽에서 들어간다.</summary>
        public static Vector3 Outward(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            return flat.sqrMagnitude < 1e-4f ? Vector3.back : flat.normalized;
        }
    }
}
