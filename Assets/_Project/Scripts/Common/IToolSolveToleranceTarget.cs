using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 도구 끝점 목표를 줄 때 "IK가 이만큼까지 빗나가도 풀린 것으로 본다"는 기준을 함께 주는 쪽.
    /// IToolPoseTarget은 IK가 엄격한 기준(수 mm)으로 수렴해야만 움직이는데, 지나가는 자리처럼 사람이 몇 cm 빗나가도 되는 단계는
    /// 같은 엄격함을 요구할 이유가 없다. 이 기준은 목표 하나에만 붙고 다음 SetToolTarget(Pose)에서는 다시 엄격해진다.
    /// 그래서 쓰는 쪽이 기준을 되돌려 놓는 것을 잊어도 다음 동작이 느슨한 채로 남지 않는다.
    /// </summary>
    public interface IToolSolveToleranceTarget
    {
        /// <summary>
        /// 월드 기준 목표 자세를 주되, IK 결과의 도구 끝점 오차가 위치 solvePositionToleranceM, 회전 solveRotationToleranceDeg
        /// 이내이면 수렴하지 못했어도 풀린 것으로 보고 그 자세로 움직인다.
        /// 두 값 중 하나라도 0 이하이면 엄격한 기준(SetToolTarget(Pose)와 같다)을 쓴다.
        /// </summary>
        void SetToolTarget(Pose worldPose, float solvePositionToleranceM, float solveRotationToleranceDeg);
    }
}
