using System;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// E2E 시험이 시간을 어떻게 흘리는지. 기본은 프레임마다 시간을 고정(captureFramerate)해 같은 입력이 같은 결과를 내게 한다.
    /// 에디터에서 사람이 플레이하면 프레임 시간이 제각각이라, 시험에서만 되고 에디터에서 안 되는 일을 가리려고
    /// 환경 변수로 고정을 끄거나 프레임 수를 제한할 수 있게 한다. 아무것도 정하지 않으면 지금까지와 똑같이 돈다.
    /// </summary>
    public readonly struct FrameTimingSettings
    {
        // 0이면 고정하지 않는다(프레임마다 실제 흐른 시간만큼 흐른다).
        public const string CaptureVariable = "MEDIKIT_E2E_CAPTURE_FPS";

        // 0이면 제한하지 않는다. 양수면 그 프레임 수로 제한해 에디터처럼 프레임당 물리 스텝 수가 흔들리게 한다.
        public const string TargetVariable = "MEDIKIT_E2E_TARGET_FPS";

        public const int DefaultCaptureFramerate = 50;
        public const int DefaultTargetFramerate = 0;

        public FrameTimingSettings(int captureFramerate, int targetFramerate)
        {
            CaptureFramerate = captureFramerate;
            TargetFramerate = targetFramerate;
        }

        /// <summary>프레임마다 시간을 이 프레임 수 기준으로 고정한다. 0이면 고정하지 않는다.</summary>
        public int CaptureFramerate { get; }

        /// <summary>프레임 수 상한. 0이면 제한하지 않는다.</summary>
        public int TargetFramerate { get; }

        public static FrameTimingSettings FromEnvironment()
        {
            return Parse(Environment.GetEnvironmentVariable(CaptureVariable), Environment.GetEnvironmentVariable(TargetVariable));
        }

        /// <param name="captureText">고정 프레임 수 글. 0 이상의 정수. 비었거나 음수이거나 정수가 아니면 기본값.</param>
        /// <param name="targetText">프레임 수 상한 글. 0 이상의 정수. 비었거나 음수이거나 정수가 아니면 제한하지 않는다.</param>
        public static FrameTimingSettings Parse(string captureText, string targetText)
        {
            return new FrameTimingSettings(
                ParseNonNegative(captureText, DefaultCaptureFramerate),
                ParseNonNegative(targetText, DefaultTargetFramerate));
        }

        private static int ParseNonNegative(string text, int fallback)
        {
            return int.TryParse(text, out int value) && value >= 0 ? value : fallback;
        }
    }
}
