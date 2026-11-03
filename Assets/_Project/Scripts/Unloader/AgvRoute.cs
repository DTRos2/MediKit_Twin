using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Unloader
{
    /// <summary>
    /// 하역 로봇이 AGVPath를 어디까지 따라갈지와, 서야 할 자리 앞에서 방향을 돌릴 지점(스테이징)을 계산한다. (FR-12)
    /// 경로 끝이 목표 자리가 아닐 수 있고(구역이 여럿이고 경로는 하나이다) 사람이 경로를 고치므로, 경로의 끝까지 가지 않고
    /// 목표 자리에 가장 가까운 지점까지만 가게 한다. 바닥 평면(x, z)만 본다. 씬 없이 시험한다.
    /// </summary>
    public static class AgvRoute
    {
        // 목표가 한 구간의 끝에 닿았다고 보는 구간 비율. 이 이상이면 그 구간의 끝 웨이포인트까지 지난다.
        private const float SegmentEndRatio = 0.999f;

        /// <summary>
        /// 목표에서 가장 가까운 경로 위 지점에 가려고 앞에서부터 지나야 하는 웨이포인트의 수.
        /// 웨이포인트가 없으면 0, 하나면 1이다. 목표가 첫 웨이포인트보다 앞이면 1(첫 웨이포인트까지는 간다).
        /// 되돌아올 때는 같은 수의 웨이포인트를 거꾸로 지난다.
        /// </summary>
        public static int WaypointsBefore(IReadOnlyList<Vector3> waypoints, Vector3 target)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                return 0;
            }

            if (waypoints.Count == 1)
            {
                return 1;
            }

            int bestSegment = 0;
            float bestDistanceSqr = float.MaxValue;
            float bestRatio = 0f;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                float ratio = ClosestRatio(waypoints[i], waypoints[i + 1], target);
                float distanceSqr = HorizontalSqrDistance(target, Vector3.Lerp(waypoints[i], waypoints[i + 1], ratio));
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestSegment = i;
                    bestRatio = ratio;
                }
            }

            return bestRatio >= SegmentEndRatio ? bestSegment + 2 : bestSegment + 1;
        }

        /// <summary>
        /// 서야 할 자리(position, 바라보는 방향 yawDeg)에서 뒤로 distanceM 떨어진 지점. 로봇은 여기서 방향을 돌린 뒤 똑바로 전진해 자리에 들어가고,
        /// 자리에서 나올 때는 여기까지 후진한다. 자리 앞에 물체가 바짝 있어도 돌 때 부딪히지 않게 하려는 여유 지점이다.
        /// </summary>
        public static Vector3 StagePoint(Vector3 position, float yawDeg, float distanceM)
        {
            float yawRad = yawDeg * Mathf.Deg2Rad;
            var forward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            Vector3 stage = position - forward * distanceM;
            stage.y = position.y;
            return stage;
        }

        // 선분 a-b 위에서 point에 가장 가까운 점의 위치(0이면 a, 1이면 b). 수평 평면에서 잰다.
        private static float ClosestRatio(Vector3 a, Vector3 b, Vector3 point)
        {
            var ab = new Vector2(b.x - a.x, b.z - a.z);
            float lengthSqr = ab.sqrMagnitude;
            if (lengthSqr < 1e-8f)
            {
                return 0f;
            }

            var ap = new Vector2(point.x - a.x, point.z - a.z);
            return Mathf.Clamp01(Vector2.Dot(ap, ab) / lengthSqr);
        }

        private static float HorizontalSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
