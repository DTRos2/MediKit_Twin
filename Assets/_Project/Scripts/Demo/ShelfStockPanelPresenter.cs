using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 재고 패널의 동작. 칸을 누르면 재고를 집거나 되돌리고(IStock), 수동 키팅 모드(IManualKitting)가 켜져 있으면 로봇이 직접 집어 박스에 담게 한다.
    /// 로봇 동작은 모르고 IManualKitting만 부른다. 행마다 어떤 키트의 재료인지는 레시피에서 읽는다(ShelfRowKits).
    /// 값은 매 프레임 읽어 화면에 넘기고, 화면(ShelfStockPanelView)이 같은 값은 다시 쓰지 않는다. 화면이 어떻게 생겼는지는 모른다.
    /// </summary>
    public sealed class ShelfStockPanelPresenter : MonoBehaviour
    {
        [Tooltip("자재 재고(IStock을 구현한 컴포넌트, 보통 ShelfStockPresenter). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _stockSource;
        [SerializeField] private ShelfStockPanelView _view;

        [Tooltip("집을 때 알릴 주문 순번. 재고가 0이면 이 순번으로 StockEmpty가 나간다. 0은 어떤 주문도 아니다.")]
        [SerializeField, Min(0)] private int _orderSequence;

        [Tooltip("선반 행에 붙일 키트 종류를 찾을 레시피. 비워 두면 씬에 올라와 있는 레시피를 모두 쓴다.")]
        [SerializeField] private RecipeSO[] _recipes;

        private IStock _stock;
        private IManualKitting _manual;
        private ShelfStockActions _actions;
        private bool _isReturnMode;
        private string _lastMessage = string.Empty;

        private void Awake()
        {
            _stock = _stockSource as IStock ?? DemoComponentFinder.FindFirst<IStock>();
            if (_view == null || _stock == null)
            {
                Debug.LogError($"{name}: 화면(ShelfStockPanelView)과 자재 재고(IStock)를 구현한 컴포넌트가 모두 있어야 한다.", this);
                enabled = false;
                return;
            }

            // 수동 키팅은 없어도 된다. 없으면 이 패널은 재고만 만진다.
            _manual = DemoComponentFinder.FindFirst<IManualKitting>();
            _actions = new ShelfStockActions(_stock, _orderSequence);
        }

        private void OnEnable()
        {
            _view.SlotPressed += OnSlotPressed;
            _view.ManualModeToggled += OnManualModeToggled;
            _view.ReturnModeToggled += OnReturnModeToggled;
            _view.KitCodeChosen += OnKitCodeChosen;
            _view.NewBoxRequested += OnNewBoxRequested;
        }

        // 행과 키트 종류의 짝은 레시피가 정한다. 모든 Awake가 끝난 Start에서 읽는다.
        private void Start()
        {
            RecipeSO[] recipes = _recipes != null && _recipes.Length > 0 ? _recipes : Resources.FindObjectsOfTypeAll<RecipeSO>();
            _view.ShowRowKits(ShelfRowKits.FromRecipes(recipes));
        }

        private void Update()
        {
            Refresh();
        }

        private void OnDisable()
        {
            _view.SlotPressed -= OnSlotPressed;
            _view.ManualModeToggled -= OnManualModeToggled;
            _view.ReturnModeToggled -= OnReturnModeToggled;
            _view.KitCodeChosen -= OnKitCodeChosen;
            _view.NewBoxRequested -= OnNewBoxRequested;
        }

        private bool IsManualMode => _manual != null && _manual.IsManualMode;

        private void Refresh()
        {
            _view.ShowTotal(TotalQuantity());
            _view.ShowMode(
                _manual != null, IsManualMode, _isReturnMode, _manual != null && _manual.IsBusy,
                _manual != null ? _manual.SelectedKitCode : string.Empty);

            // 수동 모드에서는 로봇이 움직일 수 있는 때(자동 키팅이 멈췄고 로봇이 쉬는 중)만 칸을 누를 수 있다.
            _view.ShowTiles(_stock, !IsManualMode || _manual.CanRequest);
            _view.ShowMessage(MessageText());
        }

        private int TotalQuantity()
        {
            int total = 0;
            if (_stock.ShelfSlotIds == null)
            {
                return total;
            }

            foreach (string slotId in _stock.ShelfSlotIds)
            {
                total += _stock.GetQuantity(slotId);
            }

            return total;
        }

        private string MessageText()
        {
            string message = IsManualMode ? _manual.Message : _lastMessage;
            return string.IsNullOrEmpty(message) ? ShelfStockPanelTexts.Guide : message;
        }

        private void OnSlotPressed(string slotId)
        {
            if (IsManualMode)
            {
                _manual.TryRequestPick(slotId);
                return;
            }

            _lastMessage = _isReturnMode ? _actions.Return(slotId) : _actions.Take(slotId);
        }

        private void OnManualModeToggled(bool wanted)
        {
            _manual?.SetManualMode(wanted);
        }

        private void OnReturnModeToggled(bool wanted)
        {
            _isReturnMode = wanted;
        }

        private void OnKitCodeChosen(string kitCode)
        {
            if (_manual != null)
            {
                _manual.SelectedKitCode = kitCode;
            }
        }

        private void OnNewBoxRequested()
        {
            _manual?.StartNewBox();
        }
    }
}
