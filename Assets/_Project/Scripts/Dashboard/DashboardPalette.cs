using System;
using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 대시보드가 쓰는 색 이름표. 색 값을 위젯마다 흩어 두지 않고 역할 이름(강조, 글자, 바탕, 성공 …)으로 한곳에 모은다.
    /// 스마트팩토리 관제 화면(HMI)처럼 어두운 강철빛 바탕에 밝은 글자, 산업용 청록 강조색, 신호등 같은 상태색(초록·호박·빨강)을 쓴다.
    /// 밤낮 없이 켜 두는 공장 화면에서 눈이 덜 피로하고, 이상(경고·오류) 색이 어두운 바탕에서 가장 먼저 눈에 띄게 하려는 것이다.
    /// 색을 바꾸려면 이 클래스만 고치면 된다. 글자와 바탕의 대비는 DashboardReadabilityTests가 지킨다.
    /// </summary>
    public sealed class DashboardPalette
    {
        public static DashboardPalette Default { get; } = new DashboardPalette();

        // 카드(패널 바탕): 완전히 불투명하다. 뒤의 3D 화면이 비치면 글자와 위젯이 읽기 어렵다.
        public Color Card { get; } = Hex("#0F1A26");
        public Color CardBorder { get; } = Hex("#2B3D52");

        // 글자
        public Color Ink { get; } = Hex("#E8EEF5");
        public Color InkSoft { get; } = Hex("#BDCADA");
        public Color Muted { get; } = Hex("#8FA3BA");
        public Color Faint { get; } = Hex("#7C90A8");
        public Color OnAccent { get; } = Hex("#04202B");

        // 바탕 면
        public Color Surface { get; } = Hex("#162334");
        public Color SurfaceBorder { get; } = Hex("#2B3D52");
        public Color Field { get; } = Hex("#0B141F");
        public Color Track { get; } = Hex("#2A3B50");
        public Color Thumb { get; } = Hex("#E8EEF5");

        // 마우스를 올리거나 누른 위젯의 바탕에 살짝 섞는 밝은색(InteractionShade가 쓴다)
        public Color Highlight { get; } = Hex("#FFFFFF");

        // 강조와 상태
        public Color Accent { get; } = Hex("#22D3EE");
        public Color AccentSoft { get; } = Hex("#0E3A47");
        public Color AccentThumb { get; } = Hex("#67E8F9");
        public Color Success { get; } = Hex("#34D399");
        public Color SuccessSoft { get; } = Hex("#0E3B2C");
        public Color Warning { get; } = Hex("#FBBF24");
        public Color WarningSoft { get; } = Hex("#46340A");
        public Color Danger { get; } = Hex("#F87171");
        public Color DangerSoft { get; } = Hex("#4B1B22");

        // 키트 종류 색(주문 배지, 선반 행 띠). Kit*은 띠와 배지 바탕, Kit*Ink는 어두운 카드 위에서 읽히도록 밝게 한 글자색이다.
        public Color KitEm { get; } = Hex("#FB923C");
        public Color KitDx { get; } = Hex("#60A5FA");
        public Color KitBl { get; } = Hex("#A78BFA");
        public Color KitEmInk { get; } = Hex("#FDBA74");
        public Color KitDxInk { get; } = Hex("#93C5FD");
        public Color KitBlInk { get; } = Hex("#C4B5FD");

        // 카메라 화면 위에 올리는 표시: 어두운 반투명 칩과 청록 조준선(계기판 느낌). 칩 글자는 Ink(밝은색)를 쓴다.
        public Color CameraOverlay { get; } = Hex("#0A121CD9");
        public Color CameraFrame { get; } = Hex("#7FE7F7");

        /// <summary>"#RRGGBB" 또는 "#RRGGBBAA" 글자를 색으로 바꾼다. 형식이 틀리면 예외다(색 값의 오타를 조용히 넘기지 않는다).</summary>
        public static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                throw new ArgumentException($"색 글자 '{hex}'를 읽을 수 없다. #RRGGBB 또는 #RRGGBBAA 형식이어야 한다.", nameof(hex));
            }

            return color;
        }
    }
}
