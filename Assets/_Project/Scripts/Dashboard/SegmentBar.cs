using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>진행 막대 한 칸의 상태.</summary>
    public enum SegmentTone
    {
        Pending,
        Active,
        Done,
        Skipped,
    }

    /// <summary>
    /// 칸으로 나뉜 진행 막대(예: 키트 슬롯 4칸). 칸마다 대기, 진행 중, 완료, 건너뜀을 색으로 보여 준다.
    /// 칸이 minimumCount보다 적으면 모자란 만큼 대기 칸으로 채워서, 아직 만드는 키트가 없을 때도 빈 막대가 보이게 한다.
    /// </summary>
    public static class SegmentBar
    {
        private const float Height = 8f;
        private const float Gap = 6f;
        private const float OutlineWidth = 1.5f;

        public static void Draw(IReadOnlyList<SegmentTone> tones, int minimumCount)
        {
            DashboardPalette palette = DashboardPalette.Default;
            int count = Mathf.Max(tones != null ? tones.Count : 0, minimumCount);
            Rect row = GUILayoutUtility.GetRect(0f, Height, GUILayout.ExpandWidth(true), GUILayout.Height(Height));
            if (count < 1)
            {
                return;
            }

            float width = (row.width - Gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                SegmentTone tone = tones != null && i < tones.Count ? tones[i] : SegmentTone.Pending;
                var segment = new Rect(row.x + i * (width + Gap), row.y, width, Height);
                SegmentLook look = SegmentLook.For(tone, palette);
                DashboardDraw.FillRounded(segment, look.Fill, Height * 0.5f);
                if (look.HasOutline)
                {
                    DashboardDraw.StrokeRounded(segment, look.Outline, OutlineWidth, Height * 0.5f);
                }
            }
        }
    }
}
