using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 A가 키트를 만드는 모습을 보여 주는 패널(IMGUI). 설비 상태(상태 고리와 큰 글자), 만드는 중인 키트,
    /// 키트 슬롯 진행 막대, 마지막 결과, 완료·재고 부족·오류 횟수, 구성품별 진행(대기, 집는 중, 옮기는 중, 완료, 누락)을 보여 준다.
    /// 값은 키팅 설비(IKittingStation)가 알려 주는 현황(IKittingStatus)을 읽기만 하고, 키팅 순서와 로봇 동작과 구체 클래스는 모른다.
    /// 어떤 상태를 어떤 말과 색으로 보일지는 KittingStatusText와 KittingChipTones가 정한다.
    /// </summary>
    public sealed class KittingStatusDemoPanel : DemoPanel
    {
        private const float RingDiameter = 96f;
        private const string SlotProgressFormat = "{0} / {1}";

        [Tooltip("키팅 설비(IKittingStation을 구현한 컴포넌트, 보통 KittingController). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _station;

        [Tooltip("만드는 키트가 없을 때도 진행 막대에 보여 줄 슬롯 칸 수. 키트를 만들기 시작하면 그 키트의 칸 수를 따른다.")]
        [SerializeField, Min(1)] private int _defaultSlotCount = 4;

        private IKittingStation _kittingStation;

        public KittingStatusDemoPanel()
            : base("키팅 로봇암 A", "KITTING · ARM A", DemoPanelSlots.Status)
        {
        }

        private void Awake()
        {
            _kittingStation = _station as IKittingStation ?? DemoComponentFinder.FindFirst<IKittingStation>();
            if (_kittingStation == null)
            {
                Debug.LogError($"{name}: 키팅 설비(IKittingStation)를 구현한 컴포넌트를 찾을 수 없다.", this);
                enabled = false;
            }
        }

        protected override void DrawContents()
        {
            IKittingStatus status = _kittingStation.Status;
            DashboardPalette palette = DashboardPalette.Default;

            DrawStateBlock(status);
            DashboardSpacing.Block();
            DrawSlotProgress(status);
            DashboardSpacing.Block();
            DrawLastResult(status, palette);
            DashboardSpacing.Block();
            DrawCounters(status, palette);
            DrawComponents(status, palette);
        }

        // 왼쪽에 상태 고리, 오른쪽에 "설비 상태", 큰 상태 글자, 지금 만드는 키트 설명.
        private void DrawStateBlock(IKittingStatus status)
        {
            DashboardPalette palette = DashboardPalette.Default;
            StationState state = _kittingStation.State;
            ChipColors.Resolve(KittingChipTones.Station(state), palette, out _, out Color stateColor);

            using (DashboardRow.Begin())
            {
                StateRing.Draw(RingDiameter, KittingStatusText.StationRingLabel(state), stateColor);
                GUILayout.Space(DashboardMetrics.SectionGap);

                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                DashboardText.Line("설비 상태", TextRole.Small, palette.Muted);
                DashboardText.Line(KittingStatusText.StationText(state), TextRole.State, stateColor);
                DashboardText.Paragraph(KittingStatusText.KitSummary(status.CurrentPlan), TextRole.Small, palette.Muted);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
        }

        private void DrawSlotProgress(IKittingStatus status)
        {
            DashboardPalette palette = DashboardPalette.Default;
            KittingPlan plan = status.CurrentPlan;
            int toPlace = _defaultSlotCount;
            int placed = 0;
            if (plan != null)
            {
                KittingProgress progress = KittingProgress.From(plan.Items, status.ItemStates);
                toPlace = progress.ToPlace;
                placed = progress.Placed;
            }

            using (DashboardRow.Begin())
            {
                DashboardText.Line("현재 키트 슬롯", TextRole.Small, palette.Muted);
                DashboardText.Line(string.Format(SlotProgressFormat, placed, toPlace), TextRole.Mono, palette.Muted, TextAnchor.MiddleRight);
            }

            DashboardSpacing.Row();
            SegmentBar.Draw(KittingSegmentTones.From(status.ItemStates), _defaultSlotCount);
        }

        private static void DrawLastResult(IKittingStatus status, DashboardPalette palette)
        {
            LabeledValueRow.Draw("마지막 결과", KittingStatusText.LastResultSummary(status), palette.Ink);

            // 실패했을 때만 이유를 따로 보여 준다. 줄 폭보다 길 수 있어서 줄바꿈한다.
            string detail = KittingStatusText.LastResultDetail(status);
            if (detail.Length > 0)
            {
                DashboardSpacing.Row();
                DashboardText.Paragraph(detail, TextRole.Small, palette.Warning);
            }
        }

        private static void DrawCounters(IKittingStatus status, DashboardPalette palette)
        {
            using (DashboardRow.Begin())
            {
                StatTile.Draw("완료", status.CompletedKitCount.ToString(), palette.Success);
                DashboardSpacing.Block();
                StatTile.Draw("재고 부족", status.StockShortageCount.ToString(), palette.Warning);
                DashboardSpacing.Block();
                StatTile.Draw("오류", status.FaultCount.ToString(), palette.Danger);
            }
        }

        private static void DrawComponents(IKittingStatus status, DashboardPalette palette)
        {
            KittingPlan plan = status.CurrentPlan;
            if (plan == null)
            {
                return;
            }

            SectionHeading.Draw(KittingStatusText.KitDetail(plan));
            DashboardSpacing.Row();
            for (int i = 0; i < plan.Items.Count && i < status.ItemStates.Count; i++)
            {
                KitComponentRow.Draw(plan.Items[i], status.ItemStates[i]);
            }

            if (plan.MissingSlot.HasValue)
            {
                DashboardText.Paragraph(
                    $"누락 예정 슬롯: {plan.MissingSlot.Value} (평가용 정답, 판정에는 쓰지 않는다)", TextRole.Small, palette.Faint);
            }
        }
    }
}
