using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 컨베이어 속도 패널 프리팹(PF_ConveyorSpeedPanel)을 만든다. 머리줄(제목, 현재 속도), 속도 슬라이더 줄(−, 슬라이더, +, 값),
    /// "처음 속도로" 버튼이 모두 계층의 오브젝트로 들어가고, 화면 코드(ConveyorSpeedPanelView)와 동작 코드(ConveyorSpeedPanelPresenter)가 연결한다.
    /// 위치는 IMGUI 시절과 같은 자리 표(DemoPanelSlots.Conveyor)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class ConveyorSpeedPanelPrefabBuilder
    {
        private const string RootName = "PF_ConveyorSpeedPanel";

        public static ConveyorSpeedPanelPresenter Build()
        {
            UiPanelFrame.Frame frame = UiPanelFrame.Build(
                RootName, DemoPanelSlots.Conveyor, ConveyorSpeedPanelTexts.Title, ConveyorSpeedPanelTexts.Subtitle);
            Text current = frame.Summary;
            RectTransform body = UiPanelFrame.AddColumnBody(frame, DashboardMetrics.BlockGap);

            UiSliderRow speedRow = UiWidgets.AddSliderRow(body, "SpeedRow", ConveyorSpeedPanelTexts.SpeedLabel);

            RectTransform buttons = UiPrimitives.CreateRect("Buttons", body);
            UiPrimitives.AddRow(buttons, DashboardMetrics.BlockGap);
            UiPrimitives.SetLayout(buttons, preferredHeight: DashboardMetrics.ControlHeight);
            UiKindButton reset = UiWidgets.AddKindButton(
                buttons, "ResetButton", ConveyorSpeedPanelTexts.Reset(0f), ButtonKind.Secondary, DashboardMetrics.ControlHeight);

            var view = frame.Root.AddComponent<ConveyorSpeedPanelView>();
            SerializedFieldAssigner.SetReference(view, "_current", current);
            SerializedFieldAssigner.SetReference(view, "_speedRow", speedRow);
            SerializedFieldAssigner.SetReference(view, "_resetButton", reset);

            var presenter = frame.Root.AddComponent<ConveyorSpeedPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<ConveyorSpeedPanelPresenter>(frame.Root, ProjectAssetPaths.ConveyorSpeedPanelPrefab);
        }
    }
}
