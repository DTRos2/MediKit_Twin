using System;
using System.Collections.Generic;
using MediKitTwin.Recipes;

namespace MediKitTwin.ML
{
    /// <summary>
    /// 합성 박스 하나를 만들 때 필요한 레시피 정보: 키트 코드와 슬롯 번호 순서의 구성품 ID.
    /// 생성 계획(SyntheticCasePlanner)은 RecipeSO를 직접 읽지 않고 이 값만 보므로, 계획 로직을 레시피 에셋 없이 시험할 수 있다.
    /// </summary>
    public sealed class SyntheticKitSpec
    {
        private readonly string[] _componentIdsBySlot;

        public SyntheticKitSpec(string kitCode, IReadOnlyList<string> componentIdsBySlot)
        {
            if (string.IsNullOrEmpty(kitCode))
            {
                throw new ArgumentException("키트 코드가 비어 있다.", nameof(kitCode));
            }

            if (componentIdsBySlot == null || componentIdsBySlot.Count == 0)
            {
                throw new ArgumentException("구성품이 하나도 없다.", nameof(componentIdsBySlot));
            }

            KitCode = kitCode;
            _componentIdsBySlot = new string[componentIdsBySlot.Count];
            for (int i = 0; i < componentIdsBySlot.Count; i++)
            {
                _componentIdsBySlot[i] = componentIdsBySlot[i];
            }
        }

        public string KitCode { get; }

        public int SlotCount => _componentIdsBySlot.Length;

        /// <summary>슬롯 번호 순서(인덱스 = 슬롯 번호 - 1)의 구성품 ID. 호출할 때마다 복사본을 돌려준다.</summary>
        public string[] CopyComponentIds()
        {
            return (string[])_componentIdsBySlot.Clone();
        }

        /// <summary>레시피에서 슬롯 번호 순서로 구성품 ID를 모아 만든다. 레시피 항목의 슬롯 번호는 1부터 항목 수까지 겹치지 않아야 한다.</summary>
        public static SyntheticKitSpec FromRecipe(RecipeSO recipe)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            var ids = new string[recipe.Items.Count];
            for (int i = 0; i < recipe.Items.Count; i++)
            {
                RecipeItem item = recipe.Items[i];
                if (item.SlotNumber < 1 || item.SlotNumber > ids.Length || ids[item.SlotNumber - 1] != null)
                {
                    throw new ArgumentException($"{recipe.name}: 슬롯 번호 {item.SlotNumber}가 범위를 벗어나거나 겹친다.", nameof(recipe));
                }

                ids[item.SlotNumber - 1] = item.ComponentId;
            }

            return new SyntheticKitSpec(recipe.KitCode, ids);
        }
    }
}
