using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 패널 안의 작은 스크롤 영역(주문 목록, 경고 목록, 로그). 가로 막대는 숨기고 세로 막대만 대시보드 스킨의 가는 막대로 보여 준다.
    /// 호출 위치마다 같은 BeginScrollView 인자를 복사하지 않으려고 한곳에 둔다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class DashboardScroll
    {
        /// <summary>정해 준 높이의 스크롤 영역을 열고 새 스크롤 위치를 돌려준다. 다 그린 뒤 End를 부른다.</summary>
        public static Vector2 Begin(Vector2 position, float height)
        {
            return GUILayout.BeginScrollView(
                position, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(height));
        }

        public static void End()
        {
            GUILayout.EndScrollView();
        }
    }
}
