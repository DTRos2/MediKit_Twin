using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>두 충돌체가 실제로 겹치는지와 겹침 깊이를 잰다.</summary>
    public static class ColliderPenetration
    {
        /// <summary>
        /// 겹침 깊이(m)를 재서 돌려준다. 겹치지 않으면 null이다.
        /// 모양이 허락하는 쌍(상자·구·캡슐·볼록 메시)만 잴 수 있어, 못 재는 쌍은 깊이를 알 수 없다는 뜻으로 UnknownDepth를 돌려준다.
        /// </summary>
        public static float? DepthM(Collider a, Collider b)
        {
            if (!CanMeasure(a) || !CanMeasure(b))
            {
                return UnknownDepth;
            }

            bool overlaps = Physics.ComputePenetration(
                a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out float distance);
            return overlaps ? distance : (float?)null;
        }

        /// <summary>깊이를 잴 수 없는 모양(볼록하지 않은 메시)이지만 상자 범위는 겹칠 때 돌려주는 값.</summary>
        public const float UnknownDepth = -1f;

        private static bool CanMeasure(Collider collider)
        {
            return !(collider is MeshCollider mesh) || mesh.convex;
        }
    }
}
