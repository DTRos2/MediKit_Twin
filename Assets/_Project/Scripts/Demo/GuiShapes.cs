using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>IMGUI로 간단한 도형을 그린다. 화면 표시들이 같은 그리기 코드를 복사하지 않게 한 곳에 둔다. OnGUI의 Repaint 때만 부른다.</summary>
    public static class GuiShapes
    {
        /// <summary>속이 빈 네모 테두리를 그린다.</summary>
        public static void DrawFrame(Rect box, Color color, float linePx)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, linePx), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.yMax - linePx, box.width, linePx), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.y, linePx, box.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.xMax - linePx, box.y, linePx, box.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
