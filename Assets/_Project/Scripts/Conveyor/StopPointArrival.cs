using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 위 박스가 멈출 자리를 지났는지 판정하고 그 자리에 맞춘다.
    /// 컨베이어 에셋은 물건을 프레임마다 몇 cm씩(초속 2m면 50fps에서 4cm) 옮기므로 "그 자리에 닿았는지"를 거리로 재면 프레임이 길 때 건너뛴다.
    /// 그래서 진행 방향의 앞뒤(지났는가)로 보고, 지난 만큼 되돌려 놓으면 프레임 간격과 상관없이 같은 자리에 선다.
    /// 판정은 가로 평면에서만 한다(높이는 벨트가 정한다).
    /// </summary>
    public static class StopPointArrival
    {
        private const float MinFlowSqrMagnitude = 1e-8f;

        /// <summary>
        /// 멈출 자리에서 position까지의 진행 방향 거리(m). 음수면 아직 못 미쳤고 0이면 자리 위이며 양수면 지났다.
        /// 진행 방향을 알 수 없으면(길이 0) 0이다.
        /// </summary>
        public static float AlongTrack(Vector3 position, Vector3 stopPoint, Vector3 flow)
        {
            flow.y = 0f;
            if (flow.sqrMagnitude < MinFlowSqrMagnitude)
            {
                return 0f;
            }

            Vector3 offset = position - stopPoint;
            offset.y = 0f;
            return Vector3.Dot(offset, flow.normalized);
        }

        /// <summary>멈출 자리에 닿았거나 지났으면 true. 정확히 자리 위인 것도 닿은 것으로 본다.</summary>
        public static bool HasReached(Vector3 position, Vector3 stopPoint, Vector3 flow)
        {
            return AlongTrack(position, stopPoint, flow) >= 0f;
        }

        /// <summary>
        /// position을 진행 방향으로 지난 만큼 되돌려 멈출 자리의 진행 방향 위치에 맞춘다. 옆(가로) 위치와 높이는 그대로 둔다.
        /// 아직 못 미친 위치도 같은 식으로 자리까지 당겨지므로, 지났다고 확인한 뒤에만 부른다.
        /// </summary>
        public static Vector3 SnapToStopPoint(Vector3 position, Vector3 stopPoint, Vector3 flow)
        {
            flow.y = 0f;
            if (flow.sqrMagnitude < MinFlowSqrMagnitude)
            {
                return position;
            }

            return position - flow.normalized * AlongTrack(position, stopPoint, flow);
        }
    }
}
