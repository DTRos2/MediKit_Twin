using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 대시보드 패널 밖에서 화면 위에 직접 그리는 표시(인식 표시)가 쓰는 IMGUI 글자 모양. 같은 스타일을 표시마다 새로 만들지 않고 한 번만 만든다.
    /// 패널 안의 글자 모양은 Dashboard 어셈블리의 DashboardStyles가 맡는다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class GuiLabelStyles
    {
        private static GUIStyle _centeredBold;

        /// <summary>가운데 정렬, 굵은 글자.</summary>
        public static GUIStyle CenteredBold
        {
            get
            {
                if (_centeredBold == null)
                {
                    _centeredBold = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
                }

                return _centeredBold;
            }
        }
    }
}
