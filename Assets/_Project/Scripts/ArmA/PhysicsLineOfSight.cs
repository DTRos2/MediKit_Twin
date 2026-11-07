using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 두 점 사이가 물리 충돌체에 가려지는지 본다. 로봇 자신과 쥔 물건처럼 무시할 충돌체는 주입받는다.
    /// 물건 중심 바로 앞(EndMarginM)에서 끝내, 물건이 놓인 받침이나 칸 바닥에 가려진 것으로 잘못 보지 않게 한다.
    /// </summary>
    public sealed class PhysicsLineOfSight
    {
        private const float EndMarginM = 0.01f;
        private const int HitBufferSize = 32;

        private readonly int _layerMask;
        private readonly Predicate<Collider> _ignore;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        /// <param name="layerMask">가림을 일으킬 수 있는 레이어.</param>
        /// <param name="ignore">가림으로 치지 않을 충돌체(로봇 자신 등). 없으면 null.</param>
        public PhysicsLineOfSight(int layerMask, Predicate<Collider> ignore)
        {
            _layerMask = layerMask;
            _ignore = ignore;
        }

        public bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            float distance = offset.magnitude - EndMarginM;
            if (distance <= 0f)
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(from, offset.normalized, _hits, distance, _layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i].collider;
                if (hit != null && (_ignore == null || !_ignore(hit)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
