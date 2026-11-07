namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 도구 목표를 푸는 쪽이 쓰는 로봇 운동학 모델(IArmModel)을 알려 주는 쪽. 이송 순서가 도구 목표 쪽에서 그 팔의 모델을 찾을 때 쓴다.
    /// 씬에서 팔을 따로 지정하지 않아도 도구 목표와 같은 팔을 쓰게 하려는 것이다.
    /// </summary>
    public interface IArmModelSource
    {
        IArmModel Model { get; }
    }
}
