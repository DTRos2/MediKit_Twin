namespace MediKitTwin.Common
{
    /// <summary>
    /// 환경이 보상과 에피소드 결과를 알려 줄 때 받는 쪽. 학습기에 보상을 넘겨야 하는 정책(ROS 2 브리지)이
    /// IPolicy와 함께 구현한다. 무작위 정책처럼 필요 없는 정책은 구현하지 않아도 된다.
    /// </summary>
    public interface IRlFeedbackListener
    {
        /// <summary>행동 하나의 결과로 받은 보상.</summary>
        void OnStepReward(float reward);

        /// <summary>에피소드가 끝났을 때. 누적 보상과 성공 여부.</summary>
        void OnEpisodeFinished(float totalReward, bool success);
    }
}
