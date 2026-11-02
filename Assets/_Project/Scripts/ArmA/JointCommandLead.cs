using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 관절 명령(구동 목표)을 목표 쪽으로 옮기되, 실제 관절 각도보다 너무 앞서 나가지 못하게 한다.
    /// 명령이 실제 각도와 상관없이 목표를 향해 계속 나가면, 팔이 선반에 막혔을 때 명령과 실제의 차이가 끝없이 벌어져
    /// 구동이 최대 힘으로 밀어붙이다가 떨어지는 순간 튕겨 날뛴다. 앞서 나갈 수 있는 폭을 제한하면 막혀도 힘이 일정 수준에서 멈춘다.
    /// 순수 계산이라 씬 없이 시험한다. 각도는 모두 같은 기준(URDF, rad)이다.
    /// </summary>
    public static class JointCommandLead
    {
        /// <param name="command">지금 명령.</param>
        /// <param name="goal">목표.</param>
        /// <param name="measured">실제 관절 각도.</param>
        /// <param name="maxStepRad">한 번에 명령을 옮길 수 있는 최대 각도.</param>
        /// <param name="maxLeadRad">명령이 실제 각도에서 벗어날 수 있는 최대 각도(양쪽 모두).</param>
        public static float Advance(float command, float goal, float measured, float maxStepRad, float maxLeadRad)
        {
            float next = Mathf.MoveTowards(command, goal, maxStepRad);
            return Mathf.Clamp(next, measured - maxLeadRad, measured + maxLeadRad);
        }
    }
}
