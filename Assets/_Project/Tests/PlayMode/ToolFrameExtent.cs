#if UNITY_EDITOR
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 월드의 점들을 도구 끝점 기준 좌표(도구의 x·y·z 축 방향 거리, cm)로 바꿔 축마다 최소와 최대를 모은다.
    /// 그리퍼가 도구 끝점 둘레로 얼마나 퍼져 있는지(몸체 폭, 손가락 벌림과 두께, 손가락 길이)를 재는 데 쓴다.
    /// </summary>
    public sealed class ToolFrameExtent
    {
        private readonly Pose _tool;
        private Vector3 _min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        private Vector3 _max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        public ToolFrameExtent(Pose tool)
        {
            _tool = tool;
        }

        public bool IsEmpty => _min.x > _max.x;

        public void Include(Vector3 worldPoint)
        {
            Vector3 local = Quaternion.Inverse(_tool.rotation) * (worldPoint - _tool.position);
            _min = Vector3.Min(_min, local);
            _max = Vector3.Max(_max, local);
        }

        public void Include(System.Collections.Generic.IEnumerable<Vector3> worldPoints)
        {
            foreach (Vector3 point in worldPoints)
            {
                Include(point);
            }
        }

        /// <summary>"x −4.1~+4.1, y −2.0~+2.0, z −1.0~+9.5cm (폭 8.2 × 4.0 × 10.5)"처럼 적는다.</summary>
        public string Describe()
        {
            if (IsEmpty)
            {
                return "(없음)";
            }

            Vector3 size = _max - _min;
            return $"x {_min.x * 100f:+0.0;-0.0}~{_max.x * 100f:+0.0;-0.0}, y {_min.y * 100f:+0.0;-0.0}~{_max.y * 100f:+0.0;-0.0}, " +
                   $"z {_min.z * 100f:+0.0;-0.0}~{_max.z * 100f:+0.0;-0.0}cm (크기 {size.x * 100f:0.0} × {size.y * 100f:0.0} × {size.z * 100f:0.0})";
        }
    }
}
#endif
