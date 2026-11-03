using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>테두리만 있는 태그(READY, CAMERA …)의 색 계열.</summary>
    public enum TagTone
    {
        Neutral,
        Success,
        Accent,
        Violet,
        Warning,
        Danger,
    }

    /// <summary>태그 계열에 맞는 테두리색과 글자색을 고른다. Unity 없이 시험할 수 있다.</summary>
    public static class TagColors
    {
        public static void Resolve(TagTone tone, DashboardPalette palette, out Color border, out Color text)
        {
            switch (tone)
            {
                case TagTone.Success:
                    border = palette.Success;
                    text = palette.Success;
                    break;
                case TagTone.Accent:
                    border = palette.AccentThumb;
                    text = palette.Accent;
                    break;
                case TagTone.Violet:
                    border = palette.KitBl;
                    text = palette.KitBlInk;
                    break;
                case TagTone.Warning:
                    border = palette.KitEm;
                    text = palette.Warning;
                    break;
                case TagTone.Danger:
                    border = palette.Danger;
                    text = palette.Danger;
                    break;
                default:
                    border = palette.Faint;
                    text = palette.Muted;
                    break;
            }
        }
    }
}
