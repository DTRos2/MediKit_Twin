using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;

namespace MediKitTwin.Demo
{
    /// <summary>설비 상태와 구성품 단계를 칩 색 계열(ChipTone)로 바꾼다. 어떤 상태가 어떤 색인지를 그리기 코드에서 떼어 한곳에 둔다.</summary>
    public static class KittingChipTones
    {
        public static ChipTone Station(StationState state)
        {
            switch (state)
            {
                case StationState.Running: return ChipTone.Accent;
                case StationState.Fault: return ChipTone.Danger;
                default: return ChipTone.Neutral;
            }
        }

        public static ChipTone Item(KittingItemState state)
        {
            switch (state)
            {
                case KittingItemState.Picking:
                case KittingItemState.Placing:
                    return ChipTone.Accent;
                case KittingItemState.Placed: return ChipTone.Success;
                case KittingItemState.Skipped: return ChipTone.Warning;
                default: return ChipTone.Neutral;
            }
        }
    }
}
