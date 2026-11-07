using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 카메라 화면을 정해진 칸에 늘리지 않고 가운데를 잘라 채우는 계산. 칸이 그림보다 가로로 길면 위아래를, 세로로 길면 좌우를 잘라
    /// 그림이 찌그러지지 않게 한다. 그림 속 위치(0~1)를 잘린 칸 안의 화면 좌표로 바꾸는 일도 맡아, 물건 표시가 보이는 부분에 정확히 놓인다.
    /// 그림 좌표는 왼쪽 아래가 (0, 0)이다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class CameraImageFit
    {
        /// <summary>칸에 보여 줄 그림의 부분(그림 좌표). 칸이나 그림의 크기가 0 이하이면 그림 전체다.</summary>
        public static Rect VisibleUv(float imageWidth, float imageHeight, float areaWidth, float areaHeight)
        {
            if (imageWidth <= 0f || imageHeight <= 0f || areaWidth <= 0f || areaHeight <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float imageAspect = imageWidth / imageHeight;
            float areaAspect = areaWidth / areaHeight;
            if (areaAspect > imageAspect)
            {
                float visibleHeight = imageAspect / areaAspect;
                return new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }

            float visibleWidth = areaAspect / imageAspect;
            return new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
        }

        /// <summary>그림 속 위치가 보이는 부분 안이면 칸 안의 화면 좌표를 돌려주고 true. 잘려서 안 보이는 자리면 false이다.</summary>
        public static bool TryToGuiPoint(Rect area, Rect visibleUv, Vector2 viewport, out Vector2 point)
        {
            float u = (viewport.x - visibleUv.x) / visibleUv.width;
            float v = (viewport.y - visibleUv.y) / visibleUv.height;
            point = new Vector2(area.x + u * area.width, area.y + (1f - v) * area.height);
            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
        }
    }
}
