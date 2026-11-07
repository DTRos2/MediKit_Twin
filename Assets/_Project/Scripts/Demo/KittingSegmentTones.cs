using System.Collections.Generic;
using MediKitTwin.ArmA;
using MediKitTwin.Dashboard;

namespace MediKitTwin.Demo
{
    /// <summary>키팅 구성품 상태 목록을 진행 막대 칸 색(SegmentTone)으로 바꾼다. Unity 없이 시험할 수 있다.</summary>
    public static class KittingSegmentTones
    {
        public static SegmentTone From(KittingItemState state)
        {
            switch (state)
            {
                case KittingItemState.Placed: return SegmentTone.Done;
                case KittingItemState.Picking:
                case KittingItemState.Placing:
                    return SegmentTone.Active;
                case KittingItemState.Skipped: return SegmentTone.Skipped;
                default: return SegmentTone.Pending;
            }
        }

        public static IReadOnlyList<SegmentTone> From(IReadOnlyList<KittingItemState> states)
        {
            var tones = new List<SegmentTone>(states.Count);
            foreach (KittingItemState state in states)
            {
                tones.Add(From(state));
            }

            return tones;
        }
    }
}
