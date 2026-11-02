namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건 앞(접근 자리)에 닿은 뒤, 쥐러 들어가기 전에 집는 자리를 고치는 쪽. 순서(ShelfGraspSequence)는 어떻게 고치는지 모르고 이 계약만 부른다.
    /// 고칠 수 없으면 계획한 자리를 그대로 돌려준다.
    /// </summary>
    public interface IGraspRefiner
    {
        GraspRefinement Refine(ItemPickRequest request, GraspTargets planned);
    }
}
