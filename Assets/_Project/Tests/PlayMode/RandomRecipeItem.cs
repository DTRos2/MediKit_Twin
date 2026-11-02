using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>랜덤으로 정한 레시피의 구성품 하나: 어느 칸에서 집어 박스의 몇 번 슬롯(어느 좌표)에 놓을지.</summary>
    public readonly struct RandomRecipeItem
    {
        public RandomRecipeItem(string componentId, string shelfSlotId, int slotNumber, Vector3 slotLocalPosition)
        {
            ComponentId = componentId;
            ShelfSlotId = shelfSlotId;
            SlotNumber = slotNumber;
            SlotLocalPosition = slotLocalPosition;
        }

        public string ComponentId { get; }

        public string ShelfSlotId { get; }

        /// <summary>박스 슬롯 번호(1부터). 피킹 순서와 같을 필요가 없다.</summary>
        public int SlotNumber { get; }

        /// <summary>박스 기준 로컬 좌표(m). 구성품을 내려놓을 위치다.</summary>
        public Vector3 SlotLocalPosition { get; }
    }
}
