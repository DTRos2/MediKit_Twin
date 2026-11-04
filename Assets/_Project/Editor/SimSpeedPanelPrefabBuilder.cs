using System.Globalization;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 배속 패널 프리팹(PF_SimSpeedPanel)을 만든다. 머리줄(제목, 현재 배속)과 배속 버튼 한 줄이 모두 계층의 오브젝트로 들어간다.
    /// 버튼은 SimClock이 허용하는 배속(AllowedSpeeds)마다 하나씩 만들어지므로, 허용 배속을 바꾸면 이 빌더를 다시 돌리기만 하면 된다.
    /// 위치는 IMGUI 시절과 같은 자리 표(DemoPanelSlots.SimSpeed)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class SimSpeedPanelPrefabBuilder
    {
        private const string RootName = "PF_SimSpeedPanel";
        private const string ButtonNamePrefix = "Speed_";

        public static SimSpeedPanelPresenter Build()
        {
            UiPanelFrame.Frame frame = UiPanelFrame.Build(RootName, DemoPanelSlots.SimSpeed, SimSpeedPanelTexts.Title, SimSpeedPanelTexts.Subtitle);
            Text current = frame.Summary;

            RectTransform buttonRow = UiPrimitives.CreateRect("Buttons", frame.Card.Fill);
            UiPrimitives.Stretch(
                buttonRow, DashboardMetrics.CardPadding, DashboardMetrics.HeaderHeight, DashboardMetrics.CardPadding, DashboardMetrics.CardPadding);
            UiPrimitives.AddRow(buttonRow, DashboardMetrics.BlockGap);

            foreach (float speed in SimClock.AllowedSpeeds)
            {
                string name = string.Format(CultureInfo.InvariantCulture, "{0}{1:0.##}", ButtonNamePrefix, speed);
                UiKindButton button = UiWidgets.AddKindButton(
                    buttonRow, name, SimSpeedPanelTexts.Speed(speed), ButtonKind.Secondary, DashboardMetrics.ControlHeight);

                var speedButton = button.gameObject.AddComponent<SimSpeedButton>();
                SerializedFieldAssigner.SetFloat(speedButton, "_speed", speed);
                SerializedFieldAssigner.SetReference(speedButton, "_button", button);
            }

            var view = frame.Root.AddComponent<SimSpeedPanelView>();
            SerializedFieldAssigner.SetReference(view, "_current", current);

            var presenter = frame.Root.AddComponent<SimSpeedPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<SimSpeedPanelPresenter>(frame.Root, ProjectAssetPaths.SimSpeedPanelPrefab);
        }
    }
}
