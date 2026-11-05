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

        // 테두리 굵기. uGUI 위젯이 쓴다(IMGUI 위젯은 각자 가진 같은 값을 쓴다).
        public const float BorderWidth = 1f;

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
        public const float SliderTrackHeight = 4f;
        public const float SliderThumbSize = 16f;
        public const float SliderThumbBorder = 3f;
        public const float SliderNameWidth = 44f;
        public const float SliderValueWidth = 84f;
        public const float SliderGap = 6f;

        // 칩과 배지
        public const float ChipHeight = 24f;
        public const float ChipPaddingX = 10f;
        public const float ChipRadius = 8f;
        public const float BadgeWidth = 44f;
        public const float BadgeHeight = 24f;
        public const float BadgeRadius = 8f;
        public const float TagWidth = 92f;

        // 숫자 칸(완료, 불량품 같은 이름표와 큰 숫자가 든 칸)
        public const float StatTileHeight = 76f;
        public const float StatTilePaddingX = 14f;
        public const float StatTilePaddingTop = 10f;
        public const float StatTilePaddingBottom = 8f;
        public const float StatTileCaptionHeight = 18f;
        public const float StatTileValueHeight = 40f;
        public const float StatTileRadius = RowRadius + 2f;

        // 스위치 줄의 안쪽 간격(제목, 설명 글의 위치)
        public const float SwitchRowPaddingX = 14f;
        public const float SwitchRowTitleTop = 8f;
        public const float SwitchRowTitleHeight = 20f;
        public const float SwitchRowDescriptionTop = 28f;
        public const float SwitchRowDescriptionHeight = 18f;

        // 선반 칸 타일과 행
        public const float StockTileHeight = 60f;
        public const float StockTileStripHeight = 4f;
        public const float StockTileStripInset = 6f;
        public const float StockTileTextInset = 8f;
        public const float StockTileIdTop = 8f;
        public const float StockTileIdHeight = 14f;
        public const float StockTileQuantityTop = 22f;
        public const float StockTileQuantityHeight = 30f;
        public const float StockTileUnitGap = 3f;
        public const float StockTileRadius = 12f;
        public const int StockTileQuantityFontSize = BigNumberFontSize - 6;
        public const float StockRowLabelWidth = 40f;
        public const float StockRowLetterHeight = 24f;
        public const float StockRowKitCodeHeight = 16f;

        // 머리줄 끝의 접힘 표시(−, +)
        public const float HeaderToggleSize = 18f;
        public const float HeaderToggleRadius = 6f;

        // 눌러 볼 수 있는 칩에 마우스를 올리거나 누를 때 칩 색에 곱하는 밝기(1이면 그대로)
        public const float ChipHoverShade = 0.85f;
        public const float ChipPressedShade = 0.7f;

        // 키 안내(키 모양 칸과 그 뜻)
        public const float KeyCapHeight = 22f;
        public const float KeyCapPaddingX = 8f;
        public const float KeyCapRadius = 6f;
        public const float KeyToMeaningGap = 6f;
        public const float KeyHintGap = 14f;

        // 한 줄 글 입력칸(칸 ID처럼 짧은 글)
        public const float ShortFieldWidth = 64f;

        // 머리줄에 놓는 작은 단추의 폭
        public const float HeaderButtonWidth = 64f;
        public const float HeaderFilterButtonWidth = 104f;

        // 태그(로그 줄의 READY, CAMERA …)
        public const float TagHeight = 22f;
        public const float TagRadius = 6f;

        // 로그 줄
        public const float LogRowTimeWidth = 52f;
        public const float LogRowPaddingX = 12f;
        public const float LogRowPaddingY = 4f;
        public const float LogRowGap = 10f;
        public const float LogRowMinHeight = 30f;
        public const float LogRowRadius = 8f;
        public const float LogRowSpacing = 4f;

        // 상태 줄(색 점과 한 줄 글)
        public const float StatusLineHeight = 22f;
        public const float StatusDotSize = 7f;
        public const float StatusDotGap = 9f;

        // 툴 카메라 화면과 그 위에 겹치는 표시
        public const float CameraImageHeight = 168f;
        public const float CameraBracketInset = 12f;
        public const float CameraBracketLength = 18f;
        public const float CameraBracketThickness = 2f;
        public const float CameraCrosshairRadius = 14f;
        public const float CameraCrosshairWidth = 1.5f;
        public const float CameraCrosshairDotRadius = 1.5f;
        public const float CameraChipInset = 10f;
        public const float CameraChipHeight = 22f;
        public const float CameraChipPaddingX = 8f;
        public const float CameraChipRadius = 6f;
        public const float CameraMarkerSize = 28f;
        public const float CameraMarkerLine = 2f;

        // 메인 화면에 겹치는 인식 표시(물건 자리의 네모와 글)
        public const float RecognitionMarkerSize = 36f;
        public const float RecognitionMarkerLine = 3f;
        public const float RecognitionLabelWidth = 160f;
        public const float RecognitionLabelHeight = 20f;
        public const float RecognitionLabelGap = 2f;

        // 구간 나누는 가는 선
        public const float DividerHeight = 1f;

        // 스크롤 막대
        public const float ScrollbarWidth = 6f;

        // 글자 크기
        public const int TitleFontSize = 17;
        public const int BodyFontSize = 14;
        public const int SmallFontSize = 12;
        public const int MonoFontSize = 12;
        public const int BigNumberFontSize = 30;
        public const int StateFontSize = 20;

        // 지금 단계를 알리는 큰 글 한 줄이 차지하는 높이. 글자 크기보다 조금 크게 정해, 아래 글과 겹치지 않고 위아래가 딱 맞는다.
        public const float StateLineHeight = 28f;

        // 비활성일 때 그림에 곱하는 투명도
        public const float DisabledAlpha = 0.5f;
    }
}
