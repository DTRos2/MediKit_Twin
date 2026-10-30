using System;
using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 1건을 수량만큼의 키트 요청으로 펼친다. 파일 순서와 주문 안의 순서가 그대로 유지되어
    /// 키트의 투입 순서가 결정적이다. (FR-01: 주문 1건 = 키트 수량만큼)
    /// </summary>
    public sealed class KitRequestPlanner
    {
        private readonly ILotNumberGenerator _lotNumbers;

        public KitRequestPlanner(ILotNumberGenerator lotNumbers)
        {
            _lotNumbers = lotNumbers ?? throw new ArgumentNullException(nameof(lotNumbers));
        }

        public IReadOnlyList<KitRequest> Plan(IEnumerable<OrderRecord> orders)
        {
            var requests = new List<KitRequest>();
            foreach (OrderRecord order in orders)
            {
                for (int i = 0; i < order.Quantity; i++)
                {
                    requests.Add(new KitRequest(order.Sequence, order.HospitalName, order.KitCode, _lotNumbers.Next()));
                }
            }

            return requests;
        }
    }
}
