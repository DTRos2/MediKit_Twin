using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 대시보드가 쓰는 도형 그리기. 둥근 네모, 둥근 테두리, 원, 고리, 점선 고리를 IMGUI로 그린다.
    /// 둥근 모서리는 GUI.DrawTexture의 모서리 반지름으로 그려서 질감 그림을 따로 만들지 않는다.
    /// 그리기 이벤트(Repaint)에서만 그리고, GUI.enabled가 꺼져 있으면 흐리게 그린다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class DashboardDraw
    {
        private const float FullCircleDegrees = 360f;

        /// <summary>속이 찬 둥근 네모.</summary>
        public static void FillRounded(Rect rect, Color color, float radius)
        {
            if (!ShouldDraw(rect))
            {
                return;
            }

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Faded(color), 0f, ClampRadius(rect, radius));
        }

        /// <summary>속이 빈 둥근 테두리(안쪽으로 widthPx만큼).</summary>
        public static void StrokeRounded(Rect rect, Color color, float widthPx, float radius)
        {
            if (!ShouldDraw(rect))
            {
                return;
            }

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Faded(color), widthPx, ClampRadius(rect, radius));
        }

        public static void FillCircle(Vector2 center, float radius, Color color)
        {
            FillRounded(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), color, radius);
        }

        public static void Ring(Vector2 center, float radius, float widthPx, Color color)
        {
            StrokeRounded(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), color, widthPx, radius);
        }

        /// <summary>작은 점을 원 둘레에 고르게 찍어 만든 점선 고리.</summary>
        public static void DashedRing(Vector2 center, float radius, float dotSizePx, int dotCount, Color color)
        {
            if (dotCount < 1 || Event.current.type != EventType.Repaint)
            {
                return;
            }

            float step = FullCircleDegrees / dotCount;
            for (int i = 0; i < dotCount; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                var position = new Vector2(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius);
                FillCircle(position, dotSizePx * 0.5f, color);
            }
        }

        /// <summary>GUI.enabled가 꺼져 있으면 투명도를 줄인 색.</summary>
        public static Color Faded(Color color)
        {
            if (!GUI.enabled)
            {
                color.a *= DashboardMetrics.DisabledAlpha;
            }

            return color;
        }

        private static bool ShouldDraw(Rect rect)
        {
            return Event.current.type == EventType.Repaint && rect.width > 0f && rect.height > 0f;
        }

        // 반지름이 짧은 변의 절반을 넘으면 모서리가 일그러지므로 절반까지만 쓴다.
        private static float ClampRadius(Rect rect, float radius)
        {
            return Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        }
    }
}
