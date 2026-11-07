using System.Collections;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇암 A의 키팅 반복: 한가하면 IKitSource에서 다음 키트를 받아 KittingProcess로 만든다. (FR-03)
    /// 결과에 따른 알림과 설비 상태 변경은 KittingOutcomeHandler, 만드는 방법은 KittingProcess, 로봇 동작은 IItemTransfer,
    /// 누락은 IMissingRule이 맡고 이 클래스는 협력자를 조립하고 반복 순서만 다룬다.
    /// 협력자는 모두 인터페이스로 받으므로 Inspector에서 같은 인터페이스를 구현한 컴포넌트를 연결한다.
    /// </summary>
    public sealed class KittingController : MonoBehaviour, IKittingStation, IStationPause, IStationFreeze
    {
        [Header("협력자 (인터페이스를 구현한 컴포넌트)")]
        [Tooltip("다음에 만들 키트를 주는 쪽(IKitSource, 보통 OrderSessionBehaviour).")]
        [SerializeField] private MonoBehaviour _kitSource;
        [Tooltip("자재 재고(IStock, 보통 ShelfStockPresenter).")]
        [SerializeField] private MonoBehaviour _stock;
        [Tooltip("선반 물건 위치(IShelfItemLocator, 보통 ShelfStockPresenter).")]
        [SerializeField] private MonoBehaviour _itemLocator;
        [Tooltip("박스 공급과 슬롯 위치(IBoxSupply와 IKitBoxLocator를 모두 구현, 보통 KitBoxSupply).")]
        [SerializeField] private MonoBehaviour _boxSupply;
        [Tooltip("구성품 이송(IItemTransfer, 보통 ArmItemTransfer).")]
        [SerializeField] private MonoBehaviour _itemTransfer;

        [Header("설정")]
        [Tooltip("누락 확률, 고정 누락 키트 순번과 슬롯, 난수 시드를 담은 시나리오 설정.")]
        [SerializeField] private ScenarioConfigSO _scenario;
        [Tooltip("만들 키트가 없을 때 다시 물어보는 간격(초). 시뮬레이션 시간이다.")]
        [SerializeField, Min(0.1f)] private float _idlePollSeconds = 0.5f;
        [Tooltip("박스가 지그에 놓이기를 기다리는 최대 시간(초).")]
        [SerializeField, Min(1f)] private float _boxWaitTimeoutSeconds = 10f;
        [Tooltip("오류(Fault)로 멈춘 뒤 다시 시작하기까지 쉬는 시간(초).")]
        [SerializeField, Min(0f)] private float _faultRecoverySeconds = 5f;

        private readonly KittingStatusTracker _status = new KittingStatusTracker();
        private IKitSource _source;
        private KittingPlanner _planner;
        private KittingProcess _process;
        private KittingOutcomeHandler _outcomeHandler;
        private IStationStateReporter _station;
        private IManualHoldState _holdState;
        private bool _isPauseRequested;
        private bool _isHoldAnnounced;

        /// <summary>
        /// 키트 사이에서 실제로 쉬고 있으면 true. 멈춤을 요청받았거나(수동 키팅), 사람이 팔을 직접 조작하는 동안(수동 제어 패널)이다.
        /// </summary>
        public bool IsPaused { get; private set; }

        public void RequestPause()
        {
            _isPauseRequested = true;
        }

        public void Resume()
        {
            _isPauseRequested = false;
        }

        public bool IsFrozen { get; private set; }

        /// <summary>하던 키트의 동작도 다음 단계에서 바로 멈춘다. 팔은 마지막으로 받은 목표를 지키며 그 자리에 선다.</summary>
        public void Freeze()
        {
            IsFrozen = true;
        }

        public void Unfreeze()
        {
            IsFrozen = false;
        }

        /// <summary>지금 만들고 있는 키트. 쉬는 중이면 null이다.</summary>
        public KitData CurrentKit { get; private set; }

        private bool IsArmHeld => _holdState != null && _holdState.IsHeld;

        public IKittingStatus Status => _status;

        public StationState State => _station != null ? _station.State : StationState.Idle;

        private void Awake()
        {
            _source = _kitSource as IKitSource;
            var stock = _stock as IStock;
            var itemLocator = _itemLocator as IShelfItemLocator;
            var boxSupply = _boxSupply as IBoxSupply;
            var boxLocator = _boxSupply as IKitBoxLocator;
            var transfer = _itemTransfer as IItemTransfer;
            _holdState = _itemTransfer as IManualHoldState;

            if (_source == null || stock == null || itemLocator == null || boxSupply == null || boxLocator == null
                || transfer == null || _scenario == null)
            {
                Debug.LogError($"{name}: Kit Source, Stock, Item Locator, Box Supply, Item Transfer, Scenario를 모두 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _station = new StationStateReporter(StationId.ArmA);
            _planner = new KittingPlanner(new RandomMissingRule(MissingRuleSettings.From(_scenario)));
            _process = new KittingProcess(stock, boxSupply, boxLocator, itemLocator, transfer, () => SimClock.Now, _boxWaitTimeoutSeconds, _status);
            _outcomeHandler = new KittingOutcomeHandler(new CellEventsKittingSink(), _station, _faultRecoverySeconds);
        }

        private void Start()
        {
            StartCoroutine(Loop());
        }

        private void Update()
        {
            // 응답 없음 판정은 실제 시간 기준이다. 일시정지나 배속 중에도 끊기지 않도록 unscaledTime을 쓴다.
            _station.Tick(Time.unscaledTime);
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                // 멈춤 요청은 키트 사이에서만 받는다. 하던 키트는 끝까지 만든다.
                // 사람이 팔을 직접 조작하는 중이면 팔 목표가 막혀 하던 키트가 곧바로 정리되어 끝나므로, 그 뒤에 여기서 쉰다.
                // 이때는 한 번 알린다. 아무 말 없이 멈추면 주문을 적용해도 아무 일도 안 일어나는 것으로 보인다.
                bool isHeld = IsArmHeld;
                if (isHeld && !_isHoldAnnounced)
                {
                    Debug.LogWarning($"{name}: 수동 제어가 켜져 있어 키팅을 쉰다. Arm Joints 패널의 수동 제어를 끄면 이어서 시작한다.", this);
                }

                _isHoldAnnounced = isHeld;

                if (_isPauseRequested || isHeld)
                {
                    IsPaused = true;
                    yield return null;
                    continue;
                }

                IsPaused = false;

                if (_source.TryGetNext(out KitData kit, out RecipeSO recipe))
                {
                    yield return Gate(MakeKit(kit, recipe));
                }
                else
                {
                    // 모든 주문이 보류되었거나 끝났다. 대기 자세로 기다리며 다시 묻는다. (FR-13 예외 처리)
                    _station.Set(StationState.Idle);
                    yield return new WaitForSeconds(_idlePollSeconds);
                }
            }
        }

        // 안쪽 코루틴을 한 단계씩 돌리며, 얼려 있는 동안은 다음 단계로 넘어가지 않는다. 안쪽에서 또 코루틴을 yield하면 그것도 같은 방식으로 감싼다.
        private IEnumerator Gate(IEnumerator inner)
        {
            while (true)
            {
                while (IsFrozen)
                {
                    yield return null;
                }

                if (!inner.MoveNext())
                {
                    yield break;
                }

                if (inner.Current is IEnumerator nested)
                {
                    yield return Gate(nested);
                }
                else
                {
                    yield return inner.Current;
                }
            }
        }

        private IEnumerator MakeKit(KitData kit, RecipeSO recipe)
        {
            CurrentKit = kit;
            _station.Set(StationState.Running);
            kit.StartTime = SimClock.Now;

            KittingPlan plan = _planner.Plan(kit, recipe);
            var outcome = new KittingOutcome();
            yield return _process.Run(plan, outcome);

            CurrentKit = null;
            if (outcome.Result != KittingResult.Completed)
            {
                // 이벤트 버스로만 알리면 Console에 남지 않아 원인을 찾기 어렵다.
                Debug.LogWarning($"{name}: 키팅이 {outcome.Result}로 끝났다. {outcome.Message}", this);
            }

            float waitSeconds = _outcomeHandler.Handle(kit, outcome);
            if (waitSeconds > 0f)
            {
                yield return new WaitForSeconds(waitSeconds);
            }

            _station.Set(StationState.Idle);
        }
    }
}
