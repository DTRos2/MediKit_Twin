using MediKitTwin.Dashboard;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 패널 카드 위쪽의 머리줄을 만든다: 강조 막대, 제목, 영문 라벨, 오른쪽 끝의 글. 패널 프리팹 빌더들이 같은 머리줄을 쓰도록 한곳에 둔다.
    /// </summary>
    public static class UiPanelHeader
    {
        /// <summary>머리줄을 카드 위쪽에 붙이고, 오른쪽 끝 글(요약이나 현재 값을 보여 줄 자리)을 돌려준다. 이 글은 남는 폭을 모두 차지해 오른쪽 끝에 붙는다.</summary>
        public static Text Build(Transform card, string title, string subtitle)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform header = UiPrimitives.CreateRect("Header", card);
            UiPrimitives.AnchorTopStrip(header, DashboardMetrics.HeaderHeight);
            int side = Mathf.RoundToInt(DashboardMetrics.CardPadding);
            UiPrimitives.AddRow(header, DashboardMetrics.BlockGap, TextAnchor.MiddleLeft, new RectOffset(side, side, 0, 0));

            RectTransform bar = UiPrimitives.CreateRect("AccentBar", header);
            UiPrimitives.AddImage(bar, palette.Accent, DashboardMetrics.AccentBarWidth * 0.5f);
            UiPrimitives.SetLayout(bar, preferredWidth: DashboardMetrics.AccentBarWidth, preferredHeight: DashboardMetrics.AccentBarHeight);

            UiPrimitives.AddLabel(header, "Title", title, TextRole.Title, palette.Ink);
            UiPrimitives.AddLabel(header, "Subtitle", subtitle, TextRole.Subtitle, palette.Faint);
            return UiPrimitives.AddLabel(
                header, "Summary", string.Empty, TextRole.Mono, palette.Muted, TextAnchor.MiddleRight, flexibleWidth: 1f);
        }
    }
}
