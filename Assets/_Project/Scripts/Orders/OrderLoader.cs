using System;
using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 파일을 읽어 유효한 주문 목록을 만든다. 읽기(IOrderTextSource), 해석(IOrderParser),
    /// 기본값(IDefaultOrderProvider)을 조립만 하므로 각각 바꿔 끼울 수 있다. (FR-01)
    /// </summary>
    public sealed class OrderLoader
    {
        private readonly IOrderTextSource _source;
        private readonly IOrderParser _parser;
        private readonly IDefaultOrderProvider _defaultOrders;

        public OrderLoader(IOrderTextSource source, IOrderParser parser, IDefaultOrderProvider defaultOrders)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _defaultOrders = defaultOrders ?? throw new ArgumentNullException(nameof(defaultOrders));
        }

        public OrderLoadResult Load()
        {
            if (!_source.TryReadAll(out string text))
            {
                var missing = new OrderIssue(OrderIssueCodes.FileMissing,
                    $"주문 파일을 읽을 수 없어 기본 주문 세트를 쓴다: {_source.Description}");
                return new OrderLoadResult(_defaultOrders.CreateDefaultOrders(), new[] { missing });
            }

            OrderLoadResult parsed = _parser.Parse(text);
            if (parsed.Orders.Count > 0)
            {
                return parsed;
            }

            // 모든 행이 걸러지면 셀이 아무것도 만들지 않으므로 파일이 없을 때와 같이 기본 주문으로 돌아간다.
            var issues = new List<OrderIssue>(parsed.Issues)
            {
                new OrderIssue(OrderIssueCodes.NoValidOrder,
                    $"유효한 주문이 없어 기본 주문 세트를 쓴다: {_source.Description}")
            };
            return new OrderLoadResult(_defaultOrders.CreateDefaultOrders(), issues);
        }
    }
}
