using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 점이 컨베이어 구간(입력점에서 출력점까지의 선분) 위의 어디에 놓이는지 가로 평면에서 계산한다.
    /// 씬에 놓인 센서나 멈출 자리가 어느 구간 위에 있는지를 구간 이름이 아니라 위치로 찾기 위한 것이다.
    /// </summary>
    public static class SegmentProjection
    {
        private const float MinLengthSqr = 1e-8f;

        /// <summary>
        /// point를 start→end 선분에 내린다. along은 선분 위의 위치(0이면 start, 1이면 end)이고 lateralM은 선분에서 옆으로 떨어진 거리(m)이다.
        /// 발이 선분 안(0 이상 1 이하)이면 true. 선분의 길이가 0이면 false이다.
        /// </summary>
        public static bool TryProject(Vector3 start, Vector3 end, Vector3 point, out float along, out float lateralM)
        {
            Vector3 segment = end - start;
            segment.y = 0f;
            Vector3 toPoint = point - start;
            toPoint.y = 0f;

            float lengthSqr = segment.sqrMagnitude;
            if (lengthSqr < MinLengthSqr)
            {
                along = 0f;
                lateralM = toPoint.magnitude;
                return false;
            }

            along = Vector3.Dot(toPoint, segment) / lengthSqr;
            lateralM = (toPoint - segment * along).magnitude;
            return along >= 0f && along <= 1f;
        }
    }
}
