using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 위젯 사이 간격. 패널마다 GUILayout.Space(6f)처럼 숫자를 박아 두지 않고 크기에 이름을 붙여, 간격을 바꿀 때 DashboardMetrics 한 곳만 고친다.
    /// 세로로 쌓는 그룹에서는 위아래 간격, 가로로 늘어놓는 그룹에서는 좌우 간격이 된다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class DashboardSpacing
    {
        /// <summary>같은 묶음 안의 줄 사이(좁은 간격).</summary>
        public static void Row()
        {
            GUILayout.Space(DashboardMetrics.RowGap);
        }

        /// <summary>서로 다른 덩어리 사이.</summary>
        public static void Block()
        {
            GUILayout.Space(DashboardMetrics.BlockGap);
        }

        /// <summary>구간과 구간 사이(넓은 간격).</summary>
        public static void Section()
        {
            GUILayout.Space(DashboardMetrics.SectionGap);
        }
    }
}
