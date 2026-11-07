namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건을 쥐는 데 성공한 순간을 전해 받는 쪽. 쥐고 나면 그 물건은 더 이상 노리는 대상이 아니므로,
    /// 노리는 물건을 화면에 표시하던 쪽이 표시를 거두는 데 쓴다. 순서(ShelfGraspSequence)는 누가 듣는지 모르고 이 계약만 부른다.
    /// </summary>
    public interface IGraspListener
    {
        /// <summary>request의 물건을 쥐었다. 쥐지 못했을 때는 부르지 않는다.</summary>
        void ItemGrasped(ItemPickRequest request);
    }
}
