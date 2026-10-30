using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>키팅 계획의 항목 하나: 선반 칸에서 집어 박스 슬롯에 놓을 구성품 1개. 누락 규칙이 건너뛰기로 한 항목은 집지 않는다.</summary>
    public readonly struct KittingItem
    {
        public KittingItem(string componentId, string shelfSlotId, int slotNumber, Vector3 slotLocalPosition, bool isSkipped)
        {
            ComponentId = componentId;
            ShelfSlotId = shelfSlotId;
            SlotNumber = slotNumber;
            SlotLocalPosition = slotLocalPosition;
            IsSkipped = isSkipped;
        }

        public string ComponentId { get; }

        public string ShelfSlotId { get; }

        public int SlotNumber { get; }

        /// <summary>박스 기준 로컬 좌표(m). 구성품을 내려놓을 위치다.</summary>
        public Vector3 SlotLocalPosition { get; }

        /// <summary>누락 규칙이 빠뜨리기로 한 항목이면 true. 집지 않으므로 재고도 줄지 않는다.</summary>
        public bool IsSkipped { get; }
    }
}
