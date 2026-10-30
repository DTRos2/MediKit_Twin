using System;
using System.Collections.Generic;
using System.Linq;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 키트 코드마다 주문 1건을 만든다. (FR-01 예외 처리: 3종 각 1건)
    /// 코드 목록을 레시피에서 받으므로 키트 종류가 늘면 기본 주문도 따라 늘어난다.
    /// </summary>
    public sealed class PerKitCodeDefaultOrderProvider : IDefaultOrderProvider
    {
        private readonly IReadOnlyCollection<string> _kitCodes;
        private readonly string _hospitalName;
        private readonly int _quantity;

        public PerKitCodeDefaultOrderProvider(IReadOnlyCollection<string> kitCodes, string hospitalName, int quantity)
        {
            _kitCodes = kitCodes ?? throw new ArgumentNullException(nameof(kitCodes));
            _hospitalName = hospitalName;
            _quantity = quantity;
        }

        public IReadOnlyList<OrderRecord> CreateDefaultOrders()
        {
            // 순서가 실행마다 같아야 NFR-06(같은 시드, 같은 결과)이 지켜지므로 정렬한다.
            return _kitCodes
                .OrderBy(code => code, StringComparer.Ordinal)
                .Select((code, index) => new OrderRecord(index + 1, _hospitalName, code, _quantity))
                .ToList();
        }
    }
}
