namespace MediKitTwin.ArmA
{
    /// <summary>알려 준 집는 자세를 칸 ID로 찾는 쪽. 집으러 가는 쪽(ArmItemTransfer)은 읽기만 하므로 이것만 안다.</summary>
    public interface IPickPoseSource
    {
        /// <summary>그 칸에 알려 준 자세가 있으면 true.</summary>
        bool TryGet(string slotId, out TaughtPickPose pose);
    }
}
