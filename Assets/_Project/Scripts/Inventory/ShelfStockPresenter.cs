using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 재고(ShelfStock)를 만들어 시뮬레이션 시간으로 돌리고, 수량 변화를 칸 화면(ShelfCellView)에 전한다.
    /// 로봇암 A 등은 Stock(IStock)으로만 재고를 다루고 화면은 모른다. 화면과 재고를 이어 주는 일만 한다.
    /// </summary>
    public sealed class ShelfStockPresenter : MonoBehaviour
    {
        [Tooltip("시작 재고, 보충 대기 시간, 보충 수량을 담은 시나리오 설정. 비워 두면 기본값을 쓴다.")]
        [SerializeField] private ScenarioConfigSO _config;

        private ShelfStock _stock;
        private readonly Dictionary<string, ShelfCellView> _viewsBySlotId = new Dictionary<string, ShelfCellView>();
        private ScenarioConfigSO _fallbackConfig;

        /// <summary>다른 설비가 재고를 다루는 접점. Awake 이후에 쓸 수 있다.</summary>
        public IStock Stock => _stock;

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
