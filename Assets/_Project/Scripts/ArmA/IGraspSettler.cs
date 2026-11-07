namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥐는 자리에 닿은 뒤(쥐기 전), 도구 끝점이 실제로 어디에 와 있는지 재어 겨냥한 자리와의 오차를 마저 메우는 쪽.
    /// 관절 각도로 "도착"을 판정하면 관절이 허용 오차 안에 들어온 순간 끝나고, 팔은 자체 무게로 처지므로 도구 끝점이 겨냥보다 몇 cm 덜 들어가 있을 수 있다.
    /// 그 오차만큼 명령을 더 보태서(실제 오차를 되먹임) 한 번 더 움직이게 하려는 것이다. 순서(ShelfGraspSequence)는 어떻게 재는지 모르고 이 계약만 부른다.
    /// </summary>
    public interface IGraspSettler
    {
        /// <summary>쥐는 자리에 닿은 뒤 오차를 메우려고 다시 움직여 보는 최대 횟수. 0이면 메우지 않는다.</summary>
        int SettleAttempts { get; }

        /// <summary>닿은 뒤 팔이 처짐까지 가라앉기를 기다리는 시간(초). 오차는 이 시간이 지난 뒤에 잰다.</summary>
        float SettleWaitSeconds { get; }

        /// <summary>
        /// 지금 도구 끝점의 오차를 재어, 허용 오차를 넘으면 그만큼 보탠 새 자리를 돌려준다(IsCorrected).
        /// 오차가 허용 안이거나 메울 수 없으면 현재 자리를 그대로 돌려준다.
        /// </summary>
        GraspRefinement Settle(ItemPickRequest request, GraspTargets current);
    }
}
