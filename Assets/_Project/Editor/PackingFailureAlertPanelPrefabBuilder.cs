using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 팩킹 실패 경고 패널 프리팹(PF_PackingFailureAlertPanel)을 만든다. 머리줄, 키트 번호를 알리는 줄, 실패 이유, 안내, "확인하고 다시 덮기" 버튼이
    /// 모두 계층의 오브젝트로 들어간다. 평소에는 보이지 않아야 하므로 루트에 CanvasGroup을 두고 감춘 채로 시작한다(꺼 두면 사건을 받지 못한다).
    /// 위치는 IMGUI 시절과 같은 자리 표(DemoPanelSlots.PackingAlert)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class PackingFailureAlertPanelPrefabBuilder
    {
        private const string RootName = "PF_PackingFailureAlertPanel";

        public static PackingFailureAlertPanelPresenter Build()
        {
            DashboardPalette palette = DashboardPalette.Default;
            UiPanelFrame.Frame frame = UiPanelFrame.Build(
                RootName, DemoPanelSlots.PackingAlert, PackingFailureAlertPanelTexts.Title, PackingFailureAlertPanelTexts.Subtitle);

            RectTransform body = UiPanelFrame.AddColumnBody(frame, DashboardMetrics.BlockGap);
            Text headline = UiPrimitives.AddLabel(body, "Headline", string.Empty, TextRole.BodyBold, palette.Ink, isWrapped: true);
            Text reason = UiPrimitives.AddLabel(body, "Reason", string.Empty, TextRole.Body, palette.Muted, isWrapped: true);
            UiPrimitives.AddLabel(body, "Guidance", PackingFailureAlertPanelTexts.Guidance, TextRole.Body, palette.Muted, isWrapped: true);
            UiKindButton retryButton = UiWidgets.AddKindButton(
                body, "RetryButton", PackingFailureAlertPanelTexts.RetryButton, ButtonKind.Primary, DashboardMetrics.ControlHeight);

            CanvasGroup group = frame.Root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var view = frame.Root.AddComponent<PackingFailureAlertPanelView>();
            SerializedFieldAssigner.SetReference(view, "_group", group);
            SerializedFieldAssigner.SetReference(view, "_headline", headline);
            SerializedFieldAssigner.SetReference(view, "_reason", reason);
            SerializedFieldAssigner.SetReference(view, "_retryButton", retryButton);

            var presenter = frame.Root.AddComponent<PackingFailureAlertPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<PackingFailureAlertPanelPresenter>(frame.Root, ProjectAssetPaths.PackingFailureAlertPanelPrefab);
        }
    }
}
