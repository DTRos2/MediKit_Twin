using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 관절 각도로 도구 끝점 자세를 계산한다. 시뮬레이션 상태와 무관한 순수 계산이라
    /// 강화학습이 "닿을 수 있는 목표"를 뽑거나 보상을 계산할 때 물리 엔진 없이 쓴다.
    /// </summary>
    public interface IForwardKinematics
    {
        /// <summary>관절 각도(rad, JointNames 순서)에 대한 도구 끝점의 월드 자세.</summary>
        Pose ComputeToolPose(float[] anglesRad);
    }
}
