using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 파일을 받아 주문 관리를 만들고, 실행 중에 새 주문 파일로 바꿔 끼울 수 있게 한다. (FR-01, FR-13)
    /// 로봇암 A와 대시보드는 이 객체를 IKitSource와 IOrderStatus로 한 번만 잡아 두면 되고, 주문 파일이 바뀌어도
    /// 참조를 다시 잡을 필요가 없다. 호출은 지금의 OrderManager에 그대로 넘긴다.
    /// 로트번호는 실행 안에서 유일해야 하므로 주문 파일을 바꿔도 이어서 붙이고, KitSequence는 파일마다 1부터 다시 센다
    /// (같은 파일이면 같은 키트에서 누락이 나야 하기 때문이다, NFR-06). 바꾸는 순간 만들고 있던 키트는 끝까지 만든다.
    /// </summary>
    public sealed class OrderSession : IKitSource, IOrderStatus
    {
        private readonly IRecipeCatalog _recipes;
        private readonly OrderIntakeSettings _intakeSettings;
        private readonly OrderManagerSettings _managerSettings;
        private readonly IOrderEventSink _events;
        private readonly Func<OrderManager, IDisposable> _connect;
        private readonly ILotNumberGenerator _lotNumbers;

        private OrderManager _manager;
        private IDisposable _connection;

        /// <param name="connect">새 주문 관리를 외부 신호(CellEvents)에 이어 주는 함수. 돌려준 객체는 주문 파일을 바꿀 때 해제한다. 필요 없으면 null.</param>
        public OrderSession(
            IRecipeCatalog recipes,
            OrderIntakeSettings intakeSettings,
            OrderManagerSettings managerSettings,
            IOrderEventSink events,
            Func<OrderManager, IDisposable> connect = null)
        {
            _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            _intakeSettings = intakeSettings ?? throw new ArgumentNullException(nameof(intakeSettings));
            _managerSettings = managerSettings ?? throw new ArgumentNullException(nameof(managerSettings));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _connect = connect;
            _lotNumbers = new SequentialLotNumberGenerator(intakeSettings.LotPrefix, intakeSettings.LotDigits);
        }

        /// <summary>가장 최근에 불러온 결과. 아직 불러온 적이 없으면 null이다.</summary>
        public OrderIntakeResult LastIntake { get; private set; }

        public int HeldOrderCount => _manager != null ? _manager.HeldOrderCount : 0;

        public int PendingKitCount => _manager != null ? _manager.PendingKitCount : 0;

        /// <summary>
        /// 주문 파일을 읽어 지금의 주문 관리를 새것으로 바꾼다. 건너뛴 행의 경고는 WarningRaised로 알린다.
        /// 파일을 읽지 못하거나 유효한 주문이 없으면 키트 종류마다 1건인 기본 주문을 쓴다. (FR-01 예외 처리)
        /// </summary>
        public OrderIntakeResult Load(IOrderTextSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var parser = new CsvOrderParser(
                new CsvLineSplitter(_intakeSettings.Delimiter, _intakeSettings.Quote),
                _intakeSettings.Columns,
                _intakeSettings.CommentMarker,
                new IOrderRowValidator[]
                {
                    new KnownKitCodeValidator(_recipes.KitCodes),
                    new QuantityRangeValidator(_intakeSettings.MinQuantity, _intakeSettings.MaxQuantity),

                    // 순번 중복 검사는 마지막에 둔다. 앞 검증기가 거부한 행이 순번을 차지하지 않게 하기 위해서다.
                    new UniqueSequenceValidator()
                });

            var defaultOrders = new PerKitCodeDefaultOrderProvider(
                new List<string>(_recipes.KitCodes), _intakeSettings.DefaultHospitalName, _intakeSettings.DefaultQuantity);

            var intake = new OrderIntake(
                new OrderLoader(source, parser, defaultOrders),
                new KitRequestPlanner(_lotNumbers)).Run();

            foreach (OrderIssue issue in intake.Issues)
            {
                _events.WarningRaised(issue.Code, issue.Message);
            }

            _connection?.Dispose();
            _manager = new OrderManager(intake, _recipes, _events, _managerSettings);
            _connection = _connect?.Invoke(_manager);

            LastIntake = intake;
            return intake;
        }

        public bool TryGetNext(out KitData kit, out RecipeSO recipe)
        {
            if (_manager == null)
            {
                kit = null;
                recipe = null;
                return false;
            }

            return _manager.TryGetNext(out kit, out recipe);
        }

        /// <summary>외부 신호 연결을 끊는다. 씬을 떠날 때 부른다.</summary>
        public void Disconnect()
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
