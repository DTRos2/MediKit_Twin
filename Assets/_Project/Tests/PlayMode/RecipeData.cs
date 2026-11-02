using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// RecipeSO를 JsonUtility로 덮어쓰기 위한 사본. RecipeSO의 필드는 private이고 바꾸는 메서드가 없는데,
    /// 시험만을 위해 제품 코드에 바꾸는 메서드를 열지 않으려고 직렬화 필드 이름(_kitCode 등)을 맞춘 사본을 JSON으로 만들어 덮어쓴다.
    /// 에디터에서 에셋을 고치는 것이 아니라 메모리의 객체만 고치므로 디스크의 레시피 에셋은 바뀌지 않는다.
    /// </summary>
    [Serializable]
    internal sealed class RecipeData
    {
        [SerializeField] private string _kitCode;
        [SerializeField] private int _boxSize;
        [SerializeField] private List<RecipeItemData> _items;

        public RecipeData(string kitCode, int boxSize, IEnumerable<RandomRecipeItem> items)
        {
            _kitCode = kitCode;
            _boxSize = boxSize;
            _items = items.Select(item => new RecipeItemData(item)).ToList();
        }

        public static RecipeData From(RandomRecipe recipe)
        {
            return new RecipeData(recipe.KitCode, recipe.BoxSize, recipe.Items);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
