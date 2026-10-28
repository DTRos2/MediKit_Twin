using System;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 설비 사이의 신호를 주고받는 이벤트 버스. 설비끼리 직접 참조하지 않고 이 클래스만 거친다.
    /// 이 클래스는 ROS를 알지 못한다. ROS 코드가 이 이벤트를 구독하는 방향만 허용한다.
    /// </summary>
    public static class CellEvents
    {
        /// <summary>로봇암 A가 키트 구성을 마치고 박스를 컨베이어에 넘겼다. (FR-03)</summary>
        public static event Action<KitData> KitReady;

        /// <summary>박스가 검사 구역에 도착해 정지했다. (FR-05)</summary>
        public static event Action<KitData> AtInspection;

        /// <summary>판정이 끝났다. 결과는 KitData.Verdict에 들어 있고, 발행하기 전에 채워진다. (FR-06, FR-07)</summary>
        public static event Action<KitData> InspectionResult;

        /// <summary>
        /// 불량 박스를 푸셔가 밀어내 불량 박스에 넣었고 검사 구역이 비었다. 컨베이어는 이 신호로 다음 박스를 보낸다.
        /// 푸셔가 CompleteTime을 채운 뒤에 발행한다. (FR-09)
        /// </summary>
        public static event Action<KitData> KitRejected;

        /// <summary>불량 박스가 정원에 도달해 비워졌다. 발행하는 쪽은 불량 박스 관리(푸셔)이다. (FR-09)</summary>
        public static event Action RejectBinCleared;

        /// <summary>양품 박스가 팩킹 구역에 도착했다. (FR-05)</summary>
        public static event Action<KitData> AtPacking;

        /// <summary>뚜껑이 박스에 닫혔다. (FR-10)</summary>
        public static event Action<KitData> LidClosed;

        /// <summary>박스가 컨베이어 끝에 도착했다. (FR-05)</summary>
        public static event Action<KitData> AtConveyorEnd;

        /// <summary>하역 로봇이 박스를 하역장에 내려놓았다. 하역 로봇이 CompleteTime을 채운 뒤에 발행한다. (FR-12)</summary>
        public static event Action<KitData> KitShipped;

        /// <summary>선반 칸 재고가 0이다. 인자는 (선반 칸 ID, 보류할 주문 순번). (FR-13)</summary>
        public static event Action<string, int> StockEmpty;

        /// <summary>선반 칸이 보충되었다. 인자는 선반 칸 ID. (FR-13)</summary>
        public static event Action<string> StockRefilled;

        /// <summary>재고 부족으로 주문이 보류되었다. 인자는 주문 순번. (FR-13)</summary>
        public static event Action<int> OrderHeld;

        /// <summary>보류되었던 주문이 대기열 맨 앞으로 돌아와 다시 시작된다. 인자는 주문 순번. (FR-13)</summary>
        public static event Action<int> OrderResumed;

        /// <summary>
        /// 설비 상태가 바뀌었다. 바뀔 때만 발행하므로 구독자(로거, 주문 관리)는 값을 비교하지 않고 그대로 처리한다.
        /// Fault이면 주문 관리가 해당 주문을 보류한다. (FR-14, FR-16)
        /// </summary>
        public static event Action<StationId, StationState> StationStateChanged;

        /// <summary>
        /// 설비가 살아 있다는 주기 통지다. 상태가 같아도 StationHeartbeat.IntervalSeconds마다 현재 상태를 보낸다.
        /// 대시보드만 구독하며, 이 신호가 TimeoutSeconds 넘게 끊기면 "응답 없음"으로 표시한다. (FR-14)
        /// </summary>
        public static event Action<StationId, StationState> StationAlive;

        /// <summary>
        /// 경고가 발생했다. 인자는 (경고 코드, 메시지). 대시보드가 경고 영역에 표시하고 로거가 기록한다.
        /// 예: 같은 주문 3번 보류(FR-13), 구역 센서 10초 미감지(FR-05), 적재 칸 가득 참(FR-12).
        /// </summary>
        public static event Action<string, string> WarningRaised;

        public static void RaiseKitReady(KitData kit) => KitReady?.Invoke(kit);

        public static void RaiseAtInspection(KitData kit) => AtInspection?.Invoke(kit);

        public static void RaiseInspectionResult(KitData kit)
        {
            // 구독자가 kit.Verdict를 읽으므로, 비어 있으면 신호를 보내지 않고 발행 순서 오류를 알린다.
            if (kit == null || kit.Verdict == null)
            {
                Debug.LogError("InspectionResult는 KitData.Verdict를 채운 뒤에 발행해야 한다.");
                return;
            }

            InspectionResult?.Invoke(kit);
        }

        public static void RaiseRejectBinCleared() => RejectBinCleared?.Invoke();

        public static void RaiseAtPacking(KitData kit) => AtPacking?.Invoke(kit);

        public static void RaiseLidClosed(KitData kit) => LidClosed?.Invoke(kit);

        public static void RaiseAtConveyorEnd(KitData kit) => AtConveyorEnd?.Invoke(kit);

        public static void RaiseKitShipped(KitData kit) => KitShipped?.Invoke(kit);

        public static void RaiseStockEmpty(string shelfSlotId, int orderSequence) => StockEmpty?.Invoke(shelfSlotId, orderSequence);

        public static void RaiseStockRefilled(string shelfSlotId) => StockRefilled?.Invoke(shelfSlotId);

        public static void RaiseOrderHeld(int orderSequence) => OrderHeld?.Invoke(orderSequence);

        public static void RaiseOrderResumed(int orderSequence) => OrderResumed?.Invoke(orderSequence);

        public static void RaiseKitRejected(KitData kit) => KitRejected?.Invoke(kit);

        public static void RaiseStationStateChanged(StationId station, StationState state) => StationStateChanged?.Invoke(station, state);

        public static void RaiseStationAlive(StationId station, StationState state) => StationAlive?.Invoke(station, state);

        public static void RaiseWarning(string code, string message) => WarningRaised?.Invoke(code, message);

        // Domain Reload를 끈 에디터에서 이전 Play의 구독이 남지 않도록 시작할 때 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearSubscribers()
        {
            KitReady = null;
            AtInspection = null;
            InspectionResult = null;
            RejectBinCleared = null;
            AtPacking = null;
            LidClosed = null;
            AtConveyorEnd = null;
            KitShipped = null;
            StockEmpty = null;
            StockRefilled = null;
            OrderHeld = null;
            OrderResumed = null;
            KitRejected = null;
            StationStateChanged = null;
            StationAlive = null;
            WarningRaised = null;
        }
    }
}
