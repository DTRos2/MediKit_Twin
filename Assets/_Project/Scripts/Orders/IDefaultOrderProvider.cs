using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>주문 파일이 없거나 유효한 주문이 하나도 없을 때 쓸 기본 주문을 만든다.</summary>
    public interface IDefaultOrderProvider
    {
        IReadOnlyList<OrderRecord> CreateDefaultOrders();
    }
}
