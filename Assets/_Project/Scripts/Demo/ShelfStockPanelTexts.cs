namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 재고 패널에 나오는 글. 프리팹을 만드는 도구와 화면 코드와 동작 코드가 같은 문구를 쓰도록 한곳에 모았다.
    /// Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ShelfStockPanelTexts
    {
        public const string Title = "선반 재고";
        public const string Subtitle = "SHELF · 3×4";
        public const string Unit = "개";
        public const string Guide = "칸을 눌러 재고를 확인하세요 · 수동 키팅 모드에서는 로봇이 집습니다";
        public const string ManualTitle = "수동 키팅 모드";
        public const string ManualDescription = "칸을 누르면 로봇이 집어 박스에 담음";
        public const string ReturnTitle = "되돌리기 모드";
        public const string ReturnDescription = "끄면 집기 · 켜면 박스에서 선반으로 반환";
        public const string NewBox = "새 박스";

        /// <summary>머리줄 오른쪽 요약: 선반 전체 재고 수.</summary>
        public static string Total(int total)
        {
            return $"총 {total}{Unit}";
        }

        /// <summary>칸이 보충을 기다리는 동안 칸 귀퉁이에 적는 남은 시간. 칸이 좁아서 "초"만 붙인다(재고가 0인 칸에만 나온다).</summary>
        public static string RefillNote(float remainingSeconds)
        {
            return $"{remainingSeconds:0}초";
        }

        public static string Taken(string shelfSlotId, int quantity)
        {
            return $"{shelfSlotId}: 집음 → {quantity}{Unit}";
        }

        public static string Returned(string shelfSlotId, int quantity)
        {
            return $"{shelfSlotId}: 되돌림 → {quantity}{Unit}";
        }

        public static string OutOfStock(string shelfSlotId)
        {
            return $"{shelfSlotId}: 재고 없음 (StockEmpty 발행, 보충 대기 중)";
        }
    }
}
