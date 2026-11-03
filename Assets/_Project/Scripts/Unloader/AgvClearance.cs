using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Unloader
{
    /// <summary>
    /// 로봇이 지나는 점이 물체와 겹치면 겹치지 않는 가장 가까운 점으로 민다. (FR-12)
    /// 하역 로봇은 사람이 정한 AGVPath를 따르는데, 웨이포인트가 받침대나 단 가장자리에 바짝 붙어 있으면 로봇 몸이 그 물체를 파고든다.
    /// 경로를 고치지 않고, 겹치는 지점에서만 로봇이 서는 점을 물체 밖으로 최소한만 옮긴다. 바닥 평면(x, z)만 본다. 씬 없이 시험한다.
    /// 물체는 월드 축 정렬 경계 상자(Bounds)로 본다. 받침대, 단, 컨베이어는 축에 맞춰 놓여 있어 실제와 거의 같고, 돌아간 물체는 실제보다 넓게 잡아 안전한 쪽이다.
    /// </summary>
    public static class AgvClearance
    {
        private const float OverlapEpsilon = 1e-5f;

        /// <summary>
        /// 로봇 몸(폭과 길이)을 방향 yawDeg로 놓았을 때 월드 축 정렬 직사각형의 반 크기(x, z).
        /// 길이는 로봇이 바라보는 쪽이고, yawDeg는 월드 Y축 기준이며 0도가 +z, 90도가 +x이다.
        /// </summary>
        public static Vector2 FootprintHalf(float halfWidthM, float halfLengthM, float yawDeg)
        {
            float rad = yawDeg * Mathf.Deg2Rad;
            float sin = Mathf.Abs(Mathf.Sin(rad));
            float cos = Mathf.Abs(Mathf.Cos(rad));
            return new Vector2(sin * halfLengthM + cos * halfWidthM, cos * halfLengthM + sin * halfWidthM);
        }

        /// <summary>
        /// center를 중심으로 하는 직사각형(반 크기 half)이 obstacles와 marginM 안으로 겹치면, 겹침이 가장 작은 쪽(x 또는 z)으로 밀어 낸 중심을 돌려준다.
        /// 물체를 차례로 처리하므로 한 번 밀어 다른 물체에 닿을 수 있다. 호출하는 쪽이 바뀌지 않을 때까지 되풀이한다.
        /// 높이(y)는 그대로 둔다.
        /// </summary>
        public static Vector3 PushOut(Vector3 center, Vector2 half, IReadOnlyList<Bounds> obstacles, float marginM)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                Bounds obstacle = obstacles[i];
                float offsetX = center.x - obstacle.center.x;
                float offsetZ = center.z - obstacle.center.z;
                float overlapX = half.x + obstacle.extents.x + marginM - Mathf.Abs(offsetX);
                float overlapZ = half.y + obstacle.extents.z + marginM - Mathf.Abs(offsetZ);

                if (overlapX <= OverlapEpsilon || overlapZ <= OverlapEpsilon)
                {
                    continue;
                }

                if (overlapX < overlapZ)
                {
                    center.x += (offsetX >= 0f ? 1f : -1f) * overlapX;
                }
                else
                {
                    center.z += (offsetZ >= 0f ? 1f : -1f) * overlapZ;
                }
            }

            return center;
        }
    }
}
