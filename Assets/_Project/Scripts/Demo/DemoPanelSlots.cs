using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 기준 화면(1920×1080)에서 패널들이 놓이는 기본 자리. 한 곳에 모아 두어 패널끼리 겹치지 않는지 한눈에 보고 시험한다.
    /// 배치 원칙: 화면 가운데는 로봇이 보이도록 비우고, 왼쪽 열(주문, 선반, 카메라)과 오른쪽 열(키팅 현황, 관절 제어)에
    /// 같은 간격으로 쌓는다. 가끔 보는 것(관절 제어, 로그)은 접어 둔 채로 시작해서 평소 화면이 어수선하지 않게 한다.
    /// </summary>
    public static class DemoPanelSlots
    {
        private const float LeftColumnWidth = 360f;
        private const float RightColumnWidth = 380f;
        private const float OrdersHeight = 360f;
        private const float ShelfHeight = 400f;
        private const float CameraHeight = 264f;
        // 공정 현황은 키트 단계, 설비 칩 3줄, 숫자 칸 4개가 들어가 한 설비 패널일 때보다 40px 높다. 오른쪽 열 아래 끝을 맞추려고 관절 제어를 그만큼 줄였다.
        private const float StatusHeight = 400f;
        private const float ConveyorHeight = 152f;
        // 배속 패널: 머리줄 + 버튼 한 줄 + 아래 여백.
        private const float SimSpeedHeight = DashboardMetrics.HeaderHeight + DashboardMetrics.ControlHeight + DashboardMetrics.CardPadding;
        // 오른쪽 열에 쌓이는 네 패널(키팅 현황, 컨베이어, 배속, 관절 제어)의 높이 합. 관절 제어가 나머지를 채워 열 아래 끝이 화면 아래 여백에 맞는다.
        private const float RightColumnPanelsHeight = 1024f;
        private const float JointsHeight = RightColumnPanelsHeight - StatusHeight - ConveyorHeight - SimSpeedHeight - DashboardMetrics.PanelGap;
        private const float LogWidth = 760f;
        private const float LogHeight = 232f;

        private static float Margin => DashboardMetrics.ScreenMargin;

        private static float Gap => DashboardMetrics.PanelGap;

        public static DemoPanelSlot Orders =>
            new DemoPanelSlot(DemoPanelAnchor.TopLeft, new Vector2(Margin, Margin), new Vector2(LeftColumnWidth, OrdersHeight));

        public static DemoPanelSlot Shelf =>
            new DemoPanelSlot(DemoPanelAnchor.TopLeft, new Vector2(Margin, Margin + OrdersHeight + Gap), new Vector2(LeftColumnWidth, ShelfHeight));

        public static DemoPanelSlot Camera =>
            new DemoPanelSlot(DemoPanelAnchor.BottomLeft, new Vector2(Margin, Margin), new Vector2(LeftColumnWidth, CameraHeight));

        public static DemoPanelSlot Status =>
            new DemoPanelSlot(DemoPanelAnchor.TopRight, new Vector2(Margin, Margin), new Vector2(RightColumnWidth, StatusHeight));

        // 컨베이어 속도는 키팅 현황 바로 아래에 둔다. 한눈에 속도를 보며 조절할 수 있고, 가운데 띠(로봇이 보이는 자리)를 가리지 않는다.
        public static DemoPanelSlot Conveyor =>
            new DemoPanelSlot(
                DemoPanelAnchor.TopRight,
                new Vector2(Margin, Margin + StatusHeight + Gap),
                new Vector2(RightColumnWidth, ConveyorHeight));

        // 공정 전체 배속 버튼은 컨베이어 속도 바로 아래에 둔다. 속도를 다루는 패널끼리 모아 두고, 가운데 띠(로봇이 보이는 자리)를 가리지 않는다.
        public static DemoPanelSlot SimSpeed =>
            new DemoPanelSlot(
                DemoPanelAnchor.TopRight,
                new Vector2(Margin, Margin + StatusHeight + Gap + ConveyorHeight + Gap),
                new Vector2(RightColumnWidth, SimSpeedHeight));

        // 관절 제어는 내용이 길면 안에서 스크롤한다. 위 패널들이 차지한 만큼 높이를 줄여 오른쪽 열이 화면 아래 여백까지만 오게 했다.
        public static DemoPanelSlot Joints =>
            new DemoPanelSlot(
                DemoPanelAnchor.TopRight,
                new Vector2(Margin, Margin + StatusHeight + Gap + ConveyorHeight + Gap + SimSpeedHeight + Gap),
                new Vector2(RightColumnWidth, JointsHeight),
                startsCollapsed: true);

        public static DemoPanelSlot Log =>
            new DemoPanelSlot(DemoPanelAnchor.BottomCenter, new Vector2(0f, Margin), new Vector2(LogWidth, LogHeight), startsCollapsed: true);
    }
}
