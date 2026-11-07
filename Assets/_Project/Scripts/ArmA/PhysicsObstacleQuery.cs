using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물리 엔진의 OverlapBox로 장애물과 겹치는지 묻는다. 로봇 자신, 쥔 물건, 베이스에 붙은 것처럼 장애물로 보지 않을 충돌체는 주입받는다.
    /// 트리거 충돌체는 장애물로 보지 않는다.
    /// </summary>
    public sealed class PhysicsObstacleQuery : IObstacleQuery
    {
        private const int HitBufferSize = 32;

        private readonly int _layerMask;
        private readonly Predicate<Collider> _ignore;
        private readonly Collider[] _hits = new Collider[HitBufferSize];

        /// <param name="layerMask">장애물로 볼 레이어.</param>
        /// <param name="ignore">장애물로 보지 않을 충돌체. 없으면 null.</param>
        public PhysicsObstacleQuery(int layerMask, Predicate<Collider> ignore)
        {
            _layerMask = layerMask;
            _ignore = ignore;
        }

        public bool Overlaps(Vector3 center, Vector3 halfExtents, Quaternion rotation, out string obstacleName)
        {
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _hits, rotation, _layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null || (_ignore != null && _ignore(hit)))
                {
                    continue;
                }

                obstacleName = ColliderLabel.Of(hit);
                return true;
            }

            obstacleName = string.Empty;
            return false;
        }
    }
}
