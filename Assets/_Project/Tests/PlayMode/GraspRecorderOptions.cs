namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// GraspHoldRecorder가 언제 무엇을 적을지 정하는 값들. 기록기 안에 숫자를 박아 두지 않고 한곳에 모아,
    /// 다른 물건이나 다른 프레임 간격으로 쥐는 시험이 값만 바꿔 같은 기록기를 쓰게 한다.
    /// 아무것도 정하지 않으면 지금까지의 기록과 똑같이 나온다.
    /// </summary>
    public sealed class GraspRecorderOptions
    {
        public const float DefaultLaterSnapshotSeconds = 0.4f;
        public const float DefaultAfterReleaseSeconds = 1.5f;
        public const float DefaultWindowSeconds = 0.5f;
        public const int DefaultMinWindowSamples = 5;
        public const int DefaultRecentFrameCount = 31;
        public const float DefaultLargeItemSpanMm = 30f;
        public const int DefaultMaxLargeSpanDumps = 4;
        public const float DefaultMinReportedOverlapM = 0.0003f;

        /// <summary>쥔 뒤 이만큼 지나서 한 번 더 적는다. 물건이 EnablePhysics로 풀린 뒤 미끄러졌는지 보려는 것이다.</summary>
        public float LaterSnapshotSeconds { get; set; } = DefaultLaterSnapshotSeconds;

        /// <summary>놓은 뒤에도 이만큼은 흔들림을 잰다. 놓을 때 손가락이 떠는지 보려는 것이다.</summary>
        public float AfterReleaseSeconds { get; set; } = DefaultAfterReleaseSeconds;

        /// <summary>흔들림을 한 줄로 묶어 적는 창의 길이.</summary>
        public float WindowSeconds { get; set; } = DefaultWindowSeconds;

        /// <summary>창에 표본이 이보다 적으면 적지 않는다. 2차 차분이 의미를 가지려면 몇 개는 있어야 한다.</summary>
        public int MinWindowSamples { get; set; } = DefaultMinWindowSamples;

        /// <summary>놓기 직전 프레임별로 풀어 적을 개수. 50fps에서 0.6초다.</summary>
        public int RecentFrameCount { get; set; } = DefaultRecentFrameCount;

        /// <summary>물건이 도구 기준으로 이만큼(mm) 퍼진 창은 프레임별로 풀어 적는다.</summary>
        public float LargeItemSpanMm { get; set; } = DefaultLargeItemSpanMm;

        /// <summary>프레임별 풀어 적기를 이만큼까지만 한다. 로그가 길어지는 것을 막는다.</summary>
        public int MaxLargeSpanDumps { get; set; } = DefaultMaxLargeSpanDumps;

        /// <summary>손가락과 다른 충돌체가 이보다 깊게(m) 겹쳐야 적는다. 이보다 얕은 겹침은 접촉 오차다.</summary>
        public float MinReportedOverlapM { get; set; } = DefaultMinReportedOverlapM;
    }
}
