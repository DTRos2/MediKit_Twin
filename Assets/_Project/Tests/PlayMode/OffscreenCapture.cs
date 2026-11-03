#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 화면(Game View)을 쓰지 않고 카메라 한 대를 임시로 세워 RenderTexture에 그린 뒤 PNG로 저장한다.
    /// 배치 모드에서는 Game View를 열면 Unity가 죽으므로, 눈으로 확인해야 하는 것(쥐는 순간의 손가락과 물건, 뚜껑 위 라벨)은 이 방법으로 찍는다.
    /// 그래픽 장치가 있어야 하므로 -nographics 없이 돌려야 한다. 이 클래스는 찍는 일만 하고 어디를 어떻게 찍을지는 부르는 쪽이 정한다.
    /// </summary>
    public sealed class OffscreenCapture
    {
        public const int DefaultSizePixels = 640;
        public const float DefaultFieldOfViewDeg = 30f;
        public const float DefaultNearClipM = 0.01f;
        public const float DefaultFarClipM = 20f;
        private const int DepthBits = 24;
        private const int AntiAliasingSamples = 4;
        private static readonly Color Background = new Color(0.55f, 0.6f, 0.65f, 1f);

        private readonly int _sizePixels;
        private readonly float _fieldOfViewDeg;
        private readonly float _nearClipM;

        public OffscreenCapture(int sizePixels = DefaultSizePixels, float fieldOfViewDeg = DefaultFieldOfViewDeg, float nearClipM = DefaultNearClipM)
        {
            _sizePixels = sizePixels;
            _fieldOfViewDeg = fieldOfViewDeg;
            _nearClipM = nearClipM;
        }

        /// <summary>eye에서 target을 바라보며 찍어 path에 PNG로 저장한다. up은 화면의 위쪽으로 삼을 월드 방향이다. 저장했으면 true.</summary>
        public bool Save(string path, Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 forward = target - eye;
            if (forward.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            var cameraObject = new GameObject("OffscreenCapture");
            RenderTexture texture = null;
            Texture2D image = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Background;
                camera.fieldOfView = _fieldOfViewDeg;
                camera.nearClipPlane = _nearClipM;
                camera.farClipPlane = DefaultFarClipM;
                cameraObject.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(forward, up));

                texture = new RenderTexture(_sizePixels, _sizePixels, DepthBits, RenderTextureFormat.ARGB32) { antiAliasing = AntiAliasingSamples };
                camera.targetTexture = texture;
                camera.Render();

                RenderTexture.active = texture;
                image = new Texture2D(_sizePixels, _sizePixels, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, _sizePixels, _sizePixels), 0, 0);
                image.Apply();

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
                return true;
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (texture != null)
                {
                    texture.Release();
                    Object.Destroy(texture);
                }

                if (image != null)
                {
                    Object.Destroy(image);
                }

                Object.Destroy(cameraObject);
            }
        }
    }
}
#endif
