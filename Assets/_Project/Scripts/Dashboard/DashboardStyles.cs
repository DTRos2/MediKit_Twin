using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 글자 모양(GUIStyle) 가게. 쓰임새(TextRole), 색, 정렬, 줄바꿈이 같으면 같은 모양을 한 번만 만들어 돌려준다.
    /// 프레임마다 스타일을 새로 만들면 쓰레기가 쌓이므로 처음 한 번만 만든다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class DashboardStyles
    {
        private static readonly Dictionary<TextKey, GUIStyle> Cache = new Dictionary<TextKey, GUIStyle>();

        public static GUIStyle Text(TextRole role, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = false)
        {
            var key = new TextKey(role, color, anchor, wrap);
            if (Cache.TryGetValue(key, out GUIStyle cached))
            {
                return cached;
            }

            GUIStyle style = Create(role, color, anchor, wrap);
            Cache[key] = style;
            return style;
        }

        /// <summary>글자의 가로 길이(px).</summary>
        public static float WidthOf(string text, GUIStyle style)
        {
            return style.CalcSize(new GUIContent(text)).x;
        }

        private static GUIStyle Create(TextRole role, Color color, TextAnchor anchor, bool wrap)
        {
            var style = new GUIStyle
            {
                alignment = anchor,
                wordWrap = wrap,
                clipping = wrap ? TextClipping.Overflow : TextClipping.Clip,
                richText = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;

            switch (role)
            {
                case TextRole.Title:
                    Apply(style, DashboardFonts.Sans, DashboardMetrics.TitleFontSize, FontStyle.Bold);
                    break;
                case TextRole.Subtitle:
                    Apply(style, DashboardFonts.Mono, DashboardMetrics.SmallFontSize - 1, FontStyle.Normal);
                    break;
                case TextRole.BodyBold:
                    Apply(style, DashboardFonts.Sans, DashboardMetrics.BodyFontSize, FontStyle.Bold);
                    break;
                case TextRole.Small:
                    Apply(style, DashboardFonts.Sans, DashboardMetrics.SmallFontSize, FontStyle.Normal);
                    break;
                case TextRole.Mono:
                    Apply(style, DashboardFonts.Mono, DashboardMetrics.MonoFontSize, FontStyle.Normal);
                    break;
                case TextRole.MonoBold:
                    Apply(style, DashboardFonts.Mono, DashboardMetrics.MonoFontSize, FontStyle.Bold);
                    break;
                case TextRole.BigNumber:
                    Apply(style, DashboardFonts.Mono, DashboardMetrics.BigNumberFontSize, FontStyle.Bold);
                    break;
                case TextRole.State:
                    Apply(style, DashboardFonts.Sans, DashboardMetrics.StateFontSize, FontStyle.Bold);
                    break;
                default:
                    Apply(style, DashboardFonts.Sans, DashboardMetrics.BodyFontSize, FontStyle.Normal);
                    break;
            }

            return style;
        }

        private static void Apply(GUIStyle style, Font font, int size, FontStyle fontStyle)
        {
            style.font = font;
            style.fontSize = size;
            style.fontStyle = fontStyle;
        }

        private readonly struct TextKey : IEquatable<TextKey>
        {
            private readonly TextRole _role;
            private readonly Color32 _color;
            private readonly TextAnchor _anchor;
            private readonly bool _wrap;

            public TextKey(TextRole role, Color color, TextAnchor anchor, bool wrap)
            {
                _role = role;
                _color = color;
                _anchor = anchor;
                _wrap = wrap;
            }

            public bool Equals(TextKey other)
            {
                return _role == other._role
                       && _color.r == other._color.r && _color.g == other._color.g
                       && _color.b == other._color.b && _color.a == other._color.a
                       && _anchor == other._anchor && _wrap == other._wrap;
            }

            public override bool Equals(object obj)
            {
                return obj is TextKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                int hash = (int)_role;
                hash = (hash * 397) ^ (_color.r | (_color.g << 8) | (_color.b << 16) | (_color.a << 24));
                hash = (hash * 397) ^ (int)_anchor;
                return (hash * 397) ^ (_wrap ? 1 : 0);
            }
        }
    }
}
