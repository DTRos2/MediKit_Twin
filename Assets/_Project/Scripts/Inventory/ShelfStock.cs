using System;
using System.Collections.Generic;
using MediKitTwin.Common;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 선반 칸별 재고와 보충. (FR-13, IStock) 재고가 0인 칸을 집으려 하면 StockEmpty를 알리고, 보충 대기 시간이 지나면
    /// 그 칸의 수량을 보충 수량으로 채운다(더하지 않는다). 시간은 호출하는 쪽이 주는 시각 함수와 Tick으로만 알므로
    /// 시뮬레이션 시간(배속, 일시정지)을 따르고, Unity 없이 시험할 수 있다.
    /// </summary>
    public sealed class ShelfStock : IStock
    {
        private readonly List<string> _shelfSlotIds = new List<string>();
        private readonly Dictionary<string, int> _quantities = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _refillAt = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly float _refillDelaySeconds;
        private readonly int _refillQuantity;
        private readonly Func<float> _now;
        private readonly IStockEventSink _events;

        public ShelfStock(
            IEnumerable<ShelfStockInit> initialStocks,
            float refillDelaySeconds,
            int refillQuantity,
            Func<float> now,
            IStockEventSink events)
        {
            if (initialStocks == null)
            {
                throw new ArgumentNullException(nameof(initialStocks));
            }

            if (refillDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(refillDelaySeconds), "보충 대기 시간은 0 이상이어야 한다.");
            }

            if (refillQuantity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(refillQuantity), "보충 수량은 1 이상이어야 한다.");
            }

            _refillDelaySeconds = refillDelaySeconds;
            _refillQuantity = refillQuantity;
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _events = events ?? throw new ArgumentNullException(nameof(events));

            foreach (ShelfStockInit init in initialStocks)
            {
                if (_quantities.ContainsKey(init.ShelfSlotId))
                {
                    throw new ArgumentException($"선반 칸 '{init.ShelfSlotId}'가 둘 이상이다.", nameof(initialStocks));
                }

                _shelfSlotIds.Add(init.ShelfSlotId);
                _quantities.Add(init.ShelfSlotId, Math.Max(0, init.Quantity));
            }
        }

        /// <summary>칸의 수량이 바뀌었다(칸 ID, 새 수량). 선반 화면이 구독해 물건을 없애거나 채운다.</summary>
        public event Action<string, int> QuantityChanged;

        public IReadOnlyList<string> ShelfSlotIds => _shelfSlotIds;

        public bool TryTake(string shelfSlotId, int orderSequence)
        {
            if (shelfSlotId == null || !_quantities.TryGetValue(shelfSlotId, out int quantity))
            {
                return false;
            }

            if (quantity > 0)
            {
                SetQuantity(shelfSlotId, quantity - 1);
                return true;
            }

            // 보충 타이머는 칸마다 한 번만 건다. 기다리는 동안 다른 주문이 집으려 해도 신호는 매번 나가 그 주문도 보류된다.
            if (!_refillAt.ContainsKey(shelfSlotId))
            {
                _refillAt.Add(shelfSlotId, _now() + _refillDelaySeconds);
            }

            _events.StockEmpty(shelfSlotId, orderSequence);
            return false;
        }

        public void Return(string shelfSlotId)
        {
            // 보충을 기다리는 칸은 어차피 보충 수량으로 채워지므로 되돌린 1개는 무시한다.
            if (shelfSlotId == null
                || !_quantities.TryGetValue(shelfSlotId, out int quantity)
                || _refillAt.ContainsKey(shelfSlotId))
            {
                return;
            }

            SetQuantity(shelfSlotId, quantity + 1);
        }

        public int GetQuantity(string shelfSlotId)
        {
            return shelfSlotId != null && _quantities.TryGetValue(shelfSlotId, out int quantity) ? quantity : 0;
        }

        public float? GetRefillRemainingSeconds(string shelfSlotId)
        {
            if (shelfSlotId == null || !_refillAt.TryGetValue(shelfSlotId, out float refillAt))
            {
                return null;
            }

            return Math.Max(0f, refillAt - _now());
        }

        /// <summary>보충 시각이 된 칸을 채운다. 매 프레임 호출해도 된다.</summary>
        public void Tick()
        {
            if (_refillAt.Count == 0)
            {
                return;
            }

            float now = _now();
            List<string> due = null;
            foreach (KeyValuePair<string, float> pair in _refillAt)
            {
                if (now >= pair.Value)
                {
                    (due ?? (due = new List<string>())).Add(pair.Key);
                }
            }

            if (due == null)
            {
                return;
            }

            foreach (string shelfSlotId in due)
            {
                _refillAt.Remove(shelfSlotId);
                SetQuantity(shelfSlotId, _refillQuantity);
                _events.StockRefilled(shelfSlotId);
            }
        }

        private void SetQuantity(string shelfSlotId, int quantity)
        {
            _quantities[shelfSlotId] = quantity;
            QuantityChanged?.Invoke(shelfSlotId, quantity);
        }
    }
}
