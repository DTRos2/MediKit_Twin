using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 직접 그린 위젯(스위치, 버튼, 타일)의 클릭을 알아낸다. 누른 채로 칸 밖으로 나가면 취소되고, 칸 안에서 떼야 클릭이다.
    /// IMGUI의 기본 버튼은 모양을 바꾸기 어려워서, 모양은 우리가 그리고 누름 판정만 이 클래스가 맡는다.
    /// 모든 이벤트(Layout, Repaint 포함)에서 같은 횟수로 불러야 한다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class GuiClick
    {
        /// <param name="isHover">마우스가 위젯 위에 있는지. 다른 위젯을 누르고 끄는 중이면 false.</param>
        /// <param name="isPressed">이 위젯을 누르고 있는 중인지.</param>
        /// <returns>이번 이벤트에서 클릭이 끝났으면 true.</returns>
        public static bool Detect(Rect rect, out bool isHover, out bool isPressed)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;

            bool nobodyElseIsHeld = GUIUtility.hotControl == 0 || GUIUtility.hotControl == id;
            isHover = nobodyElseIsHeld && rect.Contains(current.mousePosition);
            isPressed = GUIUtility.hotControl == id;

            bool isClicked = false;
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (isHover && current.button == 0 && GUI.enabled)
                    {
                        GUIUtility.hotControl = id;
                        isPressed = true;
                        current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        isClicked = rect.Contains(current.mousePosition);
                        current.Use();
                    }

                    break;
            }

            return isClicked;
        }
    }
}
