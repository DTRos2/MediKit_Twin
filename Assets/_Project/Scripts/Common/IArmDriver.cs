namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇암에 관절 목표를 주는 쪽. 강화학습 정책, 테스트 도구가 IArmTarget 없이 직접 목표를 줄 때 쓴다.
    /// </summary>
    public interface IArmDriver
    {
        float MaxJointSpeedRadPerSec { get; }

        /// <summary>시작 자세(rad, JointNames 순서). 에피소드 시작이나 "홈으로" 명령에 쓴다.</summary>
        float[] HomePoseRad { get; }

        /// <summary>관절 목표 각도(rad)를 JointNames 순서로 준다. 목표까지 가는 속도는 구현체가 제한한다.</summary>
        void SetJointTargets(float[] anglesRad);

        /// <summary>모든 관절이 목표에서 toleranceRad 이내이면 true.</summary>
        bool IsAtTarget(float toleranceRad);

        /// <summary>관절을 주어진 각도로 즉시 옮기고 속도를 0으로 만든다. 강화학습 에피소드 시작 때 쓴다.</summary>
        void ResetToPose(float[] anglesRad);
    }
}
