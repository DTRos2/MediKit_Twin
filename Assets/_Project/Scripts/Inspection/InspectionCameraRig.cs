using MediKitTwin.Recipes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 검사 카메라. 검사 구역 위에 고정해 둔 카메라가 박스를 위에서 아래로 한 번 찍고 슬롯 크롭을 돌려준다. (FR-07)
    /// 카메라의 높이, 시야각, 해상도는 합성 데이터 생성기와 같은 InspectionCameraSettings에서 읽는다.
    /// 카메라는 박스 중심 바로 위 Settings의 높이에 두는 것을 전제로 한다. 박스가 조금 어긋나게 서는 경우는
    /// 학습 데이터의 위치 오차와 회전 랜덤화가 흡수한다.
    /// </summary>
    public sealed class InspectionCameraRig : MonoBehaviour
    {
        // 카메라가 박스 위에 설정대로 놓였는지 볼 때 허용하는 오차. 넘으면 크롭이 슬롯에서 어긋난다.
        private const float HeightToleranceMeters = 0.02f;
        private const float LateralToleranceMeters = 0.03f;

        [Tooltip("합성 데이터 생성기와 같은 검사 카메라 설정.")]
        [SerializeField] private InspectionCameraSettingsSO _settings;

        [Tooltip("카메라가 매달린 자리. 비워 두면 이 오브젝트다. 박스 중심 위 Settings의 높이에 둔다. 이미지의 위쪽은 찍을 때마다 박스의 +Z에 맞추고, 박스가 서 있지 않을 때만 이 자리의 Y축 회전을 쓴다.")]
        [SerializeField] private Transform _mount;

        private Camera _camera;
        private RenderTexture _target;
        private bool _warnedAlignment;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"{name}: Settings(InspectionCameraSettings)를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            if (_mount == null)
            {
                _mount = transform;
            }

            _target = new RenderTexture(_settings.ImageWidth, _settings.ImageHeight, 24, RenderTextureFormat.ARGB32);

            var cameraObject = new GameObject("InspectionCamera");
            // 마운트의 스케일을 물려받지 않도록 자식으로 두지 않는다. 위치와 방향은 찍을 때마다 마운트에서 읽는다.
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = InspectionBackground.Color;
            _camera.orthographic = false;
            _camera.fieldOfView = _settings.VerticalFovDegrees;
            _camera.aspect = (float)_settings.ImageWidth / _settings.ImageHeight;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 5f;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.targetTexture = _target;

            // 학습 이미지에는 그림자가 없으므로 이 카메라에서만 그림자를 끈다. 씬의 다른 카메라 화면은 그대로이다.
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false;
        }

        private void OnDestroy()
        {
            if (_camera != null)
            {
                Destroy(_camera.gameObject);
            }

            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }
        }

        /// <summary>
        /// 그리기 장치가 있어 카메라가 실제로 그릴 수 있으면 true. 그래픽 없이 도는 배치 실행(-nographics)에서는 false이다.
        /// 그리지 못하는데 찍으면 회색 빈 이미지가 나오고, CNN은 그것을 전부 빈 슬롯으로 판정해 정상 키트도 불량으로 만든다.
        /// 그래서 이때는 촬영을 실패로 돌려 규칙 판정(슬롯 센서)으로 대체되게 한다.
        /// </summary>
        public static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        /// <summary>고정된 카메라로 박스를 찍은 전체 이미지(카메라 설정의 해상도). 호출한 쪽이 Texture를 지운다.</summary>
        public bool TryCapture(Transform box, out Texture2D image)
        {
            image = null;
            if (!enabled || _camera == null || box == null || !CanRender)
            {
                return false;
            }

            // 학습 때와 같은 방향: 아래를 보고 이미지의 위쪽이 박스의 +Z이다. 마운트를 기울여도 항상 수직으로 내려다본다.
            // 박스가 컨베이어 코너를 지나며 돌아 들어오므로, 카메라를 마운트 방향에 고정하지 않고 찍을 때마다 박스의 +Z에 맞춘다.
            Vector3 mountUp = Quaternion.Euler(0f, _mount.eulerAngles.y, 0f) * Vector3.forward;
            Vector3 up = BoxImageUp.From(box.forward, mountUp);
            _camera.transform.SetPositionAndRotation(_mount.position, Quaternion.LookRotation(Vector3.down, up));
            WarnIfMisaligned(box);

            _camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            image = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            return true;
        }

        /// <summary>박스를 찍어 레시피의 슬롯 위치별로 자른다. 찍지 못했거나 크롭이 이미지 밖으로 나가면 false이다.</summary>
        public bool TryCaptureCrops(Transform box, RecipeSO recipe, out SlotCropSet crops, out Texture2D fullImage)
        {
            crops = null;
            if (!TryCapture(box, out fullImage))
            {
                return false;
            }

            return SlotCropper.TryCrop(fullImage, recipe, _settings, out crops);
        }

        // 카메라가 박스 중심 위 설정 높이에 없거나 방향이 다르면 크롭이 슬롯에서 어긋난다. 한 번만 알린다.
        private void WarnIfMisaligned(Transform box)
        {
            if (_warnedAlignment)
            {
                return;
            }

            Vector3 offset = _camera.transform.position - box.position;
            float height = offset.y;
            float lateral = new Vector2(offset.x, offset.z).magnitude;

            // 방향은 따지지 않는다. 찍을 때마다 카메라를 박스의 +Z에 맞추므로 박스가 어느 쪽으로 돌아 와도 크롭은 어긋나지 않는다.
            bool heightOff = Mathf.Abs(height - _settings.HeightAboveBox) > HeightToleranceMeters;
            bool lateralOff = lateral > LateralToleranceMeters;
            if (!heightOff && !lateralOff)
            {
                return;
            }

            _warnedAlignment = true;
            Debug.LogWarning(
                $"{name}: 검사 카메라가 박스 위 설정 위치에 있지 않아 슬롯 크롭이 어긋난다. " +
                $"박스 중심 위 높이 {height:F3}m(설정 {_settings.HeightAboveBox:F3}m), 수평 오차 {lateral:F3}m.", this);
        }
    }
}
