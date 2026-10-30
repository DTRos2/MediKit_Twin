using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키트와 레시피로 키팅 계획을 세운다. 누락 규칙에게 빠뜨릴 슬롯을 묻고, 그 답을 키트의 정답 라벨에 기록한다.
    /// 정답 라벨은 판정에 쓰지 않고 판정이 끝난 뒤 로그 비교에만 쓴다. (FR-04, FR-06)
    /// </summary>
    public sealed class KittingPlanner
    {
        private readonly IMissingRule _missingRule;

        public KittingPlanner(IMissingRule missingRule)
        {
            _missingRule = missingRule ?? throw new ArgumentNullException(nameof(missingRule));
        }

        public KittingPlan Plan(KitData kit, RecipeSO recipe)
        {
            int? missingSlot = _missingRule.SelectMissingSlot(kit, recipe);
            if (missingSlot.HasValue)
            {
                kit.SetMissingAnswer(missingSlot.Value);
            }

            var items = new List<KittingItem>(recipe.Items.Count);
            foreach (RecipeItem item in recipe.Items)
            {
                bool isSkipped = missingSlot.HasValue && item.SlotNumber == missingSlot.Value;
                items.Add(new KittingItem(item.ComponentId, item.ShelfSlotId, item.SlotNumber, item.SlotLocalPosition, isSkipped));
            }

            return new KittingPlan(kit, recipe, items, missingSlot);
        }
    }
}
