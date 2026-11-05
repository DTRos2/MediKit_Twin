using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔을 싣고 다니는 차체(바퀴, 궤도, 받침)의 충돌 모양. 상자는 팔 베이스 링크 기준이라, 팔 베이스가 서 있을 자세만 알면 차체가 차지할 공간을 안다.
    /// 차체의 충돌체는 장애물이 아니므로 따로 기억해 두고, 밑면 높이는 차체가 올라선 바닥을 알아보는 데 쓴다.
    /// </summary>
    public sealed class CarrierBody
    {
        /// <summary>차체가 없는 경우. 상자가 없고, 밑면은 어디에도 닿지 않는 값이라 바닥으로 보는 것도 없다.</summary>
        public static readonly CarrierBody None = new CarrierBody(new List<LinkVolume>(), new HashSet<Collider>(), float.NegativeInfinity);

        private readonly HashSet<Collider> _colliders;

        /// <param name="volumes">팔 베이스 링크 기준 차체 충돌 상자.</param>
        /// <param name="colliders">상자를 만든 충돌체. 장애물로 보지 않는다.</param>
        /// <param name="undersideY">차체에서 가장 낮은 면의 월드 높이(m).</param>
        public CarrierBody(IReadOnlyList<LinkVolume> volumes, HashSet<Collider> colliders, float undersideY)
        {
            Volumes = volumes;
            _colliders = colliders;
            UndersideY = undersideY;
        }

        public IReadOnlyList<LinkVolume> Volumes { get; }

        public float UndersideY { get; }

        public bool Contains(Collider collider)
        {
            return _colliders.Contains(collider);
        }
    }
}
