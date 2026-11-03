using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 위를 가는 키트 박스의 받침. 피벗이 박스 바닥 중앙이고 컨베이어가 옮기는 Parcel이 붙어 있으며,
    /// 어느 키트의 박스인지(KitData)를 들고 다닌다. 컨베이어 쪽 이벤트(도착, 막힘)는 Parcel만 알려 주므로
    /// 이 컴포넌트로 키트를 되찾는다. 박스와 그 안의 구성품, 덮인 뚜껑은 박스의 자식이라 받침을 따라 함께 움직인다.
    /// </summary>
    public sealed class KitBoxCarrier : MonoBehaviour
    {
        /// <summary>
        /// 받침 오브젝트의 이름. 팔 B(LidCoverTask)는 이름에 CellObjectNames.KitBoxKey("KitBox")가 들어간 첫 자손을 덮을 박스로 쓰므로,
        /// 받침 이름에 그 글자가 들어가면 받침을 박스로 오해한다. 그래서 일부러 피한 이름이며, 시험(KitBoxCarrierFactoryTests)이 지킨다.
        /// </summary>
        public const string ObjectName = "KitCarrier";

        public KitData Kit { get; private set; }

        public Transform Box { get; private set; }

        /// <summary>컨베이어가 옮기는 쪽. 컨베이어 에셋의 이벤트와 API는 이것으로 받는다.</summary>
        public Parcel Parcel { get; private set; }

        internal void Bind(KitData kit, Transform box, Parcel parcel)
        {
            Kit = kit;
            Box = box;
            Parcel = parcel;
        }
    }
}
