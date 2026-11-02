using System.Collections.Generic;
using System.Linq;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 랜덤으로 정한 키트 레시피 하나. 항목 순서가 피킹 순서다.
    /// 실패했을 때 어떤 구성품을 어떤 순서로 어느 슬롯에 놓으려 했는지 로그에서 바로 읽을 수 있도록 사람이 읽는 설명을 함께 만든다.
    /// </summary>
    public sealed class RandomRecipe
    {
        public RandomRecipe(string kitCode, int boxSize, IReadOnlyList<RandomRecipeItem> items)
        {
            KitCode = kitCode;
            BoxSize = boxSize;
            Items = items;
        }

        public string KitCode { get; }

        /// <summary>상자 크기(RecipeSO.BoxSize의 정수 값). 구성품이 아니라 공급부가 정하는 값이라 원래 레시피의 값을 그대로 쓴다.</summary>
        public int BoxSize { get; }

        /// <summary>피킹 순서대로 나열한 구성품.</summary>
        public IReadOnlyList<RandomRecipeItem> Items { get; }

        /// <summary>"EM: 1.A-3 antiseptic→슬롯2, 2.C-1 swab→슬롯1"처럼 집는 순서와 놓을 슬롯을 한 줄로 적는다.</summary>
        public string Describe()
        {
            string items = string.Join(
                ", ", Items.Select((item, index) => $"{index + 1}.{item.ShelfSlotId} {item.ComponentId}→슬롯{item.SlotNumber}"));
            return $"{KitCode}: {items}";
        }
    }
}
