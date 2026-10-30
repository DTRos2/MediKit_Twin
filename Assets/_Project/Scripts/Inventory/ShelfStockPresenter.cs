using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 재고(ShelfStock)를 만들어 시뮬레이션 시간으로 돌리고, 수량 변화를 칸 화면(ShelfCellView)에 전한다.
    /// 로봇암 A 등은 IStock과 IShelfItemLocator로만 선반을 다루고 화면은 모른다. 두 인터페이스를 이 컴포넌트가 구현하지만
    /// 실제 일은 ShelfStock과 ShelfItemLocator에 맡기고 넘겨주기만 하므로, Inspector에서 이 컴포넌트 하나만 연결하면 된다.
    /// </summary>
    public sealed class ShelfStockPresenter : MonoBehaviour, IStock, IShelfItemLocator
    {
        [Tooltip("시작 재고, 보충 대기 시간, 보충 수량을 담은 시나리오 설정. 비워 두면 기본값을 쓴다.")]
        [SerializeField] private ScenarioConfigSO _config;

        private ShelfStock _stock;
        private ShelfItemLocator _itemLocator;
        private readonly Dictionary<string, ShelfCellView> _viewsBySlotId = new Dictionary<string, ShelfCellView>();
        private ScenarioConfigSO _fallbackConfig;

        /// <summary>다른 설비가 재고를 다루는 접점. Awake 이후에 쓸 수 있다.</summary>
        public IStock Stock => _stock;

        public IReadOnlyList<string> ShelfSlotIds => _stock != null ? _stock.ShelfSlotIds : System.Array.Empty<string>();

        public bool TryTake(string shelfSlotId, int orderSequence) => _stock.TryTake(shelfSlotId, orderSequence);

        public void Return(string shelfSlotId) => _stock.Return(shelfSlotId);

        public int GetQuantity(string shelfSlotId) => _stock.GetQuantity(shelfSlotId);

        public float? GetRefillRemainingSeconds(string shelfSlotId) => _stock.GetRefillRemainingSeconds(shelfSlotId);

        public bool TryGetPickItem(string shelfSlotId, out ShelfPickItem item) => _itemLocator.TryGetPickItem(shelfSlotId, out item);

        private void Awake()
        {
            ScenarioConfigSO config = _config;
            if (config == null)
            {
                Debug.LogWarning($"{name}: 시나리오 설정이 없어 기본값으로 재고를 만든다.", this);
                _fallbackConfig = ScriptableObject.CreateInstance<ScenarioConfigSO>();
                config = _fallbackConfig;
            }

            var initialStocks = new List<ShelfStockInit>();
            foreach (ShelfCellView view in GetComponentsInChildren<ShelfCellView>(true))
            {
                _viewsBySlotId[view.ShelfSlotId] = view;
                initialStocks.Add(new ShelfStockInit(view.ShelfSlotId, config.GetInitialStock(view.ShelfSlotId)));
            }

            _stock = new ShelfStock(
                initialStocks,
                config.RefillDelaySeconds,
                config.RefillQuantity,
                () => SimClock.Now,
                new CellEventsStockSink());
            _stock.QuantityChanged += OnQuantityChanged;

            _itemLocator = new ShelfItemLocator(shelfSlotId => _viewsBySlotId.TryGetValue(shelfSlotId, out ShelfCellView view) ? view : null);
        }

        private void Start()
        {
            // 칸 화면은 처음에 용량만큼 차 있으므로 시작 재고에 맞춰 줄인다. 다른 컴포넌트의 Awake가 끝난 뒤에 해야 한다.
            foreach (string shelfSlotId in _stock.ShelfSlotIds)
            {
                OnQuantityChanged(shelfSlotId, _stock.GetQuantity(shelfSlotId));
            }
        }

        private void Update()
        {
            _stock.Tick();
        }

        private void OnDestroy()
        {
            if (_stock != null)
            {
                _stock.QuantityChanged -= OnQuantityChanged;
            }

            if (_fallbackConfig != null)
            {
                Destroy(_fallbackConfig);
            }
        }

        private void OnQuantityChanged(string shelfSlotId, int quantity)
        {
            if (_viewsBySlotId.TryGetValue(shelfSlotId, out ShelfCellView view))
            {
                view.SetQuantity(quantity);
            }
        }
    }
}
