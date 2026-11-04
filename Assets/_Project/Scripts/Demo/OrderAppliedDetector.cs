using MediKitTwin.Orders;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 주문이 새로 적용됐는지 알아챈다. 주문을 불러올 때마다 접수 결과(OrderIntakeResult)가 새 객체로 바뀌므로, 지난번에 본 것과 다른 객체가 오면
    /// "새로 적용됐다"고 본다. 이벤트 버스에 신호를 따로 두지 않고 주문 입력(IOrderInput.LastIntake)만 읽는다. 씬 없이 시험한다.
    /// 처음 본 값은 기준으로만 삼는다: 패널이 켜지기 전에 적용된 주문은 "새로" 적용된 것이 아니다.
    /// </summary>
    public sealed class OrderAppliedDetector
    {
        private OrderIntakeResult _last;
        private bool _hasBaseline;

        /// <summary>지난번에 본 것과 다른 접수 결과가 오면 한 번 true. 같은 값을 다시 주면 false다.</summary>
        public bool HasChanged(OrderIntakeResult current)
        {
            if (!_hasBaseline)
            {
                _last = current;
                _hasBaseline = true;
                return false;
            }

            if (ReferenceEquals(current, _last))
            {
                return false;
            }

            _last = current;
            return true;
        }
    }
}
