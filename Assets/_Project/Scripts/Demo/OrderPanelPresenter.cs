using MediKitTwin.Common;
using MediKitTwin.Orders;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 주문 패널의 동작. 화면(OrderPanelView)에서 누른 것을 주문 입력(IOrderInput)에 전하고, 주문 입력과 현황(IOrderStatus)이
    /// 바뀌면 화면에 다시 보여 준다. 주문 입력과 현황 인터페이스만 알고 구체 클래스도, 위젯이 어떻게 생겼는지도 모른다.
    /// 주문은 StreamingAssets 아래 CSV 파일이 기본이고(SRS FR-01), 이 패널은 그 위에 덮어쓰는 시험용 편의이다.
    /// </summary>
    public sealed class OrderPanelPresenter : MonoBehaviour
    {
        [Tooltip("주문 입력(IOrderInput)과 현황(IOrderStatus)을 구현한 컴포넌트(보통 OrderSessionBehaviour). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _orders;
        [SerializeField] private OrderPanelView _view;

        [Tooltip("\"샘플 채우기\"를 누르면 입력창에 들어가는 주문. 열은 순번, 병원명, 키트 코드, 수량이다.")]
        [SerializeField, TextArea(3, 8)] private string _sampleCsv = "1,A병원,EM,2\n2,B병원,DX,3\n3,C병원,BL,2";

        private IOrderInput _input;
        private IOrderStatus _status;

        // 마지막으로 화면에 보여 준 값. 값이 바뀐 프레임에만 화면을 고치려고 기억해 둔다(프레임마다 글자를 새로 만들지 않는다).
        private OrderIntakeResult _shownIntake;
        private bool _isIntakeShown;
        private int _shownHeldCount = -1;
        private int _shownPendingCount = -1;

        private void Awake()
        {
            _input = _orders as IOrderInput ?? DemoComponentFinder.FindFirst<IOrderInput>();
            _status = _orders as IOrderStatus ?? _input as IOrderStatus ?? DemoComponentFinder.FindFirst<IOrderStatus>();

            if (_view == null || _input == null || _status == null)
            {
                Debug.LogError($"{name}: 화면(OrderPanelView)과 주문 입력(IOrderInput), 현황(IOrderStatus)을 구현한 컴포넌트가 모두 있어야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _view.ApplyRequested += OnApplyRequested;
            _view.SampleRequested += FillSample;
            _view.FileRequested += OnFileRequested;
        }

        private void Start()
        {
            FillSample();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void OnDisable()
        {
            _view.ApplyRequested -= OnApplyRequested;
            _view.SampleRequested -= FillSample;
            _view.FileRequested -= OnFileRequested;
        }

        private void OnApplyRequested(string csv)
        {
            _input.LoadText(csv);
        }

        private void OnFileRequested()
        {
            _input.LoadFromFile();
        }

        private void FillSample()
        {
            _view.CsvText = _sampleCsv;
        }

        private void Refresh()
        {
            OrderIntakeResult intake = _input.LastIntake;
            if (!_isIntakeShown || !ReferenceEquals(intake, _shownIntake))
            {
                _view.ShowIntake(intake);
                _shownIntake = intake;
                _isIntakeShown = true;
            }

            int heldCount = _status.HeldOrderCount;
            int pendingCount = _status.PendingKitCount;
            if (heldCount != _shownHeldCount || pendingCount != _shownPendingCount)
            {
                _view.ShowStatus(heldCount, pendingCount);
                _shownHeldCount = heldCount;
                _shownPendingCount = pendingCount;
            }
        }
    }
}
