using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 씬의 로봇 링크에 붙은 충돌체에서 링크별 충돌 상자(LinkVolume)를 만든다.
    /// 충돌체가 붙은 가장 가까운 관절 링크(자신이나 조상)에 소속시킨다. 그리퍼 손가락처럼 마지막 관절 링크 아래에 달린 것은 마지막 링크 소속이 된다.
    /// 첫 관절 링크 앞(베이스 쪽)에 붙은 충돌체는 건너뛴다(베이스는 움직이지 않는다).
    /// 상자는 충돌체 모양의 경계 상자(ColliderLocalBox)라서 실제 모양보다 클 수 있다.
    /// </summary>
    public static class LinkVolumeBuilder
    {
        /// <param name="robotRoot">충돌체를 찾을 로봇의 최상위.</param>
        /// <param name="jointBodies">관절 링크(베이스에서 먼 순서). 순번이 곧 LinkVolume.LinkIndex다.</param>
        public static List<LinkVolume> Build(Transform robotRoot, IReadOnlyList<ArticulationBody> jointBodies)
        {
            var volumes = new List<LinkVolume>();
            var indexOf = new Dictionary<Transform, int>();
            for (int i = 0; i < jointBodies.Count; i++)
            {
                indexOf[jointBodies[i].transform] = i;
            }

            foreach (Collider collider in robotRoot.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }

                int linkIndex = OwnerLink(collider.transform, robotRoot, indexOf);
                if (linkIndex < 0 || !ColliderLocalBox.TryGet(collider, out Vector3 localCenter, out Vector3 localSize))
                {
                    continue;
                }

                volumes.Add(ToLinkVolume(collider, jointBodies[linkIndex].transform, linkIndex, localCenter, localSize));
            }

            return volumes;
        }

        // 충돌체에서 위로 올라가며 처음 만나는 관절 링크. 없으면 -1.
        private static int OwnerLink(Transform start, Transform root, Dictionary<Transform, int> indexOf)
        {
            for (Transform current = start; current != null; current = current.parent)
            {
                if (indexOf.TryGetValue(current, out int index))
                {
                    return index;
                }

                if (current == root)
                {
                    break;
                }
            }

            return -1;
        }

        // 충돌체 로컬 상자를 링크 오브젝트 기준 상자로 바꾼다. 링크 크기(scale)는 무시한 비율 없는 좌표다(KinematicChain과 같다).
        internal static LinkVolume ToLinkVolume(Collider collider, Transform link, int linkIndex, Vector3 localCenter, Vector3 localSize)
        {
            Transform source = collider.transform;
            Vector3 worldCenter = source.TransformPoint(localCenter);
            Quaternion inverseLink = Quaternion.Inverse(link.rotation);

            Vector3 scale = source.lossyScale;
            var half = new Vector3(
                Mathf.Abs(localSize.x * scale.x) * 0.5f,
                Mathf.Abs(localSize.y * scale.y) * 0.5f,
                Mathf.Abs(localSize.z * scale.z) * 0.5f);

            return new LinkVolume(
                linkIndex,
                inverseLink * (worldCenter - link.position),
                inverseLink * source.rotation,
                half);
        }
    }
}
