using System;
using MediKitTwin.Common;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// CellEvents 신호를 OrderManager에 이어 준다. 구독과 해제를 한 곳에서 짝지어 처리하므로,
    /// Dispose를 부르면(예: 씬을 떠날 때) 구독이 남아 이전 실행의 주문 관리가 신호를 받는 일이 없다.
    /// </summary>
    public sealed class OrderManagerCellEventsBridge : IDisposable
    {
        private readonly OrderManager _manager;
        private bool _isDisposed;

        public OrderManagerCellEventsBridge(OrderManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            CellEvents.KitReady += _manager.OnKitReady;
            CellEvents.StockEmpty += _manager.OnStockEmpty;
            CellEvents.StockRefilled += _manager.OnStockRefilled;
            CellEvents.StationStateChanged += _manager.OnStationStateChanged;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            CellEvents.KitReady -= _manager.OnKitReady;
            CellEvents.StockEmpty -= _manager.OnStockEmpty;
            CellEvents.StockRefilled -= _manager.OnStockRefilled;
            CellEvents.StationStateChanged -= _manager.OnStationStateChanged;
        }
    }
}
