namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 그리퍼를 닫은 뒤 물리로 쥐었는지 가린다. 쥐지 못했다면 사람이 읽는 이유를 돌려주고, 쥐었으면 빈 글자를 돌려준다.
    /// 쥔 것으로 처리된 경우(조인트가 붙은 경우)에도, 손가락이 물건에 닿아 멈추지 않고 끝까지 닫혔다면 손가락 사이에 물건이 없는 것이다.
    /// 순수 판정이라 씬 없이 시험한다.
    /// </summary>
    public static class PhysicalGraspVerdict
    {
        /// <param name="isHolding">그리퍼가 물건을 붙잡은 것으로 처리했는지.</param>
        /// <param name="canSenseContact">손가락이 물건에 닿았는지 알 수 있는지. 모르면 붙잡은 것만 믿는다.</param>
        /// <param name="isBlockedByObject">손가락이 닫히다 물건에 걸려 멈췄는지.</param>
        public static string FailureReason(bool isHolding, bool canSenseContact, bool isBlockedByObject)
        {
            if (!isHolding)
            {
                return "도구 끝점 가까이에 쥘 물건이 없다(그리퍼를 닫았지만 붙잡히지 않았다)";
            }

            if (canSenseContact && !isBlockedByObject)
            {
                return "손가락이 물건에 닿지 않고 끝까지 닫혔다(물건이 손가락 사이에 있지 않다)";
            }

            return string.Empty;
        }
    }
}
