using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>주문 한 건의 목록 줄 프리팹(PF_OrderRowItem)을 만든다: 순번, 병원 이름, 키트 종류 배지, 수량이 한 줄에 놓인다.</summary>
    public static class OrderRowItemPrefabBuilder
    {
        /// <summary>줄 높이와 줄 사이 간격. 패널 빌더가 목록의 최대 높이를 계산할 때도 같은 값을 쓴다.</summary>
        public const float RowHeight = 34f;
        public const float RowSpacing = 4f;

        private const float SidePadding = 12f;
        private const float SequenceWidth = 28f;
        private const float QuantityWidth = 36f;
        private const float ColumnGap = 8f;

        // 견본 글. 실제 값은 실행할 때 OrderRowItem이 채우지만, 편집기에서 줄 모양을 알아볼 수 있도록 넣어 둔다.
        private const string SampleSequence = "1";
        private const string SampleHospital = "병원";
        private const string SampleKitCode = "EM";
        private const int SampleQuantity = 1;

        public static OrderRowItem Build()
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect("PF_OrderRowItem", null);
            UiPrimitives.AddImage(root, palette.Surface, DashboardMetrics.RowRadius);
            UiPrimitives.SetLayout(root, preferredHeight: RowHeight);
            int side = Mathf.RoundToInt(SidePadding);
            UiPrimitives.AddRow(root, ColumnGap, TextAnchor.MiddleLeft, new RectOffset(side, side, 0, 0));

            // IMGUI 줄은 순번 칸(SequenceWidth) 바로 뒤에서 이름이 시작한다. 배치 그룹은 모든 칸 사이에 같은 간격을 두므로,
            // 순번 칸을 그 간격만큼 줄여야 이름이 같은 자리에서 시작한다.
            Text sequence = UiPrimitives.AddLabel(
                root, "Sequence", SampleSequence, TextRole.Mono, palette.Faint, preferredWidth: SequenceWidth - ColumnGap);
            Text hospital = UiPrimitives.AddLabel(root, "Hospital", SampleHospital, TextRole.BodyBold, palette.Ink, flexibleWidth: 1f);

            RectTransform badgeRect = UiPrimitives.CreateRect("Badge", root);
            Image badge = UiPrimitives.AddImage(badgeRect, palette.KitEm, DashboardMetrics.BadgeRadius);
            UiPrimitives.SetLayout(badgeRect, preferredWidth: DashboardMetrics.BadgeWidth, preferredHeight: DashboardMetrics.BadgeHeight);
            RectTransform kitCodeRect = UiPrimitives.CreateRect("KitCode", badgeRect);
            UiPrimitives.Stretch(kitCodeRect);
            Text kitCode = UiPrimitives.AddText(kitCodeRect, SampleKitCode, TextRole.MonoBold, palette.OnAccent, TextAnchor.MiddleCenter);

            Text quantity = UiPrimitives.AddLabel(
                root, "Quantity", OrderPanelTexts.Quantity(SampleQuantity), TextRole.Mono, palette.InkSoft, TextAnchor.MiddleRight,
                preferredWidth: QuantityWidth);

            var item = root.gameObject.AddComponent<OrderRowItem>();
            SerializedFieldAssigner.SetReference(item, "_sequence", sequence);
            SerializedFieldAssigner.SetReference(item, "_hospital", hospital);
            SerializedFieldAssigner.SetReference(item, "_badge", badge);
            SerializedFieldAssigner.SetReference(item, "_kitCode", kitCode);
            SerializedFieldAssigner.SetReference(item, "_quantity", quantity);
            return UiPrefabWriter.SaveAndLoad<OrderRowItem>(root.gameObject, ProjectAssetPaths.OrderRowItemPrefab);
        }
    }
}
