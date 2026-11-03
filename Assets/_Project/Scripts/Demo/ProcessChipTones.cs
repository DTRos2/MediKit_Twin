using MediKitTwin.Dashboard;

namespace MediKitTwin.Demo
{
    /// <summary>공정 단계를 칩 색 계열(ChipTone)로 바꾼다. 설비 상태의 색은 키팅 패널과 같은 규칙(KittingChipTones)을 쓴다.</summary>
    public static class ProcessChipTones
    {
        public static ChipTone Stage(ProcessStage stage)
        {
            switch (stage)
            {
                case ProcessStage.None:
                    return ChipTone.Neutral;
                case ProcessStage.Rejecting:
                case ProcessStage.Rejected:
                    return ChipTone.Danger;
                case ProcessStage.Done:
                    return ChipTone.Success;
                default:
                    return ChipTone.Accent;
            }
        }
    }
}
