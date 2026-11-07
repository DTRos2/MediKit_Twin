using System;
using System.Collections.Generic;
using System.IO;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// OrderSession을 씬에서 쓰게 하는 컴포넌트. Inspector 값으로 설정을 만들고, 주문 파일을 시작할 때 읽거나
    /// 실행 중 문자열로 받아 주문 관리를 바꾼다. 로봇암 A는 이 컴포넌트를 IKitSource로, 대시보드는 IOrderStatus로 연결한다.
    /// 주문을 읽고 관리하는 일은 OrderSession과 OrderManager가 하고, 이 클래스는 설정 조립과 씬 수명 관리만 맡는다.
    /// </summary>
    public sealed class OrderSessionBehaviour : MonoBehaviour, IKitSource, IOrderStatus, IOrderInput
    {
        [Header("레시피")]
        [Tooltip("생산할 수 있는 키트 종류의 레시피. 이 목록의 키트 코드만 주문으로 받아들인다.")]
        [SerializeField] private List<RecipeSO> _recipes = new List<RecipeSO>();

        [Header("주문 파일 (FR-01)")]
        [Tooltip("주문 파일이 있는 StreamingAssets 아래 폴더 이름. 빌드에서도 읽으려면 StreamingAssets에 둔다.")]
        [SerializeField] private string _ordersFolderName = "Orders";
        [Tooltip("패널의 \"파일 읽기\"가 읽는 파일 이름(위 폴더 아래).")]
        [SerializeField] private string _ordersFileName = "orders.csv";
        [Tooltip("켜면 Play 시작과 동시에 위 파일을 읽어 바로 키팅을 시작한다. 끄면 패널에서 \"적용\"이나 \"파일 읽기\"를 누를 때까지 아무것도 하지 않는다.")]
        [SerializeField] private bool _loadOnStart;
        [SerializeField, Min(1)] private int _minQuantity = 1;
        [SerializeField, Min(1)] private int _maxQuantity = 10;
        [Tooltip("파일이 없거나 유효한 주문이 없을 때 쓰는 기본 주문의 병원명.")]
        [SerializeField] private string _defaultHospitalName = "DEFAULT";
        [SerializeField, Min(1)] private int _defaultQuantity = 1;

        [Header("CSV 모양")]
        [Tooltip("열을 나누는 문자 한 글자.")]
        [SerializeField] private string _delimiter = ",";
        [Tooltip("쉼표가 든 값을 감싸는 문자 한 글자.")]
        [SerializeField] private string _quote = "\"";
        [Tooltip("이 문자로 시작하는 줄은 주석으로 보고 건너뛴다. 한 글자.")]
        [SerializeField] private string _commentMarker = "#";

        [Header("로트번호")]
        [SerializeField] private string _lotPrefix = "LOT-";
        [SerializeField, Min(1)] private int _lotDigits = 4;

        [Header("보류 (FR-13, FR-16)")]
        [Tooltip("같은 주문이 이 횟수만큼 보류되면 경고한다.")]
        [SerializeField, Min(1)] private int _repeatedHoldWarningCount = 3;
        [Tooltip("이 설비가 Fault이면 만들던 주문을 보류한다.")]
        [SerializeField] private List<StationId> _faultHoldStations = new List<StationId> { StationId.ArmA };

        private OrderSession _session;

        /// <summary>주문 세션. Awake 이후에 쓸 수 있다.</summary>
        public OrderSession Session => _session;

        public OrderIntakeResult LastIntake => _session.LastIntake;

        public int HeldOrderCount => _session.HeldOrderCount;

        public int PendingKitCount => _session.PendingKitCount;

        private void Awake()
        {
            if (_delimiter.Length != 1 || _quote.Length != 1 || _commentMarker.Length != 1)
            {
                Debug.LogError($"{name}: 구분 문자, 따옴표 문자, 주석 문자는 모두 한 글자여야 한다.", this);
                enabled = false;
                return;
            }

            try
            {
                var settings = new OrderIntakeSettings(
                    _minQuantity, _maxQuantity, _defaultHospitalName, _defaultQuantity,
                    _lotPrefix, _lotDigits, _delimiter[0], _quote[0], _commentMarker[0], CsvOrderColumns.Default);

                _session = new OrderSession(
                    new RecipeCatalog(_recipes),
                    settings,
                    new OrderManagerSettings(_repeatedHoldWarningCount, _faultHoldStations),
                    new CellEventsOrderSink(),
                    manager => new OrderManagerCellEventsBridge(manager));
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{name}: 주문 설정이 올바르지 않다. {exception.Message}", this);
                enabled = false;
            }
        }

        private void Start()
        {
            if (_loadOnStart && !string.IsNullOrEmpty(_ordersFileName))
            {
                LoadFromFile();
            }
        }

        private void OnDestroy()
        {
            _session?.Disconnect();
        }

        public bool TryGetNext(out KitData kit, out RecipeSO recipe) => _session.TryGetNext(out kit, out recipe);

        /// <summary>StreamingAssets 아래 주문 폴더의 주문 파일을 읽어 주문을 바꾼다.</summary>
        public OrderIntakeResult LoadFromFile()
        {
            string path = Path.Combine(Application.streamingAssetsPath, _ordersFolderName, _ordersFileName);
            return _session.Load(new FileOrderTextSource(path));
        }

        /// <summary>CSV 글자를 받아 주문을 바꾼다. 패널에서 붙여 넣은 내용을 넣을 때 쓴다.</summary>
        public OrderIntakeResult LoadText(string csv)
        {
            return _session.Load(new StringOrderTextSource(csv, "입력한 주문"));
        }
    }
}
