using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>도구 끝점의 목표 자세를 주는 쪽. 집기 작업, 강화학습 환경, 테스트 도구가 같은 방식으로 쓴다.</summary>
    public interface IToolPoseTarget
    {
        bool HasToolTarget { get; }

        /// <summary>마지막으로 푼 IK가 수렴했으면 true. 닿을 수 없는 목표면 false.</summary>
        bool LastSolveSucceeded { get; }

        /// <summary>마지막으로 푼 IK의 오차와 반복 횟수. 푼 적이 없으면 기본값.</summary>
        IkResult LastResult { get; }

        /// <summary>월드 기준 목표 자세를 준다. 접근 방향이 +Z(forward)다.</summary>
        void SetToolTarget(Pose worldPose);

        void ClearToolTarget();

        /// <summary>
        /// 팔을 움직이지 않고, 지금 자세에서 출발해 월드 기준 자세에 닿는 관절 각도가 있는지만 풀어 본다.
        /// 집을 자세를 고를 때 여러 방향을 미리 확인하는 데 쓴다. Converged가 true이면 닿는다.
        /// </summary>
        IkResult Probe(Pose worldPose);

        /// <summary>도구 끝점이 목표에서 위치 positionToleranceM, 회전 rotationToleranceDeg 이내이면 true.</summary>
        bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg);
    }
}
