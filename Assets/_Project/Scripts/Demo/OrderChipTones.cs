using MediKitTwin.Dashboard;

namespace MediKitTwin.Demo
{
    /// <summary>주문 패널의 칩 색 계열을 고른다. 경고가 없으면 초록, 있으면 호박색으로 알린다. Unity 없이 시험할 수 있다.</summary>
    public static class OrderChipTones
    {
        public static ChipTone ForIssues(int issueCount)
        {
            return issueCount == 0 ? ChipTone.Success : ChipTone.Warning;
        }
    }
}
