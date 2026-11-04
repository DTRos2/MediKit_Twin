using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 플레이 중에 콘솔에 찍히는 로그(키팅 설비와 로봇의 진행·실패 이유 포함)를 화면에 모아 보여 주고, 복사할 수 있게 하는 패널의 동작.
    /// 줄마다 시간, 분류 이름표(READY, CAMERA …), 글이 있고 경고와 오류 줄은 바탕색이 달라 훑어볼 때 눈에 띈다.
    /// 처음에는 경고와 오류만 보여 주고, 머리줄의 단추로 전체 보기와 전환하거나 전체 복사, 지우기를 한다. 새 줄이 오면 맨 아래로 내려간다.
    /// 로그 담기는 LogBuffer, 콘솔에서 받아 오기는 UnityLogCapture, 이름표 정하기는 LogTagClassifier,
    /// 보여 줄 줄 고르기는 LogRowList가 맡고, 이 클래스는 그것들을 잇고 단추를 처리한다. 화면이 어떻게 생겼는지는 모른다(KittingLogPanelView).
    /// 패널이 접힌 동안에도 로그는 계속 담고, 줄 갱신만 건너뛰었다가 펼치면 한 번에 채운다.
    /// </summary>
    public sealed class KittingLogPanelPresenter : MonoBehaviour
    {
        private const float CopiedNoticeSeconds = 2.5f;

        [SerializeField] private KittingLogPanelView _view;

        [Tooltip("담아 둘 최대 줄 수. 넘치면 오래된 줄부터 지운다. 화면에는 이 중 최근 줄만 보이고(줄 수는 프리팹에 미리 놓은 만큼), 복사는 담아 둔 전체를 한다.")]
        [SerializeField, Min(20)] private int _capacity = 400;
        [Tooltip("켜면(기본) 경고와 오류만 보여 주고 복사한다. 진행 로그(정보)는 키팅 한 번에 수백 줄이라 평소에는 가린다. 끄면 정보 로그까지 모두 보인다.")]
        [SerializeField] private bool _warningsAndErrorsOnly = true;

        private LogBuffer _buffer;
        private UnityLogCapture _capture;
        private LogRowList _rows;

        // 화면의 줄이 지금 목록과 다르다(접혀 있는 동안 못 고쳤거나 보기 조건이 바뀌었다).
        private bool _isViewStale = true;
        private float _noticeUntil = float.NegativeInfinity;
        private bool _hasNotice;

        private LogSeverity Minimum => _warningsAndErrorsOnly ? LogSeverity.Warning : LogSeverity.Info;

        private void Awake()
        {
            if (_view == null)
            {
                Debug.LogError($"{name}: 화면(KittingLogPanelView)이 있어야 한다.", this);
                enabled = false;
                return;
            }

            _buffer = new LogBuffer(_capacity);
            _capture = new UnityLogCapture(_buffer, () => Time.realtimeSinceStartupAsDouble);
            _rows = new LogRowList(LogTagClassifier.Default);
        }

        private void OnEnable()
        {
            _capture.Start();
            _view.FilterToggled += OnFilterToggled;
            _view.CopyRequested += OnCopyRequested;
            _view.ClearRequested += OnClearRequested;
            _view.ShowFilter(_warningsAndErrorsOnly);
        }

        private void Update()
        {
            ExpireNotice();

            if (_view.IsCollapsed)
            {
                _isViewStale = true;
                return;
            }

            bool isChanged = _rows.Refresh(_buffer, Minimum, _view.RowCapacity);
            if (isChanged || _isViewStale)
            {
                _view.ShowRows(_rows.Rows);
                _isViewStale = false;
            }
        }

        private void OnDisable()
        {
            _capture.Stop();
            _view.FilterToggled -= OnFilterToggled;
            _view.CopyRequested -= OnCopyRequested;
            _view.ClearRequested -= OnClearRequested;
        }

        private void OnFilterToggled()
        {
            _warningsAndErrorsOnly = !_warningsAndErrorsOnly;
            _view.ShowFilter(_warningsAndErrorsOnly);
        }

        // 화면에는 끝부분만 보여 주지만, 복사는 담아 둔 전체를 한다.
        private void OnCopyRequested()
        {
            GUIUtility.systemCopyBuffer = _buffer.ToText(Minimum);
            _view.ShowNotice(KittingLogPanelTexts.Copied(_buffer.CountAtLeast(Minimum)));
            _noticeUntil = Time.unscaledTime + CopiedNoticeSeconds;
            _hasNotice = true;
        }

        private void OnClearRequested()
        {
            _buffer.Clear();
        }

        private void ExpireNotice()
        {
            if (_hasNotice && Time.unscaledTime > _noticeUntil)
            {
                _view.ShowNotice(string.Empty);
                _hasNotice = false;
            }
        }
    }
}
