using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스 링크(첫 관절 링크 앞, 움직이지 않는 부분)에 붙은 충돌체에서 충돌 상자(LinkVolume)를 만든다.
    /// LinkVolumeBuilder는 이 충돌체를 건너뛰므로(베이스는 팔이 움직여도 제자리라서), 팔이 아니라 "베이스가 서 있는 자리"가
    /// 장애물과 겹치는지 따지려면 따로 만들어야 한다. 상자는 팔 베이스 링크 기준이라 베이스가 어디에 서든 그 자세에 놓아 쓴다.
    /// </summary>
    public static class BaseVolumeBuilder
    {
        /// <param name="baseLink">팔 베이스 링크.</param>
        /// <param name="jointBodies">관절 링크. 이 아래에 붙은 충돌체는 팔의 것이므로 뺀다.</param>
        public static List<LinkVolume> Build(Transform baseLink, IReadOnlyList<ArticulationBody> jointBodies)
        {
            var volumes = new List<LinkVolume>();
            var jointLinks = new HashSet<Transform>();
            foreach (ArticulationBody body in jointBodies)
            {
                jointLinks.Add(body.transform);
            }

            foreach (Collider collider in baseLink.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger || IsUnderJointLink(collider.transform, baseLink, jointLinks))
                {
                    continue;
                }

                if (ColliderLocalBox.TryGet(collider, out Vector3 localCenter, out Vector3 localSize))
                {
                    volumes.Add(LinkVolumeBuilder.ToLinkVolume(collider, baseLink, 0, localCenter, localSize));
                }
            }

            return volumes;
        }

        // 충돌체에서 베이스 링크까지 올라가는 길에 관절 링크가 있으면 그 충돌체는 팔의 것이다.
        private static bool IsUnderJointLink(Transform start, Transform baseLink, HashSet<Transform> jointLinks)
        {
            for (Transform current = start; current != null && current != baseLink; current = current.parent)
            {
                if (jointLinks.Contains(current))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
