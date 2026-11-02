namespace MediKitTwin.ArmA
{
    /// <summary>이송이 실패했을 때 "어느 단계에서"를 알리는 단계 이름. 집기와 놓기, 서는 자리 탐색이 같은 이름을 쓴다.</summary>
    public static class TransferSteps
    {
        public const string ShelfApproachWaypoint = "선반 칸 정면 경유지로 이동";
        public const string ShelfStandPoint = "선반 앞 서는 자리로 이동";
        public const string ShelfNear = "물건 앞쪽으로 팔 뻗기";
        public const string ShelfReach = "물건 위치로 팔 뻗기";
        public const string ShelfRetreat = "물건을 쥔 채 뒤로 빼기";
        public const string BoxStandPoint = "박스 곁 서는 자리로 이동";
        public const string BoxAbove = "박스 슬롯 위로 팔 뻗기";
        public const string BoxSlot = "박스 슬롯으로 내리기";
        public const string BoxLift = "놓은 뒤 들어올리기";
    }
}
