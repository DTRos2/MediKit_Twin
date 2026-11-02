using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 플레이 중에 콘솔에 찍히는 로그(키팅 설비와 로봇의 진행·실패 이유 포함)를 화면에 모아 보여 주고, 복사할 수 있게 하는 패널(IMGUI).
    /// 콘솔을 열지 않고도 '복사' 버튼 한 번으로 전체를 클립보드에 담고, 글 상자에서 일부만 골라 Ctrl+C로 복사할 수도 있다.
    /// 로그 담기는 LogBuffer, 콘솔에서 받아 오기는 UnityLogCapture가 맡고, 이 패널은 그리기와 버튼만 한다.
    /// </summary>
    public sealed class KittingLogDemoPanel : DemoPanel
    {
        private const float LogAreaHeightPx = 220f;
        private const int ShownLineCount = 80;
        private const float CopiedNoticeSeconds = 2.5f;

        [Tooltip("담아 둘 최대 줄 수. 넘치면 오래된 줄부터 지운다.")]
        [SerializeField, Min(20)] private int _capacity = 400;
        [Tooltip("켜면 경고와 오류만 보여 주고 복사한다.")]
        [SerializeField] private bool _warningsAndErrorsOnly;

        private LogBuffer _buffer;
        private UnityLogCapture _capture;
        private Vector2 _logScroll;
        private string _shownText = string.Empty;
        private int _shownVersion = -1;
        private LogSeverity _shownMinimum;
        private float _copiedAt = float.NegativeInfinity;
        private int _copiedLineCount;

        // 오른쪽 위의 Kitting (Arm A) 패널(가로 360, 여백 10) 바로 왼쪽에 10px 띄워 놓는다.
        public KittingLogDemoPanel()
            : base("Kitting Log", DemoPanelAnchor.TopRight, new Vector2(380f, 10f), new Vector2(480f, 320f))
        {
        }

        private LogSeverity Minimum => _warningsAndErrorsOnly ? LogSeverity.Warning : LogSeverity.Info;

        private void Awake()
        {
            _buffer = new LogBuffer(_capacity);
            _capture = new UnityLogCapture(_buffer, () => Time.realtimeSinceStartupAsDouble);
        }

        private void OnEnable()
        {
            _capture.Start();
        }

        private void OnDisable()
        {
            _capture.Stop();
        }

        // 보여 줄 글은 그리기 밖에서 만든다. IMGUI는 한 프레임 안에서 같은 모양을 기대하기 때문이다.
        private void Update()
        {
            if (_buffer.Version == _shownVersion && Minimum == _shownMinimum)
            {
                return;
            }

            _shownText = _buffer.TailText(Minimum, ShownLineCount);
            _shownVersion = _buffer.Version;
            _shownMinimum = Minimum;
            _logScroll.y = float.MaxValue; // 새 줄이 보이도록 맨 아래로(IMGUI가 끝까지로 맞춘다).
        }

        protected override void DrawContents()
        {
            DrawToolbar();

            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(LogAreaHeightPx));
            // 돌려받는 글(고친 글)은 버린다. 골라서 복사만 되게 하려는 것이다.
            GUILayout.TextArea(_shownText, GuiLabelStyles.WrappedTextArea);
            GUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("복사", GUILayout.Width(70f)))
            {
                CopyAll();
            }

            if (GUILayout.Button("지우기", GUILayout.Width(70f)))
            {
                _buffer.Clear();
            }

            _warningsAndErrorsOnly = GUILayout.Toggle(_warningsAndErrorsOnly, "경고·오류만");

            if (Time.unscaledTime - _copiedAt <= CopiedNoticeSeconds)
            {
                GUILayout.Label(_copiedLineCount > 0 ? $"{_copiedLineCount}줄 복사했다" : "복사할 로그가 없다");
            }

            GUILayout.EndHorizontal();
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
