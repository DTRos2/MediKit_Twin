using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔을 싣고 다니는 차체의 충돌체에서 충돌 상자를 만든다. 차체 루트 아래에서 팔(armRoot) 밖에 있는 충돌체가 차체다.
    /// BaseVolumeBuilder는 팔 베이스 링크에 붙은 충돌체만 보므로, 그 옆에 붙은 차체와 받침은 서 있는 자리가 레일에 파고드는지 따질 때 빠져 있었다.
    /// 상자는 팔 베이스 링크 기준이다(팔이 차체에 고정되어 함께 움직이기 때문이다).
    /// </summary>
    public static class CarrierVolumeBuilder
    {
        /// <param name="carrierRoot">팔을 싣고 다니는 베이스의 루트. 없으면 차체가 없는 것으로 본다.</param>
        /// <param name="baseLink">팔 베이스 링크. 상자는 이 링크 기준으로 담는다.</param>
        /// <param name="armRoot">팔의 최상위. 이 아래 충돌체는 팔의 것이라 뺀다.</param>
        public static CarrierBody Build(Transform carrierRoot, Transform baseLink, Transform armRoot)
        {
            if (carrierRoot == null)
            {
                return CarrierBody.None;
            }

            var volumes = new List<LinkVolume>();
            var colliders = new HashSet<Collider>();
            float underside = float.PositiveInfinity;

            foreach (Collider collider in carrierRoot.GetComponentsInChildren<Collider>())
            {
                if (!IsCarrierPart(collider, armRoot) || !ColliderLocalBox.TryGet(collider, out Vector3 localCenter, out Vector3 localSize))
                {
                    continue;
                }

                volumes.Add(LinkVolumeBuilder.ToLinkVolume(collider, baseLink, 0, localCenter, localSize));
                colliders.Add(collider);
                underside = Mathf.Min(underside, LowestWorldY(collider.transform, localCenter, localSize));
            }

            return volumes.Count == 0 ? CarrierBody.None : new CarrierBody(volumes, colliders, underside);
        }

        // 팔의 것, 쥔 물건(도구에 붙어 따라다닌다)은 차체가 아니다. 트리거는 차체로 본다: 하역 로봇 모델은 차체 크기를 트리거 상자로만 갖고 있다.
        private static bool IsCarrierPart(Collider collider, Transform armRoot)
        {
            return !(armRoot != null && collider.transform.IsChildOf(armRoot))
                && collider.GetComponentInParent<Graspable>() == null;
        }

        // 충돌체 로컬 상자의 여덟 모서리 가운데 월드에서 가장 낮은 높이. 물리 엔진의 경계(bounds)는 변환이 반영되기 전이면 낡은 값이라 쓰지 않는다.
        private static float LowestWorldY(Transform source, Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            float lowest = float.PositiveInfinity;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(half, new Vector3(x, y, z));
                        lowest = Mathf.Min(lowest, source.TransformPoint(corner).y);
                    }
                }
            }

            return lowest;
        }
    }
}
