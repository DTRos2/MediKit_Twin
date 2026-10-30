using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 재고를 눈으로 시험하는 패널. 로봇암 A의 키팅과 별개로, 칸마다 버튼을 눌러 물건을 집고(앞줄부터 사라지고 뒤쪽이 앞으로 밀려 나옴)
    /// 되돌리며, 재고가 0인 칸을 집어 보충 대기(30초)와 보충을 확인한다. 재고는 IStock으로만 다루므로 실제 설비와 같은 경로를 쓴다.
    /// </summary>
    public sealed class ShelfStockDemoPanel : DemoPanel
    {
        private const float ButtonHeight = 34f;

        [Tooltip("자재 재고(IStock을 구현한 컴포넌트, 보통 ShelfStockPresenter). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _stockSource;

        [Tooltip("집을 때 알릴 주문 순번. 재고가 0이면 이 순번으로 StockEmpty가 나간다. 0은 어떤 주문도 아니다.")]
        [SerializeField, Min(0)] private int _orderSequence;

        private IStock _stock;
        private readonly List<List<string>> _rows = new List<List<string>>();
        private bool _isReturnMode;
        private string _lastMessage = string.Empty;

        public ShelfStockDemoPanel()
            : base("Shelf Stock", DemoPanelAnchor.TopLeft, new Vector2(10f, 420f), new Vector2(340f, 230f))
        {
        }

        private void Awake()
        {
            _stock = _stockSource as IStock ?? DemoComponentFinder.FindFirst<IStock>();
            if (_stock == null)
            {
                Debug.LogError($"{name}: 자재 재고(IStock)를 구현한 컴포넌트를 찾을 수 없다.", this);
                enabled = false;
            }
        }

        protected override void DrawContents()
        {
            // 칸 목록은 재고가 만들어진 뒤에야 알 수 있으므로 첫 그리기에서 정한다.
            if (_rows.Count == 0 && !TryBuildRows())
            {
                return;
            }

            _isReturnMode = GUILayout.Toggle(_isReturnMode, "되돌리기 모드 (끄면 집기)");

            foreach (List<string> row in _rows)
            {
                GUILayout.BeginHorizontal();
                foreach (string shelfSlotId in row)
                {
                    if (GUILayout.Button(ButtonLabel(shelfSlotId), GUILayout.Height(ButtonHeight)))
                    {
                        Press(shelfSlotId);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Label(_lastMessage);
        }

        private bool TryBuildRows()
        {
            if (_stock.ShelfSlotIds == null || _stock.ShelfSlotIds.Count == 0)
            {
                return false;
            }

            // 칸 ID의 첫 글자(행)가 바뀔 때마다 줄을 나눠 선반과 같은 3행 모양으로 그린다.
            List<string> row = null;
            char rowLetter = '\0';
            foreach (string shelfSlotId in _stock.ShelfSlotIds)
            {
                if (row == null || shelfSlotId[0] != rowLetter)
                {
                    row = new List<string>();
                    _rows.Add(row);
                    rowLetter = shelfSlotId[0];
                }

                row.Add(shelfSlotId);
            }

            return true;
        }

        private string ButtonLabel(string shelfSlotId)
        {
            string label = $"{shelfSlotId}\n{_stock.GetQuantity(shelfSlotId)}개";
            float? remaining = _stock.GetRefillRemainingSeconds(shelfSlotId);
            return remaining.HasValue ? $"{label} (보충 {remaining.Value:0}초)" : label;
        }

        private void Press(string shelfSlotId)
        {
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
