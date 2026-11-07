using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 카메라 화면 안 위치(0~1, 왼쪽 아래가 0,0)를 IMGUI 좌표(위가 0)로 바꾼다. 카메라 화면 위에 표시를 겹쳐 그리는 화면들이 같은 계산을 쓴다.
    /// 순수 계산이라 Unity 화면 없이 시험한다.
    /// </summary>
    public static class ViewportMapping
    {
        /// <param name="area">카메라 화면을 그린 IMGUI 영역.</param>
        /// <param name="viewport">화면 안 위치(0~1, 왼쪽 아래가 0,0).</param>
        public static Vector2 ToGuiPoint(Rect area, Vector2 viewport)
        {
            return new Vector2(
                area.x + viewport.x * area.width,
                area.y + (1f - viewport.y) * area.height);
        }
    }
}
