using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 재고를 눈으로 시험하는 패널. 로봇암 A의 키팅과 별개로, 칸마다 타일을 눌러 물건을 집고(앞줄부터 사라지고 뒤쪽이 앞으로 밀려 나옴)
    /// 되돌리며, 재고가 0인 칸을 집어 보충 대기(30초)와 보충을 확인한다. 재고는 IStock으로만 다루므로 실제 설비와 같은 경로를 쓴다.
    /// 수동 키팅(IManualKitting)이 있으면 "수동 키팅 모드"를 켜서, 타일을 누른 칸으로 로봇이 직접 가서 집어 박스에 담게 할 수 있다.
    /// 로봇 동작은 모르고 IManualKitting만 부른다. 행마다 어떤 키트의 재료인지는 레시피에서 읽는다(ShelfRowKits).
    /// </summary>
    public sealed class ShelfStockDemoPanel : DemoPanel
    {
        private const string TotalQuantityUnit = "개";
        private const string GuideText = "칸을 눌러 재고를 확인하세요 · 수동 키팅 모드에서는 로봇이 집습니다";

        [Tooltip("자재 재고(IStock을 구현한 컴포넌트, 보통 ShelfStockPresenter). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _stockSource;

        [Tooltip("집을 때 알릴 주문 순번. 재고가 0이면 이 순번으로 StockEmpty가 나간다. 0은 어떤 주문도 아니다.")]
        [SerializeField, Min(0)] private int _orderSequence;

        [Tooltip("선반 행에 붙일 키트 종류를 찾을 레시피. 비워 두면 씬에 올라와 있는 레시피를 모두 쓴다.")]
        [SerializeField] private RecipeSO[] _recipes;

        private IStock _stock;
        private ManualKittingControls _manual;
        private IReadOnlyList<ShelfRow> _rows;
        private ShelfRowKits _rowKits;
        private bool _isReturnMode;
        private string _lastMessage = string.Empty;

        public ShelfStockDemoPanel()
            : base("선반 재고", "SHELF · 3×4", DemoPanelSlots.Shelf)
        {
        }

        private void Awake()
        {
            _stock = _stockSource as IStock ?? DemoComponentFinder.FindFirst<IStock>();
            if (_stock == null)
            {
                Debug.LogError($"{name}: 자재 재고(IStock)를 구현한 컴포넌트를 찾을 수 없다.", this);
                enabled = false;
                return;
            }

            // 수동 키팅은 없어도 된다. 없으면 이 패널은 재고만 만진다.
            IManualKitting manual = DemoComponentFinder.FindFirst<IManualKitting>();
            if (manual != null)
            {
                _manual = new ManualKittingControls(manual, RunAtNextLayout);
            }
        }

        protected override void DrawHeaderRight()
        {
            if (_stock.ShelfSlotIds == null)
            {
                return;
            }

            int total = 0;
            foreach (string slotId in _stock.ShelfSlotIds)
            {
                total += _stock.GetQuantity(slotId);
            }

            DashboardText.Inline($"총 {total}{TotalQuantityUnit}", TextRole.Mono, DashboardPalette.Default.Muted);
        }

        protected override void DrawContents()
        {
            // 칸 목록은 재고가 만들어진 뒤에야 알 수 있으므로 첫 그리기에서 정한다.
            if (_rows == null && !TryBuildRows())
            {
                return;
            }

            _manual?.DrawSwitch();
            bool isManual = _manual != null && _manual.IsManualMode;

            DashboardSpacing.Row();
            if (isManual)
            {
                _manual.DrawOperations();
            }
            else
            {
                _isReturnMode = SwitchRow.Draw(_isReturnMode, "되돌리기 모드", "끄면 집기 · 켜면 박스에서 선반으로 반환");
            }

            DashboardSpacing.Block();
            DrawRows(isManual);
            DashboardSpacing.Row();
            DashboardText.Paragraph(MessageText(isManual), TextRole.Small, DashboardPalette.Default.Muted);
        }

        private string MessageText(bool isManual)
        {
            string message = isManual ? _manual.Message : _lastMessage;
            return string.IsNullOrEmpty(message) ? GuideText : message;
        }

        private void DrawRows(bool isManual)
        {
            // 수동 모드에서는 로봇이 움직일 수 있는 때(자동 키팅이 멈췄고 로봇이 쉬는 중)만 칸을 누를 수 있다.
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && (!isManual || _manual.CanRequest);

            string pressed = null;
            foreach (ShelfRow row in _rows)
            {
                string pressedInRow = ShelfRowView.Draw(row, _rowKits.KitCodeOf(row.Letter), _stock);
                if (pressedInRow != null)
                {
                    pressed = pressedInRow;
                }

                DashboardSpacing.Row();
            }

            GUI.enabled = wasEnabled;

            if (pressed != null)
            {
                string slotId = pressed;
                RunAtNextLayout(() => Press(slotId, isManual));
            }
        }

        private bool TryBuildRows()
        {
            if (_stock.ShelfSlotIds == null || _stock.ShelfSlotIds.Count == 0)
            {
                return false;
            }

            _rows = ShelfRows.Group(_stock.ShelfSlotIds);
            _rowKits = ShelfRowKits.FromRecipes(_recipes != null && _recipes.Length > 0 ? _recipes : Resources.FindObjectsOfTypeAll<RecipeSO>());
            return true;
        }

        private void Press(string shelfSlotId, bool isManual)
        {
            if (isManual)
            {
                _manual.RequestPick(shelfSlotId);
                return;
            }

            if (_isReturnMode)
            {
                _stock.Return(shelfSlotId);
                _lastMessage = $"{shelfSlotId}: 되돌림 → {_stock.GetQuantity(shelfSlotId)}개";
                return;
            }

            bool isTaken = _stock.TryTake(shelfSlotId, _orderSequence);
            _lastMessage = isTaken
                ? $"{shelfSlotId}: 집음 → {_stock.GetQuantity(shelfSlotId)}개"
                : $"{shelfSlotId}: 재고 없음 (StockEmpty 발행, 보충 대기 중)";
        }
    }
}
