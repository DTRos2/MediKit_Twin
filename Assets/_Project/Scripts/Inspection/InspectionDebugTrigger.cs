using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 컨베이어(FR-05)가 AtInspection을 보내기 전까지 검사 구역을 단독으로 확인하는 임시 장치다.
    /// 플레이 모드에서 컴포넌트를 우클릭해 'Raise AtInspection (Test)'를 누른다. 컨베이어를 구현하면 이 컴포넌트는 지운다.
    /// 가장 최근 KitReady의 키트를 쓰고, 아직 없으면 아래 키트 코드로 임시 키트를 만든다.
    /// </summary>
    public sealed class InspectionDebugTrigger : MonoBehaviour
    {
        [Tooltip("키팅 결과가 없을 때 만들 임시 키트의 종류(EM, BL, DX).")]
        [SerializeField] private string _fallbackKitCode = "EM";

        private KitData _lastKit;

        private void OnEnable()
        {
            CellEvents.KitReady += OnKitReady;
            CellEvents.InspectionResult += OnInspectionResult;
        }

        private void OnDisable()
        {
            CellEvents.KitReady -= OnKitReady;
            CellEvents.InspectionResult -= OnInspectionResult;
        }

        private void OnKitReady(KitData kit)
        {
            _lastKit = kit;
        }

        private void OnInspectionResult(KitData kit)
        {
            InspectionVerdict v = kit.Verdict.Value;
            string slots = v.MissingSlots == null ? "알 수 없음" : string.Join(",", v.MissingSlots);
            string detected = kit.DetectedComponentIds == null ? "없음" : string.Join(",", System.Array.ConvertAll(kit.DetectedComponentIds, id => id ?? "-"));
            Debug.Log($"[InspectionDebug] 로트 {kit.LotNumber} {(v.IsPass ? "정상" : "불량")} 불일치 슬롯: {slots} / 센서: {detected} / 방식: {v.Method}", this);
        }

        [ContextMenu("Raise AtInspection (Test)")]
        private void RaiseAtInspectionForTest()
        {
            KitData kit = _lastKit ?? new KitData(1, 1, "TEST-0001", "테스트병원", _fallbackKitCode);
            kit.Verdict = null;
            CellEvents.RaiseAtInspection(kit);
        }
    }
}
