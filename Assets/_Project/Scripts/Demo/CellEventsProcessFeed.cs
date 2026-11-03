using System;
using MediKitTwin.Common;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 이벤트 버스(CellEvents)의 신호를 ProcessTally에 넘기는 다리. 센 값은 ProcessTally가 갖고, 이 클래스는 구독과 해제만 한다.
    /// Attach를 두 번 불러도 구독이 겹치지 않고, 해제하지 않은 채 사라진 패널이 신호를 계속 받지 않게 Detach를 짝으로 부른다.
    /// </summary>
    public sealed class CellEventsProcessFeed
    {
        private readonly ProcessTally _tally;
        private bool _isAttached;

        public CellEventsProcessFeed(ProcessTally tally)
        {
            _tally = tally ?? throw new ArgumentNullException(nameof(tally));
        }

        public void Attach()
        {
            if (_isAttached)
            {
                return;
            }

            CellEvents.KitReady += _tally.OnKitReady;
            CellEvents.AtInspection += _tally.OnAtInspection;
            CellEvents.InspectionResult += _tally.OnInspectionResult;
            CellEvents.KitRejected += _tally.OnKitRejected;
            CellEvents.AtPacking += _tally.OnAtPacking;
            CellEvents.LidClosed += _tally.OnLidClosed;
            CellEvents.AtConveyorEnd += _tally.OnAtConveyorEnd;
            CellEvents.KitShipped += _tally.OnKitShipped;
            CellEvents.StockEmpty += OnStockEmpty;
            CellEvents.StationStateChanged += _tally.OnStationStateChanged;
            CellEvents.WarningRaised += _tally.OnWarning;
            _isAttached = true;
        }

        public void Detach()
        {
            if (!_isAttached)
            {
                return;
            }

            CellEvents.KitReady -= _tally.OnKitReady;
            CellEvents.AtInspection -= _tally.OnAtInspection;
            CellEvents.InspectionResult -= _tally.OnInspectionResult;
            CellEvents.KitRejected -= _tally.OnKitRejected;
            CellEvents.AtPacking -= _tally.OnAtPacking;
            CellEvents.LidClosed -= _tally.OnLidClosed;
            CellEvents.AtConveyorEnd -= _tally.OnAtConveyorEnd;
            CellEvents.KitShipped -= _tally.OnKitShipped;
            CellEvents.StockEmpty -= OnStockEmpty;
            CellEvents.StationStateChanged -= _tally.OnStationStateChanged;
            CellEvents.WarningRaised -= _tally.OnWarning;
            _isAttached = false;
        }

        // StockEmpty는 (칸 ID, 주문 순번)을 싣지만 현황은 횟수만 센다.
        private void OnStockEmpty(string shelfSlotId, int orderSequence)
        {
            _tally.OnStockEmpty();
        }
    }
}
