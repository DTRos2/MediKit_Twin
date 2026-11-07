using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 키팅 로그 패널 프리팹(PF_KittingLogPanel)을 만든다. 머리줄(복사 알림, 경고·오류만, 복사, 지우기, 접기 표시)과 로그 줄 60개가 모두
    /// 계층의 오브젝트로 들어가고, 화면 코드(KittingLogPanelView)와 동작 코드(KittingLogPanelPresenter)가 그 오브젝트들을 연결한다.
    /// 로그 줄은 시간, 분류 태그, 글로 이루어지며 실행 중에는 최근 로그로 내용만 바꿔 채운다.
    /// 위치와 처음 접힘은 IMGUI 시절과 같은 자리 표(DemoPanelSlots.Log)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class KittingLogPanelPrefabBuilder
    {
        private const string RootName = "PF_KittingLogPanel";

        public static KittingLogPanelPresenter Build()
        {
            DemoPanelSlot slot = DemoPanelSlots.Log;

            UiPanelFrame.Frame frame = UiPanelFrame.Build(RootName, slot, KittingLogPanelTexts.Title, KittingLogPanelTexts.Subtitle);
            UiKindButton filter = UiWidgets.AddHeaderButton(
                frame.Header, "FilterButton", KittingLogPanelTexts.Filter, ButtonKind.Secondary, DashboardMetrics.HeaderFilterButtonWidth);
            UiKindButton copy = UiWidgets.AddHeaderButton(
                frame.Header, "CopyButton", KittingLogPanelTexts.Copy, ButtonKind.Secondary, DashboardMetrics.HeaderButtonWidth);
            UiKindButton clear = UiWidgets.AddHeaderButton(
                frame.Header, "ClearButton", KittingLogPanelTexts.Clear, ButtonKind.Secondary, DashboardMetrics.HeaderButtonWidth);

            UiWidgets.ScrollView body = UiPanelFrame.AddScrollBody(frame, DashboardMetrics.LogRowSpacing);
            UiCollapsiblePanel panel = UiPanelFrame.AddCollapse(frame, body.Root.gameObject, slot.StartsCollapsed);

            for (int i = 0; i < KittingLogPanelTexts.RowCapacity; i++)
            {
                AddRow(body.Content, i + 1);
            }

            var view = frame.Root.AddComponent<KittingLogPanelView>();
            SerializedFieldAssigner.SetReference(view, "_panel", panel);
            SerializedFieldAssigner.SetReference(view, "_notice", frame.Summary);
            SerializedFieldAssigner.SetReference(view, "_filterButton", filter);
            SerializedFieldAssigner.SetReference(view, "_copyButton", copy);
            SerializedFieldAssigner.SetReference(view, "_clearButton", clear);
            SerializedFieldAssigner.SetReference(view, "_scroll", body.Root.GetComponent<ScrollRect>());

            var presenter = frame.Root.AddComponent<KittingLogPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<KittingLogPanelPresenter>(frame.Root, ProjectAssetPaths.KittingLogPanelPrefab);
        }

        // 로그 한 줄: 둥근 바탕에 시간, 태그, 글이 가로로 놓인다. 글이 줄바꿈되면 줄 높이가 따라 커진다.
        private static void AddRow(Transform parent, int number)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect($"LogRow_{number}", parent);
            Image background = UiPrimitives.AddImage(root, palette.Field, DashboardMetrics.LogRowRadius);
            int sidePadding = Mathf.RoundToInt(DashboardMetrics.LogRowPaddingX);
            int verticalPadding = Mathf.RoundToInt(DashboardMetrics.LogRowPaddingY);
            UiPrimitives.AddRow(
                root, DashboardMetrics.LogRowGap, TextAnchor.UpperLeft,
                new RectOffset(sidePadding, sidePadding, verticalPadding, verticalPadding));
            UiPrimitives.SetLayout(root, minHeight: DashboardMetrics.LogRowMinHeight);

            Text time = UiPrimitives.AddLabel(root, "Time", string.Empty, TextRole.Mono, palette.Muted);
            UiPrimitives.SetLayout(
                (RectTransform)time.transform, preferredWidth: DashboardMetrics.LogRowTimeWidth, preferredHeight: DashboardMetrics.TagHeight);

            UiTag tag = UiWidgets.AddTag(root, "Tag", string.Empty);

            Text message = UiPrimitives.AddLabel(
                root, "Message", string.Empty, TextRole.Body, palette.Ink, TextAnchor.MiddleLeft, isWrapped: true);
            UiPrimitives.SetLayout(
                (RectTransform)message.transform, preferredWidth: 0f, flexibleWidth: 1f, minHeight: DashboardMetrics.TagHeight);

            var item = root.gameObject.AddComponent<LogRowItem>();
            SerializedFieldAssigner.SetReference(item, "_background", background);
            SerializedFieldAssigner.SetReference(item, "_time", time);
            SerializedFieldAssigner.SetReference(item, "_tag", tag);
            SerializedFieldAssigner.SetReference(item, "_message", message);
        }
    }
}
