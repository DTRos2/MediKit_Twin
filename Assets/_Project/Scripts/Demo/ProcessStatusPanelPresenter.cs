using MediKitTwin.Orders;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 공정 현황 패널의 동작. 켜질 때 이벤트 버스(CellEvents)의 공정 신호를 받기 시작하고(CellEventsProcessFeed), 꺼질 때 끊는다.
    /// 센 값(ProcessTally)이 바뀐 프레임에만 화면(ProcessStatusPanelView)을 고친다(Version이 달라졌는지 본다).
    /// 켜지기 전에 지나간 신호는 세지 않는다. 화면이 어떻게 생겼는지는 모른다.
    /// 주문을 다시 적용하면(IOrderInput.LastIntake가 새 객체로 바뀌면) 지금 키트와 단계만 처음 상태로 돌리고, 센 값은 이어서 센다.
    /// </summary>
    public sealed class ProcessStatusPanelPresenter : MonoBehaviour
    {
        [SerializeField] private ProcessStatusPanelView _view;

        [Tooltip("주문 입력(IOrderInput)을 씬에서 찾아 두는 간격(초). 새 주문이 적용된 것을 알아채는 데 쓴다.")]
        [SerializeField, Min(0.1f)] private float _findOrderInputIntervalSeconds = 1f;

        private ProcessTally _tally;
        private CellEventsProcessFeed _feed;
        private LazyComponentFinder<IOrderInput> _orderInput;
        private OrderAppliedDetector _orderApplied;
        private bool _hasShown;
        private int _shownVersion;

        /// <summary>지금까지 센 현황. 시험이 읽는다.</summary>
        public IProcessStatus Status => _tally;

        private void Awake()
        {
            _tally = new ProcessTally();
            _feed = new CellEventsProcessFeed(_tally);
            _orderInput = new LazyComponentFinder<IOrderInput>(null, _findOrderInputIntervalSeconds, DemoComponentFinder.FindFirst<IOrderInput>);
            _orderApplied = new OrderAppliedDetector();
        }

        private void OnEnable()
        {
            _feed.Attach();
            _hasShown = false;
            Refresh();
        }

        private void OnDisable()
        {
            _feed.Detach();
        }

        private void Update()
        {
            WatchOrders();
            Refresh();
        }

        // 새 주문이 적용됐으면 지난 주문의 끝("공정 완료")에 머물던 화면을 처음 상태로 돌린다.
        private void WatchOrders()
        {
            if (_orderInput.Refresh(Time.unscaledTime) && _orderApplied.HasChanged(_orderInput.Found.LastIntake))
            {
                _tally.OnOrdersApplied();
            }
        }

        private void Refresh()
        {
            if (_hasShown && _tally.Version == _shownVersion)
            {
                return;
            }

            _view.Show(_tally);
            _hasShown = true;
            _shownVersion = _tally.Version;
        }
    }
}
