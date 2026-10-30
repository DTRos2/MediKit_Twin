using System.Text;
using MediKitTwin.Common;
using MediKitTwin.Orders;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 주문 CSV를 화면에서 넣어 보는 패널. 글자를 붙여 넣고 "적용"을 누르면 주문 관리가 새 주문으로 바뀌고,
    /// 로봇암 A는 그 주문대로 키팅한다. 주문 수, 키트 수, 건너뛴 행의 경고, 보류와 대기 현황을 보여 준다.
    /// 주문은 StreamingAssets 아래 CSV 파일이 기본이고(SRS FR-01), 이 패널은 그 위에 덮어쓰는 시험용 편의이다.
    /// 주문 입력(IOrderInput)과 현황(IOrderStatus)만 알고 구체 클래스는 모르며, 이 클래스는 입력과 표시만 맡는다.
    /// </summary>
    public sealed class OrderCsvDemoPanel : DemoPanel
    {
        [Tooltip("주문 입력(IOrderInput)과 현황(IOrderStatus)을 구현한 컴포넌트(보통 OrderSessionBehaviour). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _orders;
        [SerializeField, Min(40f)] private float _inputHeight = 110f;
        [SerializeField, Min(40f)] private float _issuesHeight = 90f;

        [Tooltip("\"샘플 채우기\"를 누르면 입력창에 들어가는 주문. 열은 순번, 병원명, 키트 코드, 수량이다.")]
        [SerializeField, TextArea(3, 8)] private string _sampleCsv = "1,A병원,EM,2\n2,B병원,DX,3\n3,C병원,BL,2";

        private IOrderInput _input;
        private IOrderStatus _status;
        private string _csvText = string.Empty;
        private Vector2 _issuesScroll;
        private string _message = string.Empty;

        public OrderCsvDemoPanel()
            : base("Orders (CSV)", DemoPanelAnchor.TopLeft, new Vector2(10f, 10f), new Vector2(340f, 400f))
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

        protected override void DrawContents()
        {
            GUILayout.Label("순번,병원명,키트 코드,수량 (한 줄에 주문 1건)");
            _csvText = GUILayout.TextArea(_csvText, GUILayout.Height(_inputHeight));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("적용"))
            {
                Apply(_input.LoadText(_csvText));
            }

            if (GUILayout.Button("샘플 채우기"))
            {
                _csvText = _sampleCsv;
            }

            if (GUILayout.Button("파일 읽기"))
            {
                Apply(_input.LoadFromFile());
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(_message);
            GUILayout.Label($"보류 {_status.HeldOrderCount}건 · 대기 키트 {_status.PendingKitCount}개");

            DrawIssues(_input.LastIntake);
        }

        private void Apply(OrderIntakeResult result)
        {
            _message = $"주문 {result.Orders.Count}건 · 키트 {result.KitRequests.Count}개로 바꿨다.";
        }

        private void DrawIssues(OrderIntakeResult result)
        {
            _issuesScroll = GUILayout.BeginScrollView(_issuesScroll, GUILayout.Height(_issuesHeight));
            if (result == null || result.Issues.Count == 0)
            {
                GUILayout.Label("경고 없음");
            }
            else
            {
                var text = new StringBuilder();
                foreach (OrderIssue issue in result.Issues)
                {
                    text.Append("• ").AppendLine(issue.Message);
                }

                GUILayout.Label(text.ToString());
            }

            GUILayout.EndScrollView();
        }
    }
}
