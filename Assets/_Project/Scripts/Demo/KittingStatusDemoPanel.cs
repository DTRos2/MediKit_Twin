using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 A가 키트를 만드는 모습을 글로 보여 주는 패널(IMGUI). 설비 상태, 만드는 중인 키트(종류, 순번, 주문, 병원, 로트),
    /// 구성품별 진행(대기, 집는 중, 옮기는 중, 완료, 누락), 마지막 결과와 횟수를 보여 준다.
    /// 값은 키팅 설비(IKittingStation)가 알려 주는 현황(IKittingStatus)을 읽기만 하고, 키팅 순서와 로봇 동작과 구체 클래스는 모른다.
    /// 화면 오른쪽에 붙어, 왼쪽의 주문과 선반 패널과 나뉜다.
    /// </summary>
    public sealed class KittingStatusDemoPanel : DemoPanel
    {
        [Tooltip("키팅 설비(IKittingStation을 구현한 컴포넌트, 보통 KittingController). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _station;

        private IKittingStation _kittingStation;

        public KittingStatusDemoPanel()
            : base("Kitting (Arm A)", DemoPanelAnchor.TopRight, new Vector2(10f, 10f), new Vector2(360f, 380f))
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

            GUILayout.Label($"설비 상태: {StateText(_kittingStation.State)}");

            if (status.CurrentPlan != null)
            {
                DrawCurrentKit(status);
            }
            else
            {
                GUILayout.Label("지금 만드는 키트 없음");
            }

            GUILayout.Space(6f);
            GUILayout.Label(LastResultText(status));
            GUILayout.Label($"완료 {status.CompletedKitCount}개 · 재고 부족 {status.StockShortageCount}회 · 오류 {status.FaultCount}회");
        }

        private static void DrawCurrentKit(IKittingStatus status)
        {
            KittingPlan plan = status.CurrentPlan;
            KitData kit = plan.Kit;

            GUILayout.Label($"{kit.KitCode} 키트 #{kit.KitSequence} · 주문 #{kit.OrderSequence} · {kit.HospitalName}");
            GUILayout.Label($"로트 {kit.LotNumber} · 박스 {plan.Recipe.BoxSize}");

            int placed = 0;
            int toPlace = 0;
            for (int i = 0; i < plan.Items.Count; i++)
            {
                if (plan.Items[i].IsSkipped)
                {
                    continue;
                }

                toPlace++;
                if (status.ItemStates[i] == KittingItemState.Placed)
                {
                    placed++;
                }
            }

            GUILayout.Label($"담은 구성품 {placed}/{toPlace}");
            if (plan.MissingSlot.HasValue)
            {
                GUILayout.Label($"누락 예정 슬롯: {plan.MissingSlot.Value} (평가용 정답, 판정에는 쓰지 않는다)");
            }

            for (int i = 0; i < plan.Items.Count; i++)
            {
                KittingItem item = plan.Items[i];
                GUILayout.Label($"  슬롯 {item.SlotNumber}  {item.ComponentId} ({item.ShelfSlotId})  {ItemStateText(status.ItemStates[i])}");
            }
        }

        // 대시보드 표시 규칙(INTERFACES.md 3장): 대기, 가동, 정지.
        private static string StateText(StationState state)
        {
            switch (state)
            {
                case StationState.Running: return "가동";
                case StationState.Fault: return "정지";
                default: return "대기";
            }
        }

        private static string ItemStateText(KittingItemState state)
        {
            switch (state)
            {
                case KittingItemState.Picking: return "집는 중";
                case KittingItemState.Placing: return "옮기는 중";
                case KittingItemState.Placed: return "완료";
                case KittingItemState.Skipped: return "누락(집지 않음)";
                default: return "대기";
            }
        }

        private static string LastResultText(IKittingStatus status)
        {
            switch (status.LastResult)
            {
                case KittingResult.Completed:
                    return $"마지막 결과: {status.LastLotNumber} 완료";
                case KittingResult.StockShortage:
                    return $"마지막 결과: {status.LastLotNumber} 재고 부족으로 중단 — {status.LastMessage}";
                case KittingResult.Fault:
                    return $"마지막 결과: {status.LastLotNumber} 오류로 중단 — {status.LastMessage}";
                default:
                    return "마지막 결과: 아직 없음";
            }
        }
    }
}
