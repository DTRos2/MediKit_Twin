using System;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 옮기는 동안(Begin ~ End) 물리 스텝마다 베이스의 자리·방향과 쥔 물건의 자세를 재서 모으고, 끝나면 요약을 로그로 남긴다.
    /// 물건 하나를 놓는 동안 쓰고 버린다. 이동이 여러 번 되풀이되면 "1번째", "2번째"로 이름을 붙인다.
    /// 재기만 하고 아무것도 움직이지 않는다. 끝내지 않은 채 다시 시작하면 앞의 것은 도착하지 못한 것으로 끝낸다.
    /// </summary>
    public sealed class BaseMotionRecorder
    {
        private readonly BaseMotionProbe _probe;
        private readonly Func<HeldItemFrame?> _measureHeld;
        private readonly BoxPlaceDiagnostics _diagnostics;
        private readonly Action<string> _log;

        private BaseMotionTrace _trace;
        private int _moveCount;

        /// <param name="measureHeld">지금 쥔 물건이 도구에 대해 어떤 자세인지. 재지 못하면 null을 돌려준다.</param>
        /// <param name="log">요약을 남기는 쪽. 한 줄씩 받는다.</param>
        public BaseMotionRecorder(BaseMotionProbe probe, Func<HeldItemFrame?> measureHeld, BoxPlaceDiagnostics diagnostics, Action<string> log)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _measureHeld = measureHeld ?? throw new ArgumentNullException(nameof(measureHeld));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public bool IsRecording => _trace != null;

        /// <summary>베이스를 옮기기 시작한다. 지금 자세를 첫 스텝으로 잰다.</summary>
        public void Begin()
        {
            if (IsRecording)
            {
                End(arrived: false);
            }

            _moveCount++;
            _trace = new BaseMotionTrace(_probe.OnsetSlipM, _probe.StillSpeedMps, _probe.StillYawRateDegPerSec);
            AddSample();
            _probe.Steps.Stepped += AddSample;
        }

        /// <summary>베이스 옮기기를 끝낸다. 지금 자세를 마지막 스텝으로 재고 요약을 남긴다. 시작하지 않았으면 아무것도 하지 않는다.</summary>
        public void End(bool arrived)
        {
            if (!IsRecording)
            {
                return;
            }

            _probe.Steps.Stepped -= AddSample;
            AddSample();

            BaseMotionTrace trace = _trace;
            _trace = null;

            BaseMotionSummary summary = trace.Summarize();
            string label = $"{_moveCount}번째";
            _log($"박스 진단: {_diagnostics.DescribeBaseMotion(label, summary, arrived, _probe.OnsetSlipM)}");

            // 베이스가 움직이지 않았고 밀리지도 않은 이동은 추이를 적어 봐야 모두 0이라 요약만 남긴다.
            if (summary.HasMotion || summary.MaxSlipM >= _probe.OnsetSlipM)
            {
                _log($"박스 진단: {_diagnostics.DescribeBaseMotionTrace(label, summary)}");
            }
        }

        private void AddSample()
        {
            if (_trace == null)
            {
                return;
            }

            _trace.Add(new BaseMotionSample(_probe.Now(), _probe.Mobile.Position, _probe.Mobile.YawDeg, _measureHeld()));
        }
    }
}
