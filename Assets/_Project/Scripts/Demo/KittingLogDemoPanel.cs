using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 플레이 중에 콘솔에 찍히는 로그(키팅 설비와 로봇의 진행·실패 이유 포함)를 화면에 모아 보여 주고, 복사할 수 있게 하는 패널(IMGUI).
    /// 줄마다 시간, 분류 이름표(READY, CAMERA …), 글이 있고 경고와 오류 줄은 바탕색이 달라 훑어볼 때 눈에 띈다.
    /// 처음에는 경고와 오류만 보여 주고, 머리줄의 버튼으로 전체 보기와 전환하거나 전체 복사, 지우기를 한다. 새 줄이 오면 맨 아래로 내려간다.
    /// 로그 담기는 LogBuffer, 콘솔에서 받아 오기는 UnityLogCapture, 이름표 정하기는 LogTagClassifier,
    /// 줄 높이와 목록 간직은 LogRowList가 맡고, 이 패널은 그리기와 버튼만 한다.
    /// </summary>
    public sealed class KittingLogDemoPanel : DemoPanel
    {
        private const float CopiedNoticeSeconds = 2.5f;

        // 스크롤 영역이 패널 안쪽 높이와 정확히 같으면 바깥 스크롤에 1px 넘쳐 막대가 생길 수 있어 조금 줄인다.
        private const float ScrollHeightTrimPx = 1f;

        [Tooltip("담아 둘 최대 줄 수. 넘치면 오래된 줄부터 지운다.")]
        [SerializeField, Min(20)] private int _capacity = 400;
        [Tooltip("화면에 그리는 최근 줄 수. 줄마다 그리는 비용이 있어 담아 둔 전체를 그리지 않는다. 복사는 담아 둔 전체를 한다.")]
        [SerializeField, Min(10)] private int _visibleLineLimit = 60;
        [Tooltip("켜면(기본) 경고와 오류만 보여 주고 복사한다. 진행 로그(정보)는 키팅 한 번에 수백 줄이라 평소에는 가린다. 끄면 정보 로그까지 모두 보인다.")]
        [SerializeField] private bool _warningsAndErrorsOnly = true;

        private LogBuffer _buffer;
        private UnityLogCapture _capture;
        private LogRowList _rows;
        private Vector2 _scroll;
        private float _copiedAt = float.NegativeInfinity;
        private int _copiedLineCount;

        public KittingLogDemoPanel()
            : base("키팅 로그", "KITTING LOG", DemoPanelSlots.Log)
        {
        }

        private LogSeverity Minimum => _warningsAndErrorsOnly ? LogSeverity.Warning : LogSeverity.Info;

        private void Awake()
        {
            _buffer = new LogBuffer(_capacity);
            _capture = new UnityLogCapture(_buffer, () => Time.realtimeSinceStartupAsDouble);
            _rows = new LogRowList(LogTagClassifier.Default, LogRowView.HeightOf);
        }

        private void OnEnable()
        {
            _capture.Start();
        }

        private void OnDisable()
        {
            _capture.Stop();
        }

        protected override void DrawHeaderRight()
        {
            if (Time.unscaledTime - _copiedAt <= CopiedNoticeSeconds)
            {
                string notice = _copiedLineCount > 0 ? $"{_copiedLineCount}줄 복사했다" : "복사할 로그가 없다";
                DashboardText.Inline(notice, TextRole.Small, DashboardPalette.Default.Muted);
                DashboardSpacing.Block();
            }

            // 눌러서 바뀌는 것이 줄 목록뿐이라 이벤트 중간에 바꿔도 위젯 수는 같지만, 필터는 다음 레이아웃부터 적용한다.
            ButtonKind filterKind = _warningsAndErrorsOnly ? ButtonKind.Selected : ButtonKind.Secondary;
            if (DashboardButton.DrawFit("경고·오류만", filterKind))
            {
                RunAtNextLayout(() => _warningsAndErrorsOnly = !_warningsAndErrorsOnly);
            }

            DashboardSpacing.Block();
            if (DashboardButton.DrawFit("복사"))
            {
                RunAtNextLayout(CopyAll);
            }

            DashboardSpacing.Block();
            if (DashboardButton.DrawFit("지우기"))
            {
                RunAtNextLayout(_buffer.Clear);
            }
        }

        protected override void DrawContents()
        {
            // 줄 높이 계산은 글 크기를 재야 해서 비싸다. 레이아웃 이벤트에서만 새로 만들고, 그리기 이벤트는 같은 목록을 읽는다.
            // 그래야 한 프레임 안에서 배치와 그리기가 어긋나지 않는다.
            if (Event.current.type == EventType.Layout && _rows.Refresh(_buffer, Minimum, _visibleLineLimit, ContentWidth))
            {
                _scroll.y = float.MaxValue; // 새 줄이 보이도록 맨 아래로(IMGUI가 끝까지로 맞춘다).
            }

            _scroll = DashboardScroll.Begin(_scroll, ContentHeight - ScrollHeightTrimPx);
            foreach (LogRowEntry row in _rows.Rows)
            {
                LogRowView.Draw(row.Line, row.Tag, ContentWidth, row.Height);
            }

            DashboardScroll.End();
        }

        // 화면에는 끝부분만 보여 주지만, 복사는 담아 둔 전체를 한다.
        private void CopyAll()
        {
            GUIUtility.systemCopyBuffer = _buffer.ToText(Minimum);
            _copiedLineCount = _buffer.CountAtLeast(Minimum);
            _copiedAt = Time.unscaledTime;
        }
    }
}
