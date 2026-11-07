using System.Collections.Generic;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 자재 선반 칸별 재고. 로봇암 A는 이 인터페이스로만 재고를 집고 되돌린다. (FR-03, FR-13)
    /// 선반 칸 ID는 RecipeItem.ShelfSlotId와 같은 형식(A-1 ~ C-4)이다.
    /// </summary>
    public interface IStock
    {
        /// <summary>재고를 관리하는 선반 칸 ID 전체. 대시보드의 선반별 재고 표시에 쓴다.</summary>
        IReadOnlyList<string> ShelfSlotIds { get; }

        /// <summary>
        /// 구성품 1개를 꺼낸다. 재고가 0이면 false를 돌려주고, 꺼내기에 실패할 때마다
        /// 그 주문 순번으로 CellEvents.StockEmpty를 발행한다. 보충 대기 중에 다른 주문이 같은 칸을 집으려 해도
        /// 신호가 나가므로 그 주문도 보류된다. 보충 타이머는 칸마다 한 번만 건다. 알 수 없는 칸 ID도 false이다.
        /// </summary>
        bool TryTake(string shelfSlotId, int orderSequence);

        /// <summary>
        /// 반쯤 담긴 박스를 제거할 때 담겨 있던 구성품 1개를 재고로 되돌린다.
        /// 보충을 기다리는 칸에는 되돌리지 않는다(무시). 보충하면 그 칸은 어차피 보충 수량이 된다.
        /// </summary>
        void Return(string shelfSlotId);

        /// <summary>현재 재고 수량. 알 수 없는 칸 ID이면 0이다.</summary>
        int GetQuantity(string shelfSlotId);

        /// <summary>보충까지 남은 시뮬레이션 시간(초). 보충을 기다리는 중이 아니면 null이다.</summary>
        float? GetRefillRemainingSeconds(string shelfSlotId);
    }
}
