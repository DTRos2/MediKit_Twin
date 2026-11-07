using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 충돌체들이 차지하는 공간을 어떤 기준 자세(보통 도구 끝점)의 좌표로 잰다. 도구 링크의 몸통 크기를 알아야 카메라를 몸통 옆면에 붙일 수 있다.
    /// 충돌체마다 로컬 경계 상자(ColliderLocalBox)의 꼭짓점 8개를 기준 좌표로 옮겨 모두 감싼다. 회전이나 크기가 있어도 맞는다.
    /// </summary>
    public static class ToolBodyBounds
    {
        /// <param name="colliders">잴 충돌체. 트리거와 모양을 모르는 것은 건너뛴다.</param>
        /// <param name="frame">이 자세를 기준으로 좌표를 낸다(월드 자세).</param>
        /// <returns>잴 수 있는 충돌체가 하나도 없으면 false.</returns>
        public static bool TryMeasure(IEnumerable<Collider> colliders, Pose frame, out Bounds bounds)
        {
            bool any = false;
            bounds = default;
            Quaternion inverse = Quaternion.Inverse(frame.rotation);

            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger || !ColliderLocalBox.TryGet(collider, out Vector3 center, out Vector3 size))
                {
                    continue;
                }

                Vector3 half = size * 0.5f;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 local = center + new Vector3(
                        (corner & 1) == 0 ? -half.x : half.x,
                        (corner & 2) == 0 ? -half.y : half.y,
                        (corner & 4) == 0 ? -half.z : half.z);
                    Vector3 inFrame = inverse * (collider.transform.TransformPoint(local) - frame.position);

                    if (!any)
                    {
                        bounds = new Bounds(inFrame, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(inFrame);
                    }
                }
            }

            return any;
        }
    }
}
