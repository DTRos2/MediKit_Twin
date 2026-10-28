using System;
using UnityEngine;

namespace MediKitTwin.Recipes
{
    /// <summary>
    /// 박스 슬롯 1칸에 담을 구성품 1개. 키트 1개는 구성품을 종류마다 1개씩 쓰므로
    /// 한 레시피에서 구성품 ID와 선반 칸 ID와 슬롯 번호는 각각 겹치지 않는다.
    /// 레시피의 항목 순서가 피킹 순서다.
    /// </summary>
    [Serializable]
    public struct RecipeItem
    {
        [Tooltip("구성품 종류 ID. 슬롯 감지 센서가 보고하는 ID와 같아야 한다.")]
        [SerializeField] private string _componentId;

        [Tooltip("구성품을 집어 올 자재 선반 칸 ID. 행은 키트 종류(A=EM, B=BL, C=DX), 열은 재료 박스(1~4)이며 열 번호는 피킹 순서와 같다. 재고(StockEmpty/StockRefilled)의 단위이기도 하다.")]
        [SerializeField] private string _shelfSlotId;

        [Tooltip("박스 슬롯 번호(1부터). 슬롯 감지 센서와 판정의 MissingSlots가 쓰는 번호다.")]
        [SerializeField] private int _slotNumber;

        [Tooltip("이 슬롯 번호에 대응하는 박스 기준 로컬 좌표(m). 구성품을 내려놓을 위치다.")]
        [SerializeField] private Vector3 _slotLocalPosition;

        public string ComponentId => _componentId;

        public string ShelfSlotId => _shelfSlotId;

        public int SlotNumber => _slotNumber;

        public Vector3 SlotLocalPosition => _slotLocalPosition;
    }
}
