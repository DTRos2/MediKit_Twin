using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇 도구 링크에 다는 카메라. 카메라 자세와 시야 모델, 가장 최근 인식 결과를 알려 준다(IToolCameraView).
    /// 설정(RenderImage, 기본 켬)이 켜져 있을 때만 화면을 RenderTexture로 그린다. 꺼져 있으면 화면 없이 자세와 시야만 쓴다.
    /// 도구 링크의 자식으로 만들어져 팔이 움직이면 함께 움직인다. 어디에 달지는 모른다(ToolCameraMounter가 정해서 넘긴다).
    /// 화면을 어디에 어떻게 보여 줄지는 모른다(화면 패널이 IToolCameraView만 읽는다).
    /// </summary>
    public sealed class ToolCameraFeed : MonoBehaviour, IToolCameraView
    {
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

        public PinholeCamera Model => _model;

        public ItemObservation LastObservation => _lastObservation;

        public int ObservationCount { get; private set; }

        /// <summary>카메라 화면에서 숨기려고 GPU Resident Drawer에서 뺀 렌더러의 수(알림용).</summary>
        public int GpuDrivenOptOutCount => _gpuDrivenOptOut != null ? _gpuDrivenOptOut.AppliedCount : 0;

        /// <summary>
        /// 도구 링크 아래에 카메라를 만들어 단다. 도구 끝점의 월드 자세(tcp)를 기준으로 localToTcp(도구 끝점 좌표)의 자리에 놓는다.
        /// hiddenFromCamera가 있으면 이 카메라가 그릴 때만 그 렌더러들을 그리지 않는다(로봇 몸이 화면을 가리지 않게).
        /// </summary>
        public static ToolCameraFeed Mount(Transform toolLink, Pose tcp, Pose localToTcp, ToolCameraSettings settings, IEnumerable<Renderer> hiddenFromCamera = null)
        {
            var host = new GameObject("ToolCamera");
            host.transform.SetParent(toolLink, false);
            host.transform.SetPositionAndRotation(
                tcp.position + tcp.rotation * localToTcp.position,
                tcp.rotation * localToTcp.rotation);

            var feed = host.AddComponent<ToolCameraFeed>();
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

        // 팔은 물리 단계에서 움직이므로, 그 결과가 반영된 뒤에 그린다.
        private void LateUpdate()
        {
            RenderNow();
        }

        private void OnDisable()
        {
            _bodyMuter?.Restore();
        }

        private void OnDestroy()
        {
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
