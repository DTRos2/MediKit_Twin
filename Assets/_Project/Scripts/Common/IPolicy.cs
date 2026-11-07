namespace MediKitTwin.Common
{
    /// <summary>
    /// 관측을 받아 행동을 고르는 쪽. 무작위 정책, 학습된 ONNX 정책, ROS 2 정책 노드(외부 학습기), 스크립트 정책이
    /// 같은 인터페이스를 구현한다. 강화학습 환경은 정책이 어디서 오는지 모른다.
    /// 인터페이스가 Common에 있어서 Rl(환경)과 Ros(통신)가 서로를 참조하지 않고도 정책을 주고받는다.
    /// </summary>
    public interface IPolicy
    {
        /// <param name="observation">관측 벡터</param>
        /// <param name="action">출력 버퍼. 각 값은 -1~1로 채운다. 정책이 아직 답을 못 줬으면 0으로 둔다.</param>
        void SelectAction(float[] observation, float[] action);
    }
}
