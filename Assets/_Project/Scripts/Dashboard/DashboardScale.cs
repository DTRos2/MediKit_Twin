using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 실제 화면 크기에 맞춰 대시보드 전체를 키우고 줄이는 배율을 구한다. 기준 화면(1920×1080)보다 작은 창에서도
    /// 패널들이 서로 겹치지 않게 하려는 것이다. 가로와 세로 중 더 모자란 쪽에 맞추고, 너무 작아지거나 커지지 않게 막는다.
    /// 화면 크기를 인자로 받으므로 Unity 없이 시험할 수 있다.
    /// </summary>
    public static class DashboardScale
    {
        // 이보다 작게 줄이면 글자를 읽을 수 없다. 이 아래 화면에서는 패널이 겹치더라도 글자 크기를 지킨다.
        public const float MinScale = 0.6f;
        public const float MaxScale = 2f;

        public static float For(float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                return 1f;
            }

            float fit = Mathf.Min(screenWidth / DashboardMetrics.ReferenceWidth, screenHeight / DashboardMetrics.ReferenceHeight);
            return Mathf.Clamp(fit, MinScale, MaxScale);
        }
    }
}
