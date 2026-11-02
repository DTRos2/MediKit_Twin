namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥔 물건의 자세를 알려 주는 쪽. 물건을 실제로 손에 붙들고 있는 방식(PhysicsGraspHolder)만 구현한다.
    /// 물건을 슬롯 자리에 맞춰 놓는 방식(ScriptAttachHolder)은 쥔 자세와 상관없이 놓이므로 알 필요가 없어 구현하지 않는다.
    /// IItemHolder에 넣지 않은 것은 쥐고 놓는 모든 방식이 이 질문에 답해야 하는 것은 아니기 때문이다.
    /// </summary>
    public interface IHeldItemSource
    {
        /// <summary>물건을 쥐고 있으면 그 모습을 돌려주고 true. 쥔 것이 없으면 false.</summary>
        bool TryGetHeldItem(out HeldItem held);
    }
}
