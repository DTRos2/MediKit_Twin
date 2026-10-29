using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>도구 끝점의 목표 자세를 주는 쪽. 집기 작업, 강화학습 환경, 테스트 도구가 같은 방식으로 쓴다.</summary>
    public interface IToolPoseTarget
    {
        bool HasToolTarget { get; }

        /// <summary>마지막으로 푼 IK가 수렴했으면 true. 닿을 수 없는 목표면 false.</summary>
        bool LastSolveSucceeded { get; }

        /// <summary>월드 기준 목표 자세를 준다. 접근 방향이 +Z(forward)다.</summary>
        void SetToolTarget(Pose worldPose);

        void ClearToolTarget();

        /// <summary>도구 끝점이 목표에서 위치 positionToleranceM, 회전 rotationToleranceDeg 이내이면 true.</summary>
        bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg);
    }
}
