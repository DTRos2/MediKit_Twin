namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 선반 칸 ID를 비교할 때 쓰는 표준 모양. 앞뒤 공백을 떼고 대문자로 맞춰 "a-1"과 " A-1 "을 같은 칸으로 본다.
    /// 칸 ID를 저장하는 쪽과 찾는 쪽이 같은 규칙을 쓰도록 한 곳에 둔다.
    /// </summary>
    public static class PickSlotKey
    {
        public static string Normalize(string slotId)
        {
            return slotId == null ? string.Empty : slotId.Trim().ToUpperInvariant();
        }
    }
}
