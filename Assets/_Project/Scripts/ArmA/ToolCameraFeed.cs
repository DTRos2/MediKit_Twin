using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇 도구 링크에 다는 카메라. 카메라 자세와 시야 모델, 가장 최근 인식 결과를 알려 준다(IToolCameraView).
    /// 설정(RenderImage, 기본 켬)이 켜져 있을 때만 화면을 RenderTexture로 그린다. 꺼져 있으면 화면 없이 자세와 시야만 쓴다.
    /// 도구 링크에는 목표 자세 오브젝트만 달고, 카메라는 그 자세를 조금 늦게 부드럽게 따라간다(ToolCameraSettings.SmoothingSeconds).
    /// 팔이 물리 스텝마다 끊겨 움직이고 손끝이 잔떨림해도 화면이 흔들리지 않게 하기 위해서다. CameraPose도 걸러진 자세라 화면과 인식 표식이 같은 자세를 쓴다.
    /// 다만 물건이 시야에 들어오는지 가리는 인식은 늦지 않은 손의 실제 자세(SensorPose)로 한다.
    /// 어디에 달지는 모른다(ToolCameraMounter가 정해서 넘긴다).
    /// 화면을 어디에 어떻게 보여 줄지는 모른다(화면 패널이 IToolCameraView만 읽는다).
    /// </summary>
    public sealed class ToolCameraFeed : MonoBehaviour, IToolCameraView
    {
        private Transform _target;
        private float _smoothingSeconds;
        private Camera _camera;
        private RenderTexture _texture;
        private PinholeCamera _model;
        private bool _isMounted;
        private RendererMuter _bodyMuter;
        private GpuDrivenRenderingOptOut _gpuDrivenOptOut;
        private ItemObservation _lastObservation = ItemObservation.None;

        /// <summary>카메라가 달렸으면 true. 화면을 그리지 않는 설정이면 Image는 null이어도 준비된 것이다(자세와 시야만 쓴다).</summary>
        public bool IsReady => _isMounted;

        public Texture Image => _texture;

        public Pose CameraPose => new Pose(transform.position, transform.rotation);

        // 손에 붙은 목표 자세가 걸러지지 않은 실제 자세다. 카메라가 이미 정리돼 목표가 없으면 카메라 자세를 대신 준다.
        public Pose SensorPose => _target != null ? new Pose(_target.position, _target.rotation) : CameraPose;

        public PinholeCamera Model => _model;

        public ItemObservation LastObservation => _lastObservation;

        public int ObservationCount { get; private set; }

        public int TargetTakenCount { get; private set; }

        /// <summary>카메라 화면에서 숨기려고 GPU Resident Drawer에서 뺀 렌더러의 수(알림용).</summary>
        public int GpuDrivenOptOutCount => _gpuDrivenOptOut != null ? _gpuDrivenOptOut.AppliedCount : 0;

        /// <summary>
        /// 도구 링크 아래에 카메라를 만들어 단다. 도구 끝점의 월드 자세(tcp)를 기준으로 localToTcp(도구 끝점 좌표)의 자리에 놓는다.
        /// hiddenFromCamera가 있으면 이 카메라가 그릴 때만 그 렌더러들을 그리지 않는다(로봇 몸이 화면을 가리지 않게).
        /// </summary>
        public static ToolCameraFeed Mount(Transform toolLink, Pose tcp, Pose localToTcp, ToolCameraSettings settings, IEnumerable<Renderer> hiddenFromCamera = null)
        {
            Vector3 position = tcp.position + tcp.rotation * localToTcp.position;
            Quaternion rotation = tcp.rotation * localToTcp.rotation;

            // 손에 붙은 목표 자세. 팔 물리는 물리 스텝마다 움직이고 손끝이 잔떨림하므로, 카메라는 여기에 직접 붙이지 않고 따라가게 한다.
            var target = new GameObject("ToolCameraTarget");
            target.transform.SetParent(toolLink, false);
            target.transform.SetPositionAndRotation(position, rotation);

            var host = new GameObject("ToolCamera");
            host.transform.SetPositionAndRotation(position, rotation);

            var feed = host.AddComponent<ToolCameraFeed>();
            feed._target = target.transform;
            feed._smoothingSeconds = settings.SmoothingSeconds;
            feed.Initialize(settings);

            // 화면을 그리지 않으면 보이는 본체도, 숨길 몸도 필요 없다. 자세와 시야만 남는다.
            if (!settings.RenderImage)
            {
                return feed;
            }

            // 카메라 본체는 화면에 보이되, 카메라 자신의 화면에는 나오지 않게 항상 뺀다.
            var hidden = new List<Renderer>();
            if (hiddenFromCamera != null)
            {
                hidden.AddRange(hiddenFromCamera);
            }

            if (settings.ShowCameraBody)
            {
                hidden.AddRange(ToolCameraHousing.Attach(host.transform));
            }

            feed._bodyMuter = new RendererMuter(hidden);

            // 숨길 렌더러는 GPU Resident Drawer에서 뺀다. Drawer는 켜짐·꺼짐을 프레임 끝에서야 읽어, 그리는 순간에만 끄는 것이 먹히지 않기 때문이다.
            feed._gpuDrivenOptOut = new GpuDrivenRenderingOptOut(hidden);
            feed._gpuDrivenOptOut.Apply();
            return feed;
        }

        /// <summary>
        /// 지금 화면을 한 번 그린다. 로봇 몸(숨길 렌더러)은 이 순간에만 끄고 곧바로 되돌린다.
        /// 렌더 파이프라인의 알림에 기대지 않고 끄고, 그리고, 되돌리는 순서를 여기서 정하므로 어떤 파이프라인에서도 몸이 화면에 나오지 않는다
        /// (GPU Resident Drawer는 켜짐·꺼짐을 프레임 끝에야 읽으므로, 숨길 렌더러는 Mount에서 Drawer에서 빼 둔다).
        /// 메인 화면은 다른 카메라가 그리므로 로봇이 그대로 보인다.
        /// </summary>
        public void RenderNow()
        {
            if (_camera == null || _texture == null)
            {
                return;
            }

            _bodyMuter?.Mute();
            try
            {
                _camera.Render();
            }
            finally
            {
                _bodyMuter?.Restore();
            }
        }

        /// <summary>가장 최근에 물건을 찾아본 결과를 남긴다. 화면 패널이 이것으로 표시를 겹쳐 그린다.</summary>
        public void Record(ItemObservation observation)
        {
            _lastObservation = observation;
            ObservationCount++;
        }

        /// <summary>노리던 물건을 쥐었다고 남긴다. 표시를 그리는 쪽이 TargetTakenCount가 늘어난 것으로 알아채고 표시를 거둔다.</summary>
        public void MarkTargetTaken()
        {
            TargetTakenCount++;
        }

        private void Initialize(ToolCameraSettings settings)
        {
            if (settings.RenderImage)
            {
                CreateRenderedCamera(settings);
            }

            _model = new PinholeCamera(settings.VerticalFovDeg, (float)settings.Width / settings.Height, settings.NearM, settings.FarM);
            _isMounted = true;
        }

        private void CreateRenderedCamera(ToolCameraSettings settings)
        {
            _texture = new RenderTexture(settings.Width, settings.Height, 16, RenderTextureFormat.ARGB32)
            {
                name = "ToolCameraTexture"
            };
            _texture.Create();

            _camera = gameObject.AddComponent<Camera>();
            _camera.fieldOfView = settings.VerticalFovDeg;
            _camera.nearClipPlane = settings.NearM;
            _camera.farClipPlane = settings.FarM;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            _camera.targetTexture = _texture;
            // 자동으로 그리지 않고 RenderNow가 그린다(로봇 몸을 끄고 그리기 위해).
            _camera.enabled = false;
        }

        // 팔은 물리 단계에서 움직이므로, 그 결과가 반영된 뒤에 따라가고 그린다.
        private void LateUpdate()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            Follow(Time.unscaledDeltaTime);
            RenderNow();
        }

        // 지수 필터로 목표 자세를 따라간다. 프레임 시간에 맞춰 계수를 정하므로 프레임레이트가 달라도 같은 지연이다.
        private void Follow(float deltaSeconds)
        {
            if (_smoothingSeconds <= 0f)
            {
                transform.SetPositionAndRotation(_target.position, _target.rotation);
                return;
            }

            float blend = 1f - Mathf.Exp(-deltaSeconds / _smoothingSeconds);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, _target.position, blend),
                Quaternion.Slerp(transform.rotation, _target.rotation, blend));
        }

        private void OnDisable()
        {
            _bodyMuter?.Restore();
        }

        private void OnDestroy()
        {
            if (_target != null)
            {
                Destroy(_target.gameObject);
            }

            _gpuDrivenOptOut?.Restore();

            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
