using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 검사 카메라 설정. 합성 데이터 생성기(FR-08)와 검사 구역의 카메라(FR-07)가 이 에셋 하나를 읽으므로
    /// 학습 이미지와 실제 판정 이미지의 시점과 해상도가 어긋나지 않는다. 고정 시점, 고정 조명이 전제이다.
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Inspection Camera Settings", fileName = "InspectionCameraSettings")]
    public sealed class InspectionCameraSettingsSO : ScriptableObject
    {
        [Header("카메라 배치 (박스 기준)")]
        [Tooltip("박스 원점(박스 중심)에서 카메라까지의 높이(m). 카메라는 박스 바로 위에서 아래를 본다.")]
        [SerializeField, Min(0.1f)] private float _heightAboveBox = 0.6f;

        [Tooltip("세로 시야각(도). 이미지의 세로가 박스의 긴 변(Z)을 따른다.")]
        [SerializeField, Range(5f, 90f)] private float _verticalFovDegrees = 30f;

        [Header("이미지")]
        [SerializeField, Min(16)] private int _imageWidth = 256;
        [SerializeField, Min(16)] private int _imageHeight = 512;

        [Header("슬롯 크롭")]
        [Tooltip("슬롯 하나를 잘라 낼 실제 크기(m): 가로(X), 세로(Z). 슬롯 간격 안에 들어가게 잡아 이웃 슬롯의 구성품이 섞이지 않게 한다.")]
        [SerializeField] private Vector2 _cropSizeMeters = new Vector2(0.0976f, 0.0546f);

        [Tooltip("크롭 크기를 픽셀로 바꿀 때 기준이 되는 박스 로컬 높이(m). 구성품이 놓이는 높이 근처로 둔다.")]
        [SerializeField] private float _cropPlaneLocalY = 0f;

        public float HeightAboveBox => _heightAboveBox;

        public float VerticalFovDegrees => _verticalFovDegrees;

        public int ImageWidth => _imageWidth;

        public int ImageHeight => _imageHeight;

        public Vector2 CropSizeMeters => _cropSizeMeters;

        public float CropPlaneLocalY => _cropPlaneLocalY;

        public TopDownCameraModel CreateModel()
        {
            return new TopDownCameraModel(_heightAboveBox, _verticalFovDegrees, _imageWidth, _imageHeight);
        }

        private void OnValidate()
        {
            if (_cropSizeMeters.x <= 0f || _cropSizeMeters.y <= 0f)
            {
                Debug.LogWarning($"{name}: 크롭 크기는 0보다 커야 한다.", this);
            }
        }
    }
}
