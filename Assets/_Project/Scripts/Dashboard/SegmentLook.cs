using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 진행 막대 한 칸이 어떻게 보이는지(속 색과 테두리). 칸의 상태(SegmentTone)에서 정한다.
    /// 속을 강조색으로 채우는 것은 끝난(Done) 칸뿐이다. 진행 중인 칸까지 채우면, 아직 놓지 않은 칸이 이미 끝난 것처럼 보인다.
    /// 그래서 진행 중인 칸은 속이 빈 칸에 강조색 테두리만 둘러 "지금 이 칸을 하는 중"이라고만 알린다.
    /// 그리지 않고 색만 정하므로 Unity 화면 없이 시험할 수 있다.
    /// </summary>
    public readonly struct SegmentLook
    {
        private SegmentLook(Color fill, bool hasOutline, Color outline)
        {
            Fill = fill;
            HasOutline = hasOutline;
            Outline = outline;
        }

        /// <summary>칸 속 색.</summary>
        public Color Fill { get; }

        /// <summary>테두리를 두르는 칸이면 true.</summary>
        public bool HasOutline { get; }

        /// <summary>테두리 색. HasOutline이 true일 때만 쓴다.</summary>
        public Color Outline { get; }

        public static SegmentLook For(SegmentTone tone, DashboardPalette palette)
        {
            switch (tone)
            {
                case SegmentTone.Done:
                    return new SegmentLook(palette.Accent, false, default);
                case SegmentTone.Active:
                    return new SegmentLook(palette.Track, true, palette.Accent);
                case SegmentTone.Skipped:
                    return new SegmentLook(palette.SurfaceBorder, false, default);
                default:
                    return new SegmentLook(palette.Track, false, default);
            }
        }
    }
}
