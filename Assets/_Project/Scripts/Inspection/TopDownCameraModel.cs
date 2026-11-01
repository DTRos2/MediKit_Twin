using System;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 박스 바로 위에서 내려다보는 검사 카메라의 투영 계산. 합성 데이터 생성기와 검사 구역이 같은 값을 쓰도록
    /// 카메라 배치(높이, 시야각, 해상도)를 한 곳에 두고, 슬롯 위치가 이미지의 어디에 찍히는지를 계산한다.
    /// 좌표계는 박스 기준 로컬이다. 카메라는 박스 원점 위 (0, 높이, 0)에서 아래(-Y)를 보며, 이미지의 오른쪽이 +X, 위쪽이 +Z이다.
    /// 픽셀 좌표의 원점은 이미지 왼쪽 아래이다(Texture2D와 같다).
    /// </summary>
    public sealed class TopDownCameraModel
    {
        private readonly float _focalLengthPixels;

        public TopDownCameraModel(float heightAboveBoxOrigin, float verticalFovDegrees, int imageWidth, int imageHeight)
        {
            if (heightAboveBoxOrigin <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(heightAboveBoxOrigin), "카메라 높이는 0보다 커야 한다.");
            }

            if (verticalFovDegrees <= 0f || verticalFovDegrees >= 170f)
            {
                throw new ArgumentOutOfRangeException(nameof(verticalFovDegrees), "시야각은 0 초과 170 미만이어야 한다.");
            }

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(imageWidth), "이미지 크기는 0보다 커야 한다.");
            }

            HeightAboveBoxOrigin = heightAboveBoxOrigin;
            VerticalFovDegrees = verticalFovDegrees;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            _focalLengthPixels = (imageHeight * 0.5f) / Mathf.Tan(verticalFovDegrees * 0.5f * Mathf.Deg2Rad);
        }

        public float HeightAboveBoxOrigin { get; }

        public float VerticalFovDegrees { get; }

        public int ImageWidth { get; }

        public int ImageHeight { get; }

        /// <summary>박스 로컬 좌표의 점이 이미지에서 찍히는 픽셀 좌표(실수, 왼쪽 아래 원점). 카메라와 같은 높이 이상이면 false이다.</summary>
        public bool TryProject(Vector3 boxLocalPoint, out Vector2 pixel)
        {
            float depth = HeightAboveBoxOrigin - boxLocalPoint.y;
            if (depth <= 0f)
            {
                pixel = default;
                return false;
            }

            float scale = _focalLengthPixels / depth;
            pixel = new Vector2(
                ImageWidth * 0.5f + boxLocalPoint.x * scale,
                ImageHeight * 0.5f + boxLocalPoint.z * scale);
            return true;
        }

        /// <summary>카메라에서 깊이가 box 로컬 높이 y인 평면까지일 때 1m가 차지하는 픽셀 수.</summary>
        public float PixelsPerMeterAt(float boxLocalY)
        {
            float depth = HeightAboveBoxOrigin - boxLocalY;
            return depth > 0f ? _focalLengthPixels / depth : 0f;
        }

        /// <summary>
        /// 슬롯 중심(박스 로컬 좌표)에서 가로(X) x 세로(Z)가 sizeMeters인 영역을 이미지에서 잘라 낼 픽셀 사각형.
        /// 사각형이 이미지 밖으로 나가면 false이다. 슬롯 위치를 알 수 없는 것과는 다르므로 호출하는 쪽이 판정 불가로 다룬다.
        /// </summary>
        public bool TryGetCropRect(Vector3 slotCenterLocal, Vector2 sizeMeters, out RectInt rect)
        {
            rect = default;
            if (sizeMeters.x <= 0f || sizeMeters.y <= 0f || !TryProject(slotCenterLocal, out Vector2 center))
            {
                return false;
            }

            float pixelsPerMeter = PixelsPerMeterAt(slotCenterLocal.y);
            int width = Mathf.Max(1, Mathf.RoundToInt(sizeMeters.x * pixelsPerMeter));
            int height = Mathf.Max(1, Mathf.RoundToInt(sizeMeters.y * pixelsPerMeter));
            int x = Mathf.RoundToInt(center.x - width * 0.5f);
            int y = Mathf.RoundToInt(center.y - height * 0.5f);

            if (x < 0 || y < 0 || x + width > ImageWidth || y + height > ImageHeight)
            {
                return false;
            }

            rect = new RectInt(x, y, width, height);
            return true;
        }
    }
}
