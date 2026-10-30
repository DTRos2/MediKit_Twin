using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>선반 칸 하나의 구성품을 박스 슬롯 하나에 채워 달라는 요청.</summary>
    public readonly struct FillRequest
    {
        public FillRequest(string shelfSlotId, Vector3 slotLocalPosition, int slotNumber, int orderSequence)
        {
            ShelfSlotId = shelfSlotId;
            SlotLocalPosition = slotLocalPosition;
            SlotNumber = slotNumber;
            OrderSequence = orderSequence;
        }

        public string ShelfSlotId { get; }

        /// <summary>박스 기준 로컬 좌표(m). 구성품을 내려놓을 위치다.</summary>
        public Vector3 SlotLocalPosition { get; }

        public int SlotNumber { get; }

        /// <summary>재고가 비었을 때 알릴 주문 순번. 어떤 주문도 아니면 0이다.</summary>
        public int OrderSequence { get; }
    }
}
