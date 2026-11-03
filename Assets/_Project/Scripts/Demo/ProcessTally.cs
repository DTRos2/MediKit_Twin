using System.Collections.Generic;
using MediKitTwin.Common;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 공정 신호(CellEvents)를 받아 공정 전체의 현황(IProcessStatus)을 센다. Unity도 이벤트 버스도 모르는 순수한 계산이라 그대로 시험한다.
    /// 신호를 어디서 받아 넘길지는 CellEventsProcessFeed가 맡는다.
    ///  - 완료: 컨베이어 끝에 닿았거나 출하한 키트. 두 신호가 다 와도 키트당 한 번만 센다.
    ///  - 불량품: 판정이 불합격인 키트. 푸셔가 밀어낸 신호(KitRejected)만 오는 경우에도 한 번 센다.
    ///  - 재고 부족: 선반 칸 재고가 바닥났다는 신호의 횟수.
    ///  - 오류: 설비가 정지(Fault)로 바뀐 횟수. 이미 정지인 설비가 다시 정지를 알려도 늘지 않는다.
    /// 화면에 보이는 "지금 키트"는 투입 순번이 가장 큰 키트다. 앞선 키트의 늦은 신호는 숫자만 올리고 화면은 되돌리지 않는다.
    /// </summary>
    public sealed class ProcessTally : IProcessStatus
    {
        private readonly HashSet<int> _completed = new HashSet<int>();
        private readonly HashSet<int> _rejected = new HashSet<int>();
        private readonly Dictionary<StationId, StationState> _stationStates = new Dictionary<StationId, StationState>();

        public int CompletedCount => _completed.Count;

        public int RejectedCount => _rejected.Count;

        public int StockShortageCount { get; private set; }

        public int FaultCount { get; private set; }

        public KitData CurrentKit { get; private set; }

        public ProcessStage CurrentStage { get; private set; }

        public string LastWarning { get; private set; } = string.Empty;

        public StationState StateOf(StationId station)
        {
            return _stationStates.TryGetValue(station, out StationState state) ? state : StationState.Idle;
        }

        public void OnKitReady(KitData kit) => Follow(kit, ProcessStage.ToInspection);

        public void OnAtInspection(KitData kit) => Follow(kit, ProcessStage.Inspecting);

        public void OnInspectionResult(KitData kit)
        {
            if (kit?.Verdict == null)
            {
                return;
            }

            if (kit.Verdict.Value.IsPass)
            {
                Follow(kit, ProcessStage.ToPacking);
                return;
            }

            _rejected.Add(kit.KitSequence);
            Follow(kit, ProcessStage.Rejecting);
        }

        public void OnKitRejected(KitData kit)
        {
            if (kit == null)
            {
                return;
            }

            _rejected.Add(kit.KitSequence);
            Follow(kit, ProcessStage.Rejected);
        }

        public void OnAtPacking(KitData kit) => Follow(kit, ProcessStage.Packing);

        public void OnLidClosed(KitData kit) => Follow(kit, ProcessStage.ToEnd);

        public void OnAtConveyorEnd(KitData kit) => Finish(kit);

        public void OnKitShipped(KitData kit) => Finish(kit);

        public void OnStockEmpty()
        {
            StockShortageCount++;
        }

        public void OnStationStateChanged(StationId station, StationState state)
        {
            bool wasFault = StateOf(station) == StationState.Fault;
            _stationStates[station] = state;
            if (state == StationState.Fault && !wasFault)
            {
                FaultCount++;
            }
        }

        public void OnWarning(string code, string message)
        {
            LastWarning = string.IsNullOrEmpty(code) ? message ?? string.Empty : $"{code}: {message}";
        }

        private void Finish(KitData kit)
        {
            if (kit == null)
            {
                return;
            }

            _completed.Add(kit.KitSequence);
            Follow(kit, ProcessStage.Done);
        }

        // 가장 뒤에 투입된 키트(같은 순번 포함)의 신호만 화면 상태를 바꾼다. 앞선 키트의 늦은 신호가 화면을 되돌리지 않게 한다.
        private void Follow(KitData kit, ProcessStage stage)
        {
            if (kit == null)
            {
                return;
            }

            if (CurrentKit != null && kit.KitSequence < CurrentKit.KitSequence)
            {
                return;
            }

            CurrentKit = kit;
            CurrentStage = stage;
        }
    }
}
