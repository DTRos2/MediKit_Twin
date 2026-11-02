using System.Collections.Generic;
using System.Reflection;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Inspection.Tests
{
    /// <summary>검사 테스트용 레시피와 키트를 만든다. 슬롯 번호는 항목 순서와 같다.</summary>
    internal static class InspectionTestFactory
    {
        internal static RecipeSO Recipe(string kitCode, params string[] componentIds)
        {
            return Recipe(kitCode, null, componentIds);
        }

        // slotPositions가 있으면 슬롯 i+1의 박스 로컬 좌표로 쓴다.
        internal static RecipeSO Recipe(string kitCode, Vector3[] slotPositions, params string[] componentIds)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeSO>();
            var items = new List<RecipeItem>();
            for (int i = 0; i < componentIds.Length; i++)
            {
                object item = new RecipeItem();
                Set(item, "_componentId", componentIds[i]);
                Set(item, "_slotNumber", i + 1);
                if (slotPositions != null)
                {
                    Set(item, "_slotLocalPosition", slotPositions[i]);
                }

                items.Add((RecipeItem)item);
            }

            typeof(RecipeSO).GetField("_kitCode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(recipe, kitCode);
            typeof(RecipeSO).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(recipe, items);
            return recipe;
        }

        internal static KitData Kit(string kitCode, params string[] detected)
        {
            return new KitData(1, 1, "L-1", "병원", kitCode) { DetectedComponentIds = detected };
        }

        private static void Set(object boxed, string field, object value)
        {
            typeof(RecipeItem).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxed, value);
        }
    }
}
