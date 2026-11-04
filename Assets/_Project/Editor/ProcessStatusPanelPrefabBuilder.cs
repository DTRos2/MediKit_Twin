using System;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 공정 현황 패널 프리팹(PF_ProcessStatusPanel)을 만든다. 지금 키트의 단계 글, 설비별 상태 칩, 숫자 칸 네 개, 마지막 경고가
    /// 모두 계층의 오브젝트로 들어가고, 화면 코드(ProcessStatusPanelView)와 동작 코드(ProcessStatusPanelPresenter)가 그 오브젝트들을 연결한다.
    /// 설비 칩은 StationId 값마다 하나씩 만들어지므로, 설비가 늘면 이 빌더를 다시 돌리기만 하면 된다.
    /// 위치는 IMGUI 시절과 같은 자리 표(DemoPanelSlots.Status)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class ProcessStatusPanelPrefabBuilder
    {
        // 설비 칩은 한 줄에 이만큼씩. 패널 폭(380px)에서 "로봇암 A 가동" 같은 칩이 두 개 들어간다.
        private const int StationChipsPerRow = 2;

        private const string RootName = "PF_ProcessStatusPanel";

        public static ProcessStatusPanelPresenter Build()
        {
            DashboardPalette palette = DashboardPalette.Default;

            UiPanelFrame.Frame frame = UiPanelFrame.Build(
                RootName, DemoPanelSlots.Status, ProcessStatusPanelTexts.Title, ProcessStatusPanelTexts.Subtitle);
            UiWidgets.ScrollView body = UiPanelFrame.AddScrollBody(frame, DashboardMetrics.BlockGap);

            // 지금 키트: 작은 제목, 큰 단계 글, 키트 설명.
            RectTransform currentKit = UiPrimitives.CreateRect("CurrentKit", body.Content);
            UiPrimitives.AddColumn(currentKit, 0f);
            UiPrimitives.AddLabel(currentKit, "Caption", ProcessStatusPanelTexts.CurrentKit, TextRole.Small, palette.Muted);
            Text headline = UiPrimitives.AddLabel(currentKit, "Headline", string.Empty, TextRole.State, palette.Muted);
            Text kitSummary = UiPrimitives.AddLabel(
                currentKit, "KitSummary", string.Empty, TextRole.Small, palette.Muted, TextAnchor.UpperLeft, isWrapped: true);

            AddStationChips(body.Content);

            // 숫자 칸 네 개: 완료, 불량품, 재고부족, 오류.
            RectTransform counters = UiPrimitives.CreateRect("Counters", body.Content);
            UiPrimitives.AddRow(counters, DashboardMetrics.BlockGap);
            UiPrimitives.SetLayout(counters, preferredHeight: DashboardMetrics.StatTileHeight);
            UiStatTile completed = UiWidgets.AddStatTile(counters, "Completed", ProcessStatusPanelTexts.Completed, palette.Success);
            UiStatTile rejected = UiWidgets.AddStatTile(counters, "Rejected", ProcessStatusPanelTexts.Rejected, palette.Warning);
            UiStatTile stockShortage = UiWidgets.AddStatTile(counters, "StockShortage", ProcessStatusPanelTexts.StockShortage, palette.Accent);
            UiStatTile fault = UiWidgets.AddStatTile(counters, "Fault", ProcessStatusPanelTexts.Fault, palette.Danger);

            // 마지막 경고는 있을 때만 켜진다. 줄 폭보다 길 수 있어서 줄바꿈한다.
            Text warning = UiPrimitives.AddLabel(
                body.Content, "Warning", string.Empty, TextRole.Small, palette.Warning, TextAnchor.UpperLeft, isWrapped: true);
            warning.gameObject.SetActive(false);

            var view = frame.Root.AddComponent<ProcessStatusPanelView>();
            SerializedFieldAssigner.SetReference(view, "_headline", headline);
            SerializedFieldAssigner.SetReference(view, "_kitSummary", kitSummary);
            SerializedFieldAssigner.SetReference(view, "_completed", completed);
            SerializedFieldAssigner.SetReference(view, "_rejected", rejected);
            SerializedFieldAssigner.SetReference(view, "_stockShortage", stockShortage);
            SerializedFieldAssigner.SetReference(view, "_fault", fault);
            SerializedFieldAssigner.SetReference(view, "_warning", warning);

            var presenter = frame.Root.AddComponent<ProcessStatusPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<ProcessStatusPanelPresenter>(frame.Root, ProjectAssetPaths.ProcessStatusPanelPrefab);
        }

        // 설비마다 칩 하나. 한 줄에 StationChipsPerRow개씩 놓는다.
        private static void AddStationChips(Transform parent)
        {
            RectTransform section = UiWidgets.AddSection(parent, "Stations", ProcessStatusPanelTexts.Stations, DashboardMetrics.RowGap);

            var stations = (StationId[])Enum.GetValues(typeof(StationId));
            for (int start = 0; start < stations.Length; start += StationChipsPerRow)
            {
                RectTransform row = UiPrimitives.CreateRect($"StationRow_{start / StationChipsPerRow + 1:00}", section);
                UiPrimitives.AddRow(row, DashboardMetrics.RowGap);
                UiPrimitives.SetLayout(row, preferredHeight: DashboardMetrics.ChipHeight);

                for (int i = start; i < start + StationChipsPerRow && i < stations.Length; i++)
                {
                    UiChip chip = UiWidgets.AddChip(row, $"Chip_{stations[i]}", string.Empty, ChipTone.Neutral, hasDot: true);
                    var stationChip = chip.gameObject.AddComponent<ProcessStationChip>();
                    SerializedFieldAssigner.SetEnum(stationChip, "_station", stations[i]);
                    SerializedFieldAssigner.SetReference(stationChip, "_chip", chip);
                }
            }
        }
    }
}
