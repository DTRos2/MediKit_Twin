using System.Reflection;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Orders.Tests
{
    /// <summary>키트 코드만 가진 RecipeSO를 만든다. 주문 관리는 레시피 내용이 아니라 코드로만 찾으므로 항목은 채우지 않는다.</summary>
    internal static class RecipeTestFactory
    {
        internal static RecipeSO Create(string kitCode)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeSO>();
            typeof(RecipeSO).GetField("_kitCode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(recipe, kitCode);
            return recipe;
        }
    }
}
