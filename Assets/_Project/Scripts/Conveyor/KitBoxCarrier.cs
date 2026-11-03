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
    public sealed class KitBoxCarrier : MonoBehaviour, IKitCarrier
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

        /// <summary>
        /// 박스를 받침 아래로 dropM만큼 내려 벨트 윗면에 닿게 한다. 컨베이어는 받침의 피벗을 입력점 높이에 두는데 그 높이는 벨트 윗면보다 높아서,
        /// 받침(피벗)은 그대로 두고 박스만 내려야 컨베이어의 높이 규약과 박스가 벨트에 얹힌 모양이 함께 맞는다. 0 이하는 아무것도 하지 않는다.
        /// </summary>
        public void SeatOnBelt(float dropM)
        {
            if (dropM <= 0f || Box == null)
            {
                return;
            }

            Box.position += Vector3.down * dropM;
        }
    }
}
