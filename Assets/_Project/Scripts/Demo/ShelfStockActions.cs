using System;
using MediKitTwin.Common;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 패널에서 칸을 눌렀을 때 재고에 하는 일(집기, 되돌리기)과 그 결과 문장. 재고는 IStock으로만 다루므로 실제 설비와 같은 경로를 쓴다.
    /// 화면이나 로봇은 모른다. Unity 없이 시험할 수 있다.
    /// </summary>
    public sealed class ShelfStockActions
    {
        private readonly IStock _stock;
        private readonly int _orderSequence;

        /// <param name="orderSequence">집을 때 알릴 주문 순번. 재고가 0이면 이 순번으로 StockEmpty가 나간다. 0은 어떤 주문도 아니다.</param>
        public ShelfStockActions(IStock stock, int orderSequence)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _orderSequence = orderSequence;
        }

        /// <summary>칸에서 하나를 집는다. 재고가 없으면 집지 못하고 보충 대기가 시작된다.</summary>
        public string Take(string shelfSlotId)
        {
            return _stock.TryTake(shelfSlotId, _orderSequence)
                ? ShelfStockPanelTexts.Taken(shelfSlotId, _stock.GetQuantity(shelfSlotId))
                : ShelfStockPanelTexts.OutOfStock(shelfSlotId);
        }

        /// <summary>박스에서 선반으로 하나를 되돌린다.</summary>
        public string Return(string shelfSlotId)
        {
            _stock.Return(shelfSlotId);
            return ShelfStockPanelTexts.Returned(shelfSlotId, _stock.GetQuantity(shelfSlotId));
        }
    }
}
