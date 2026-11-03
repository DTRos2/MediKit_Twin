using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 키트 박스를 컨베이어 패키지의 Parcel로 다루기 위한 덧붙임. 어느 키트(KitData)인지 알고, 컨베이어에서 풀어 주는 일을 한다. (FR-05)
    /// 박스 루트에 붙는다. Parcel이 같이 붙는다.
    /// </summary>
    [RequireComponent(typeof(Parcel))]
    public sealed class KitBoxParcel : MonoBehaviour, IBoxConveyorRelease
    {
        private Parcel _parcel;

        public KitData Kit { get; private set; }

        public Parcel Parcel => _parcel != null ? _parcel : (_parcel = GetComponent<Parcel>());

        public void Configure(KitData kit)
        {
            Kit = kit;
        }

        /// <summary>
        /// 컨베이어에서 박스를 풀고 Parcel을 꺼서 떨어지는 동안 컨베이어가 다시 붙잡지 않게 한다.
        /// (Parcel은 컨베이어 입구 영역에 닿은 채 놓인 물체를 컨베이어에 다시 올리려고 하므로 끈다.)
        /// </summary>
        public void ReleaseToPhysics()
        {
            Parcel parcel = Parcel;
            ConveyorBase conveyor = parcel.CurrentConveyor;
            if (conveyor != null)
            {
                conveyor.RemoveParcel(parcel);
            }

            parcel.CurrentConveyor = null;
            parcel.IsCarried = false;
            transform.SetParent(null, true);
            parcel.enabled = false;
        }
    }
}
