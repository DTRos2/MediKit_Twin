using System;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키팅부터 컨베이어 끝까지 공정 전체를 보여 주는 패널(IMGUI). 지금 키트가 어느 단계인지(큰 글자), 설비별 상태 칩,
    /// 완료·불량품·재고부족·오류 횟수, 마지막 경고를 보여 준다. 설비 하나(로봇암 A)의 일은 보지 않는다.
    /// 값은 이벤트 버스(CellEvents)의 신호를 ProcessTally가 세어 둔 현황(IProcessStatus)을 읽기만 하고,
    /// 어떤 단계를 어떤 말과 색으로 보일지는 ProcessStatusText와 ProcessChipTones가 정한다.
    /// 패널이 켜질 때 신호를 받기 시작하고 꺼질 때 끊는다. 켜지기 전에 지나간 신호는 세지 않는다.
    /// </summary>
    public sealed class ProcessStatusDemoPanel : DemoPanel
    {
        // 설비 칩은 한 줄에 이만큼씩. 패널 폭(380px)에서 "로봇암 A 가동" 같은 칩이 두 개 들어간다.
        private const int StationChipsPerRow = 2;
        private const string CompletedLabel = "완료";
        private const string RejectedLabel = "불량품";
        private const string StockShortageLabel = "재고부족";
        private const string FaultLabel = "오류";

        private ProcessTally _tally;
        private CellEventsProcessFeed _feed;

        public ProcessStatusDemoPanel()
            : base("공정 현황", "PROCESS · 전체", DemoPanelSlots.Status)
        {
        }

        private void Awake()
        {
            _tally = new ProcessTally();
            _feed = new CellEventsProcessFeed(_tally);
        }

        private void OnEnable()
        {
            _feed.Attach();
        }

        private void OnDisable()
        {
            _feed.Detach();
        }

        protected override void DrawContents()
        {
            DashboardPalette palette = DashboardPalette.Default;

            DrawCurrentKit(_tally, palette);
            DashboardSpacing.Block();
            DrawStations(_tally);
            DashboardSpacing.Block();
            DrawCounters(_tally, palette);
            DrawLastWarning(_tally, palette);
        }

        private static void DrawCurrentKit(IProcessStatus status, DashboardPalette palette)
        {
            ChipTone tone = status.CurrentKit == null ? ChipTone.Neutral : ProcessChipTones.Stage(status.CurrentStage);
            ChipColors.Resolve(tone, palette, out _, out Color stageColor);

            DashboardText.Line("지금 키트", TextRole.Small, palette.Muted);
            DashboardText.Line(ProcessStatusText.Headline(status), TextRole.State, stageColor);
            DashboardText.Paragraph(ProcessStatusText.KitSummary(status.CurrentKit), TextRole.Small, palette.Muted);
        }

        private static void DrawStations(IProcessStatus status)
        {
            SectionHeading.Draw("설비 상태");
            DashboardSpacing.Row();

            StationId[] stations = (StationId[])Enum.GetValues(typeof(StationId));
            for (int start = 0; start < stations.Length; start += StationChipsPerRow)
            {
                using (DashboardRow.Begin())
                {
                    for (int i = start; i < start + StationChipsPerRow && i < stations.Length; i++)
                    {
                        StationState state = status.StateOf(stations[i]);
                        Chip.Draw(ProcessStatusText.StationChip(stations[i], state), KittingChipTones.Station(state), withDot: true);
                        GUILayout.Space(DashboardMetrics.RowGap);
                    }
                }

                DashboardSpacing.Row();
            }
        }

        private static void DrawCounters(IProcessStatus status, DashboardPalette palette)
        {
            using (DashboardRow.Begin())
            {
                StatTile.Draw(CompletedLabel, status.CompletedCount.ToString(), palette.Success);
                DashboardSpacing.Block();
                StatTile.Draw(RejectedLabel, status.RejectedCount.ToString(), palette.Warning);
                DashboardSpacing.Block();
                StatTile.Draw(StockShortageLabel, status.StockShortageCount.ToString(), palette.Accent);
                DashboardSpacing.Block();
                StatTile.Draw(FaultLabel, status.FaultCount.ToString(), palette.Danger);
            }
        }

        // 경고가 있을 때만 줄을 더한다. 줄 폭보다 길 수 있어서 줄바꿈한다.
        private static void DrawLastWarning(IProcessStatus status, DashboardPalette palette)
        {
            if (status.LastWarning.Length == 0)
            {
                return;
            }

            DashboardSpacing.Row();
            DashboardText.Paragraph(status.LastWarning, TextRole.Small, palette.Warning);
        }
    }
}
