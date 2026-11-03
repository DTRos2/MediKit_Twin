using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>칩(작은 알약 모양 표시)의 색 계열.</summary>
    public enum ChipTone
    {
        Neutral,
        Success,
        Warning,
        Danger,
        Accent,
    }

    /// <summary>칩 계열에 맞는 바탕색과 글자색을 고른다. Unity 없이 시험할 수 있다.</summary>
    public static class ChipColors
    {
        public static void Resolve(ChipTone tone, DashboardPalette palette, out Color background, out Color text)
        {
            switch (tone)
            {
                case ChipTone.Success:
                    background = palette.SuccessSoft;
                    text = palette.Success;
                    break;
                case ChipTone.Warning:
                    background = palette.WarningSoft;
                    text = palette.Warning;
                    break;
                case ChipTone.Danger:
                    background = palette.DangerSoft;
                    text = palette.Danger;
                    break;
                case ChipTone.Accent:
                    background = palette.AccentSoft;
                    text = palette.Accent;
                    break;
                default:
                    background = palette.Surface;
                    text = palette.InkSoft;
                    break;
            }
        }
    }
}
