using System;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// Unity 콘솔에 찍히는 로그(Debug.Log 계열)를 받아 LogBuffer에 옮겨 담는다. Start와 Stop 사이에 찍힌 로그만 담긴다.
    /// 콘솔처럼 호출 위치(스택)는 길어서 버리고, 예외만 원인을 알 수 있도록 스택을 함께 남긴다.
    /// 시각은 주입받으므로 시험에서 바꿀 수 있다.
    /// </summary>
    public sealed class UnityLogCapture
    {
        private readonly LogBuffer _buffer;
        private readonly Func<double> _now;
        private bool _isCapturing;

        /// <param name="now">플레이를 시작하고 흐른 시간(초). 보통 Time.realtimeSinceStartupAsDouble.</param>
        public UnityLogCapture(LogBuffer buffer, Func<double> now)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _now = now ?? throw new ArgumentNullException(nameof(now));
        }

        public bool IsCapturing => _isCapturing;

        public void Start()
        {
            if (_isCapturing)
            {
                return;
            }

            Application.logMessageReceived += OnLogReceived;
            _isCapturing = true;
        }

        public void Stop()
        {
            if (!_isCapturing)
            {
                return;
            }

            Application.logMessageReceived -= OnLogReceived;
            _isCapturing = false;
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            string text = type == LogType.Exception && !string.IsNullOrEmpty(stackTrace)
                ? $"{condition}\n{stackTrace.TrimEnd()}"
                : condition;
            _buffer.Add(_now(), LogSeverityMap.From(type), text?.TrimEnd());
        }
    }
}
