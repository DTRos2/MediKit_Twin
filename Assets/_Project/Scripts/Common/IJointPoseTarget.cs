using System.Collections.Generic;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇암을 관절 각도로 직접 보내는 쪽. 도구 끝점 목표(IToolPoseTarget)가 풀어 낸 관절 모양이 아니라,
    /// 사람이 알려 준 관절 각도 그대로 가야 할 때 쓴다. 각도는 로봇의 관절 이름 순서이고 단위는 rad(URDF 기준)이다.
    /// </summary>
    public interface IJointPoseTarget
    {
        /// <summary>
        /// 남은 도구 끝점 목표를 지우고 관절 목표를 준다(남겨 두면 IK가 관절 목표를 덮어쓴다).
        /// 관절 수가 다르거나 사람이 직접 조작하는 중이라 받을 수 없으면 false이고 아무것도 바꾸지 않는다.
        /// </summary>
        bool SetJointTarget(IReadOnlyList<float> anglesRad);

        /// <summary>모든 관절이 마지막으로 준 관절 목표에서 toleranceRad 이내이면 true.</summary>
        bool HasReachedJointTarget(float toleranceRad);
    }
}
