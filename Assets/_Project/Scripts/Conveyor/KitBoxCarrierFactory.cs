using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 박스를 받침(KitBoxCarrier)에 얹는다. 받침은 박스 바닥 중앙에 만들고, 박스는 월드 위치와 자세를 그대로 둔 채 받침의 자식이 된다.
    /// 지그에서 떼는 순간 박스가 움직이거나 돌아 보이지 않게 하려는 것이다.
    /// 검사·푸셔 구간이 받침과 박스를 알아보도록 KitBoxParcel(받침)과 KitBoxConveyorRelease(박스)도 붙인다.
    /// </summary>
    public sealed class KitBoxCarrierFactory
    {
        /// <param name="box">받침에 얹을 박스(구성품이 담겨 있는 채로).</param>
        /// <param name="kit">이 박스가 담은 키트.</param>
        /// <param name="carrierRotation">받침의 월드 회전. 컨베이어의 진행 방향과 맞는 회전을 준다.</param>
        public KitBoxCarrier Create(Transform box, KitData kit, Quaternion carrierRotation)
        {
            Vector3 pivot = BoxPivot.BottomCenter(box.position, MeasureBounds(box));

            var carrierObject = new GameObject(KitBoxCarrier.ObjectName);
            carrierObject.transform.SetPositionAndRotation(pivot, carrierRotation);

            // 부모만 바꾸고 월드 위치는 유지한다. 이 줄이 컨베이어에 올리기 전에 박스를 지그에서 떼어 낸다.
            box.SetParent(carrierObject.transform, true);

            var carrier = carrierObject.AddComponent<KitBoxCarrier>();
            var parcel = carrierObject.AddComponent<Parcel>();
            carrier.Bind(kit, box, parcel);

            // 검사 구간 제어(InspectionConveyorLine)는 Parcel에서 KitBoxParcel로 키트를 찾고,
            // 푸셔는 박스에서 IBoxConveyorRelease를 찾아 컨베이어에서 풀므로 둘 다 붙여 둔다.
            carrierObject.AddComponent<KitBoxParcel>().Configure(kit);
            if (box.GetComponent<KitBoxConveyorRelease>() == null)
            {
                box.gameObject.AddComponent<KitBoxConveyorRelease>();
            }

            return carrier;
        }

        // 박스 아래 모든 렌더러를 감싼 범위. 렌더러가 없으면 박스 위치 한 점이다.
        private static Bounds MeasureBounds(Transform box)
        {
            var bounds = new Bounds(box.position, Vector3.zero);
            foreach (Renderer renderer in box.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
