namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 대시보드의 크기·간격·글자 크기 값(픽셀). 기준 화면(1920×1080)에서의 값이며, 화면이 다르면 DashboardScale로 한꺼번에 키우고 줄인다.
    /// 위젯 안에 숫자를 박아 두지 않고 이름을 붙여 한곳에 둔다.
    /// </summary>
    public static class DashboardMetrics
    {
        // 기준 화면
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        // 카드(패널)
        public const float CardRadius = 16f;
        public const float CardPadding = 16f;
        public const float HeaderHeight = 46f;
        public const float AccentBarWidth = 4f;
        public const float AccentBarHeight = 16f;

        // 화면 가장자리와 패널 사이 간격
        public const float ScreenMargin = 16f;
        public const float PanelGap = 12f;

        // 줄과 블록
        public const float RowRadius = 10f;
        public const float RowGap = 6f;
        public const float BlockGap = 8f;
        public const float SectionGap = 14f;
        public const float ControlHeight = 34f;
        public const float SmallControlHeight = 26f;
        public const float IconButtonSize = 34f;
        public const float FieldPaddingX = 10f;
        public const float FieldPaddingY = 8f;

        // 스위치
        public const float SwitchWidth = 44f;
        public const float SwitchHeight = 24f;
        public const float SwitchThumbInset = 3f;
        public const float SwitchRowHeight = 52f;
        public const float SwitchRowTitleOnlyHeight = 40f;

        // 슬라이더
        public const float SliderHeight = 34f;
        public const float SliderTrackHeight = 6f;
        public const float SliderThumbSize = 20f;
        public const float SliderThumbBorder = 3f;

        // 칩과 배지
        public const float ChipHeight = 24f;
        public const float ChipPaddingX = 10f;
        public const float ChipRadius = 8f;
        public const float BadgeWidth = 44f;
        public const float TagWidth = 92f;

        // 스크롤 막대
        public const float ScrollbarWidth = 6f;

        // 글자 크기
        public const int TitleFontSize = 17;
        public const int BodyFontSize = 14;
        public const int SmallFontSize = 12;
        public const int MonoFontSize = 12;
        public const int BigNumberFontSize = 30;
        public const int StateFontSize = 28;

        // 비활성일 때 그림에 곱하는 투명도
        public const float DisabledAlpha = 0.5f;
    }
}
