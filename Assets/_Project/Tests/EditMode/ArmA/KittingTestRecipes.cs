using System.Collections.Generic;
using System.Reflection;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    /// <summary>
    /// 항목을 가진 RecipeSO를 만든다. 레시피 필드는 인스펙터 편집용 private이라 리플렉션으로 채운다.
    /// 운영 코드에 시험용 생성자를 열어 두지 않기 위해서다.
    /// </summary>
    internal static class KittingTestRecipes
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        internal static RecipeSO Create(string kitCode, params (string componentId, string shelfSlotId, int slotNumber)[] items)
        {
            var list = new List<RecipeItem>();
            foreach (var (componentId, shelfSlotId, slotNumber) in items)
            {
                object boxed = new RecipeItem();
                typeof(RecipeItem).GetField("_componentId", Private).SetValue(boxed, componentId);
                typeof(RecipeItem).GetField("_shelfSlotId", Private).SetValue(boxed, shelfSlotId);
                typeof(RecipeItem).GetField("_slotNumber", Private).SetValue(boxed, slotNumber);
                list.Add((RecipeItem)boxed);
            }

            var recipe = ScriptableObject.CreateInstance<RecipeSO>();
            typeof(RecipeSO).GetField("_kitCode", Private).SetValue(recipe, kitCode);
            typeof(RecipeSO).GetField("_items", Private).SetValue(recipe, list);
            return recipe;
        }

        /// <summary>EM 레시피: 거즈(A-1, 슬롯 1), 붕대(A-2, 슬롯 2), 소독제(A-3, 슬롯 3).</summary>
        internal static RecipeSO Em()
        {
            return Create("EM", ("gauze", "A-1", 1), ("bandage", "A-2", 2), ("antiseptic", "A-3", 3));
        }
    }
}
