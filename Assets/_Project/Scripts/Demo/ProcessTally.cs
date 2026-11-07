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
    /// 화면에 보이는 "지금 키트"는 가장 늦게 투입된 키트다(KitDispatchOrder). 앞선 키트의 늦은 신호는 숫자만 올리고 화면은 되돌리지 않는다.
    /// 센 값은 주문을 다시 적용해도 지우지 않고 이어서 센다. 주문을 다시 불러오면 키트 순번이 1부터 다시 시작하므로,
    /// 키트는 순번이 아니라 객체(KitData 인스턴스)로 구분한다. 새 주문이 적용되면(OnOrdersApplied) 지금 키트와 단계만 처음 상태로 돌린다.
    /// </summary>
    public sealed class ProcessTally : IProcessStatus
    {
        // KitData는 Equals를 따로 정하지 않아 객체 자체로 구분된다. 같은 순번이 주문마다 다시 나와도 서로 다른 키트로 센다.
        private readonly HashSet<KitData> _completed = new HashSet<KitData>();
        private readonly HashSet<KitData> _rejected = new HashSet<KitData>();
        private readonly Dictionary<StationId, StationState> _stationStates = new Dictionary<StationId, StationState>();

        public int CompletedCount => _completed.Count;

        public int RejectedCount => _rejected.Count;

        /// <summary>현황이 바뀔 때마다 오르는 번호. 화면은 이 번호가 달라진 프레임에만 다시 그리면 된다.</summary>
        public int Version { get; private set; }

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

            _rejected.Add(kit);
            Touch();
            Follow(kit, ProcessStage.Rejecting);
        }

        public void OnKitRejected(KitData kit)
        {
            if (kit == null)
            {
                return;
            }

            _rejected.Add(kit);
            Touch();
            Follow(kit, ProcessStage.Rejected);
        }

        public void OnAtPacking(KitData kit) => Follow(kit, ProcessStage.Packing);

        public void OnLidClosed(KitData kit) => Follow(kit, ProcessStage.ToEnd);

        public void OnAtConveyorEnd(KitData kit) => Finish(kit);

        public void OnKitShipped(KitData kit) => Finish(kit);

        /// <summary>
        /// 새 주문이 적용됐다. 지금 키트와 단계만 처음 상태로 돌려 화면이 "공정 완료" 같은 지난 주문의 끝에 머물지 않게 한다.
        /// 센 값(완료, 불량, 재고 부족, 오류)과 설비 상태, 마지막 경고는 그대로 둔다. 새 주문의 키트는 이어서 센다.
        /// </summary>
        public void OnOrdersApplied()
        {
            if (CurrentKit == null && CurrentStage == ProcessStage.None)
            {
                return;
            }

            CurrentKit = null;
            CurrentStage = ProcessStage.None;
            Touch();
        }

        public void OnStockEmpty()
        {
            StockShortageCount++;
            Touch();
        }

        public void OnStationStateChanged(StationId station, StationState state)
        {
            bool wasFault = StateOf(station) == StationState.Fault;
            _stationStates[station] = state;
            if (state == StationState.Fault && !wasFault)
            {
                FaultCount++;
            }

            Touch();
        }

        public void OnWarning(string code, string message)
        {
            LastWarning = string.IsNullOrEmpty(code) ? message ?? string.Empty : $"{code}: {message}";
            Touch();
        }

        private void Finish(KitData kit)
        {
            if (kit == null)
            {
                return;
            }

            _completed.Add(kit);
            Touch();
            Follow(kit, ProcessStage.Done);
        }

        private void Touch()
        {
            Version++;
        }

        // 가장 뒤에 투입된 키트(같은 때 포함)의 신호만 화면 상태를 바꾼다. 앞선 키트의 늦은 신호가 화면을 되돌리지 않게 한다.
        private void Follow(KitData kit, ProcessStage stage)
        {
            if (kit == null)
            {
                return;
            }

            if (CurrentKit != null && KitDispatchOrder.IsEarlier(kit, CurrentKit))
            {
                return;
            }

            CurrentKit = kit;
            CurrentStage = stage;
            Touch();
        }
    }
}
