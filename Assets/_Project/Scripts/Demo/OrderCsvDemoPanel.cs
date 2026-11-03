using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Orders;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 주문 CSV를 화면에서 넣어 보는 패널. 글자를 붙여 넣고 "적용"을 누르면 주문 관리가 새 주문으로 바뀌고,
    /// 로봇암 A는 그 주문대로 키팅한다. 주문 줄, 주문 수, 키트 수, 건너뛴 행의 경고, 보류와 대기 현황을 보여 준다.
    /// 주문은 StreamingAssets 아래 CSV 파일이 기본이고(SRS FR-01), 이 패널은 그 위에 덮어쓰는 시험용 편의이다.
    /// 주문 입력(IOrderInput)과 현황(IOrderStatus)만 알고 구체 클래스는 모르며, 이 클래스는 입력과 표시만 맡는다.
    /// </summary>
    public sealed class OrderCsvDemoPanel : DemoPanel
    {
        // 주문 줄이 이 개수를 넘으면 목록 안에서 스크롤한다. 주문이 많아도 패널 높이가 늘지 않게 한다.
        private const int VisibleOrderRows = 3;
        private const string GuideText = "순번,병원명,키트 코드,수량 — 한 줄에 주문 1건";
        private const string EmptyOrdersText = "주문이 없다";
        private const string IssueBullet = "• ";

        [Tooltip("주문 입력(IOrderInput)과 현황(IOrderStatus)을 구현한 컴포넌트(보통 OrderSessionBehaviour). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _orders;
        [Tooltip("CSV 입력 상자의 높이(px).")]
        [SerializeField, Min(40f)] private float _csvAreaHeight = 72f;
        [Tooltip("경고 목록이 차지하는 최대 높이(px). 넘치면 목록 안에서 스크롤한다.")]
        [SerializeField, Min(20f)] private float _issuesMaxHeight = 60f;

        [Tooltip("\"샘플 채우기\"를 누르면 입력창에 들어가는 주문. 열은 순번, 병원명, 키트 코드, 수량이다.")]
        [SerializeField, TextArea(3, 8)] private string _sampleCsv = "1,A병원,EM,2\n2,B병원,DX,3\n3,C병원,BL,2";

        private IOrderInput _input;
        private IOrderStatus _status;
        private string _csvText = string.Empty;
        private Vector2 _ordersScroll;
        private Vector2 _issuesScroll;

        public OrderCsvDemoPanel()
            : base("주문", "ORDERS · CSV", DemoPanelSlots.Orders)
        {
        }

        private void Awake()
        {
            _input = _orders as IOrderInput ?? DemoComponentFinder.FindFirst<IOrderInput>();
            _status = _orders as IOrderStatus ?? _input as IOrderStatus ?? DemoComponentFinder.FindFirst<IOrderStatus>();

            if (_input == null || _status == null)
            {
                Debug.LogError($"{name}: 주문 입력(IOrderInput)과 현황(IOrderStatus)을 구현한 컴포넌트를 찾을 수 없다.", this);
                enabled = false;
                return;
            }

            _csvText = _sampleCsv;
        }

        protected override void DrawHeaderRight()
        {
            OrderIntakeResult intake = _input.LastIntake;
            if (intake == null)
            {
                return;
            }

            DashboardText.Inline($"{intake.Orders.Count}건 · 키트 {intake.KitRequests.Count}개", TextRole.Mono, DashboardPalette.Default.Muted);
        }

        protected override void DrawContents()
        {
            OrderIntakeResult intake = _input.LastIntake;

            DashboardText.Paragraph(GuideText, TextRole.Small, DashboardPalette.Default.Muted);
            DashboardSpacing.Row();
            DrawOrderRows(intake);
            DashboardSpacing.Row();
            _csvText = DashboardField.TextArea(_csvText, _csvAreaHeight);
            DashboardSpacing.Row();
            DrawButtons();
            DashboardSpacing.Row();
            DrawSummaryChips(intake);
            DrawIssues(intake);
        }

        private void DrawOrderRows(OrderIntakeResult intake)
        {
            int count = intake != null ? intake.Orders.Count : 0;
            if (count == 0)
            {
                DashboardText.Line(EmptyOrdersText, TextRole.Small, DashboardPalette.Default.Faint);
                return;
            }

            float height = Mathf.Min(count, VisibleOrderRows) * (OrderRowView.Height + OrderRowView.Spacing);
            _ordersScroll = DashboardScroll.Begin(_ordersScroll, height);
            foreach (OrderRecord order in intake.Orders)
            {
                OrderRowView.Draw(order);
            }

            DashboardScroll.End();
        }

        // 누른 이벤트 안에서 주문이 바뀌면 같은 OnGUI 안에서 줄 수가 달라지므로 다음 레이아웃으로 미룬다.
        private void DrawButtons()
        {
            using (DashboardRow.Begin())
            {
                if (DashboardButton.Draw("적용", ButtonKind.Primary))
                {
                    string text = _csvText;
                    RunAtNextLayout(() => _input.LoadText(text));
                }

                DashboardSpacing.Block();
                if (DashboardButton.Draw("샘플 채우기"))
                {
                    RunAtNextLayout(FillSample);
                }

                DashboardSpacing.Block();
                if (DashboardButton.Draw("파일 읽기"))
                {
                    RunAtNextLayout(() => _input.LoadFromFile());
                }
            }
        }

        // 글 상자는 포커스를 가진 동안 자기 글을 따로 들고 있으므로, 포커스를 풀어야 바뀐 글이 보인다.
        private void FillSample()
        {
            _csvText = _sampleCsv;
            GUIUtility.keyboardControl = 0;
        }

        private void DrawSummaryChips(OrderIntakeResult intake)
        {
            int issueCount = intake != null ? intake.Issues.Count : 0;

            using (DashboardRow.Begin())
            {
                Chip.Draw($"보류 {_status.HeldOrderCount}건");
                DashboardSpacing.Block();
                Chip.Draw($"대기 키트 {_status.PendingKitCount}개");
                DashboardSpacing.Block();
                if (issueCount == 0)
                {
                    Chip.Draw("경고 없음", ChipTone.Success, withDot: true);
                }
                else
                {
                    Chip.Draw($"경고 {issueCount}건", ChipTone.Warning, withDot: true);
                }
            }
        }

        private void DrawIssues(OrderIntakeResult intake)
        {
            if (intake == null || intake.Issues.Count == 0)
            {
                return;
            }

            DashboardSpacing.Row();
            _issuesScroll = DashboardScroll.Begin(_issuesScroll, _issuesMaxHeight);
            foreach (OrderIssue issue in intake.Issues)
            {
                DashboardText.Paragraph(IssueBullet + issue.Message, TextRole.Small, DashboardPalette.Default.Warning);
            }

            DashboardScroll.End();
        }
    }
}
