using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥔 물건이 손에서 한 지점에서 재었을 때 직전 지점과 처음 잰 지점에 비해 얼마나 움직이고 돌았는지.
    /// 움직임은 도구 기준 좌표의 차이다. 도구와 함께 움직인 것은 빠지고 손 안에서 밀린 것만 남는다.
    /// </summary>
    public readonly struct SlipMark
    {
        public SlipMark(Vector3 sinceLastInTool, float sinceLastDeg, Vector3 sinceFirstInTool, float sinceFirstDeg)
        {
            SinceLastInTool = sinceLastInTool;
            SinceLastDeg = sinceLastDeg;
            SinceFirstInTool = sinceFirstInTool;
            SinceFirstDeg = sinceFirstDeg;
        }

        /// <summary>직전 지점에서 지금까지 물건 중심이 도구 기준으로 움직인 양(m).</summary>
        public Vector3 SinceLastInTool { get; }

        /// <summary>직전 지점에서 지금까지 물건이 도구 기준으로 돈 각도(도).</summary>
        public float SinceLastDeg { get; }

        /// <summary>처음 잰 지점에서 지금까지 물건 중심이 도구 기준으로 움직인 양(m).</summary>
        public Vector3 SinceFirstInTool { get; }

        /// <summary>처음 잰 지점에서 지금까지 물건이 도구 기준으로 돈 각도(도).</summary>
        public float SinceFirstDeg { get; }
    }
}
