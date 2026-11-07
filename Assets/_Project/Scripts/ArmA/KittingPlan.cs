using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.ArmA
{
    /// <summary>키트 1개를 만드는 계획. 레시피 순서(피킹 순서)대로 항목을 나열하고, 어떤 슬롯을 빠뜨릴지 담는다.</summary>
    public sealed class KittingPlan
    {
        public KittingPlan(KitData kit, RecipeSO recipe, IReadOnlyList<KittingItem> items, int? missingSlot)
        {
            Kit = kit;
            Recipe = recipe;
            Items = items;
            MissingSlot = missingSlot;
        }

        public KitData Kit { get; }

        public RecipeSO Recipe { get; }

        public IReadOnlyList<KittingItem> Items { get; }

        /// <summary>빠뜨릴 슬롯 번호. 누락이 없으면 null이다.</summary>
        public int? MissingSlot { get; }
    }
}
