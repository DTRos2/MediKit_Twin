using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 충돌체 하나를 충돌체 로컬 공간의 경계 상자(중심, 전체 크기)로 바꾼다. 상자는 그대로, 구와 캡슐은 감싸는 상자, 메시는 메시의 경계 상자다.
    /// 실제 모양보다 클 수 있다. 충돌 상자(LinkVolumeBuilder)와 도구 몸통 크기 재기(ToolBodyBounds)가 함께 쓴다.
    /// </summary>
    public static class ColliderLocalBox
    {
        /// <summary>모양을 알 수 없는 충돌체면 false.</summary>
        public static bool TryGet(Collider collider, out Vector3 center, out Vector3 size)
        {
            switch (collider)
            {
                case BoxCollider box:
                    center = box.center;
                    size = box.size;
                    return true;
                case SphereCollider sphere:
                    center = sphere.center;
                    size = Vector3.one * (sphere.radius * 2f);
                    return true;
                case CapsuleCollider capsule:
                    center = capsule.center;
                    size = CapsuleSize(capsule);
                    return true;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    center = mesh.sharedMesh.bounds.center;
                    size = mesh.sharedMesh.bounds.size;
                    return true;
                default:
                    center = Vector3.zero;
                    size = Vector3.zero;
                    return false;
            }
        }

        private static Vector3 CapsuleSize(CapsuleCollider capsule)
        {
            float diameter = capsule.radius * 2f;
            float length = Mathf.Max(capsule.height, diameter);
            switch (capsule.direction)
            {
                case 0: return new Vector3(length, diameter, diameter);
                case 1: return new Vector3(diameter, length, diameter);
                default: return new Vector3(diameter, diameter, length);
            }
        }
    }
}
