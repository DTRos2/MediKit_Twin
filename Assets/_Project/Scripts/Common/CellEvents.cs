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

        /// <summary>불량 박스가 정원에 도달해 비워졌다. (FR-09)</summary>
        public static event Action RejectBinCleared;

        /// <summary>양품 박스가 팩킹 구역에 도착했다. (FR-05)</summary>
        public static event Action<KitData> AtPacking;

        /// <summary>뚜껑이 박스에 닫혔다. (FR-10)</summary>
        public static event Action<KitData> LidClosed;

        /// <summary>박스가 컨베이어 끝에 도착했다. (FR-05)</summary>
        public static event Action<KitData> AtConveyorEnd;

        /// <summary>하역 로봇이 박스를 하역장에 내려놓았다. (FR-12)</summary>
        public static event Action<KitData> KitShipped;

        /// <summary>선반 칸 재고가 0이다. 인자는 (선반 칸 ID, 보류할 주문 순번). (FR-13)</summary>
        public static event Action<string, int> StockEmpty;

        /// <summary>선반 칸이 보충되었다. 인자는 선반 칸 ID. (FR-13)</summary>
        public static event Action<string> StockRefilled;

        /// <summary>설비 상태가 바뀌었다. 대시보드의 설비 상태 표시에 쓴다. (FR-14)</summary>
        public static event Action<StationId, StationState> StationStateChanged;

        public static void RaiseKitReady(KitData kit) => KitReady?.Invoke(kit);

        public static void RaiseAtInspection(KitData kit) => AtInspection?.Invoke(kit);

        public static void RaiseInspectionResult(KitData kit) => InspectionResult?.Invoke(kit);

        public static void RaiseRejectBinCleared() => RejectBinCleared?.Invoke();

        public static void RaiseAtPacking(KitData kit) => AtPacking?.Invoke(kit);

        public static void RaiseLidClosed(KitData kit) => LidClosed?.Invoke(kit);

        public static void RaiseAtConveyorEnd(KitData kit) => AtConveyorEnd?.Invoke(kit);

        public static void RaiseKitShipped(KitData kit) => KitShipped?.Invoke(kit);

        public static void RaiseStockEmpty(string shelfSlotId, int orderSequence) => StockEmpty?.Invoke(shelfSlotId, orderSequence);

        public static void RaiseStockRefilled(string shelfSlotId) => StockRefilled?.Invoke(shelfSlotId);

        public static void RaiseStationStateChanged(StationId station, StationState state) => StationStateChanged?.Invoke(station, state);

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
            StationStateChanged = null;
        }
    }
}
