using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 핀홀 카메라 모델. 월드의 한 점이 카메라 화면 어디에 보이는지, 아예 화면 밖인지를 계산한다.
    /// 카메라는 +Z(forward)를 보고 +X가 오른쪽, +Y가 위쪽이다(Unity 카메라와 같다). 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public readonly struct PinholeCamera
    {
        public PinholeCamera(float verticalFovDeg, float aspect, float nearM, float farM)
        {
            float near = Mathf.Max(0.001f, nearM);
            VerticalFovDeg = Mathf.Clamp(verticalFovDeg, 1f, 170f);
            Aspect = Mathf.Max(0.1f, aspect);
            NearM = near;
            FarM = Mathf.Max(near + 0.001f, farM);
        }

        /// <summary>세로 시야각(도).</summary>
        public float VerticalFovDeg { get; }

        /// <summary>가로 / 세로.</summary>
        public float Aspect { get; }

        public float NearM { get; }

        public float FarM { get; }

        /// <summary>
        /// worldPoint가 카메라 앞 near~far 사이에 있고 화면 안이면 true.
        /// viewport는 화면 안의 위치(0~1, 왼쪽 아래가 0,0), depthM은 카메라 앞쪽으로 잰 거리다. 화면 밖이어도 앞에 있으면 viewport를 채운다.
        /// </summary>
        public bool TryProject(Pose cameraPose, Vector3 worldPoint, out Vector2 viewport, out float depthM)
        {
            Vector3 local = Quaternion.Inverse(cameraPose.rotation) * (worldPoint - cameraPose.position);
            depthM = local.z;
            viewport = default;

            if (depthM <= 0f)
            {
                return false;
            }

            float halfHeight = depthM * Mathf.Tan(VerticalFovDeg * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * Aspect;
            float x = local.x / halfWidth;
            float y = local.y / halfHeight;
            viewport = new Vector2(0.5f + 0.5f * x, 0.5f + 0.5f * y);

            bool isInDepthRange = depthM >= NearM && depthM <= FarM;
            return isInDepthRange && Mathf.Abs(x) <= 1f && Mathf.Abs(y) <= 1f;
        }
    }
}
