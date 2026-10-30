using System;
using System.Collections.Generic;
using MediKitTwin.Recipes;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 레시피 목록을 키트 코드로 색인한다. 키트 종류를 늘릴 때는 목록에 레시피 에셋 하나를 더하면 되고 이 클래스는 고치지 않는다. (NFR-07)
    /// </summary>
    public sealed class RecipeCatalog : IRecipeCatalog
    {
        private readonly Dictionary<string, RecipeSO> _recipes = new Dictionary<string, RecipeSO>(StringComparer.Ordinal);

        public RecipeCatalog(IEnumerable<RecipeSO> recipes)
        {
            if (recipes == null)
            {
                throw new ArgumentNullException(nameof(recipes));
            }

            foreach (RecipeSO recipe in recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                string code = Normalize(recipe.KitCode);
                if (code.Length == 0)
                {
                    throw new ArgumentException("키트 코드가 비어 있는 레시피가 있다.", nameof(recipes));
                }

                // 겹친 코드를 조용히 덮어쓰면 어느 레시피로 생산되는지 알 수 없어서, 시작할 때 바로 알린다.
                if (!_recipes.TryAdd(code, recipe))
                {
                    throw new ArgumentException($"키트 코드 '{code}'의 레시피가 둘 이상이다.", nameof(recipes));
                }
            }
        }

        public IReadOnlyCollection<string> KitCodes => _recipes.Keys;

        public bool TryGetRecipe(string kitCode, out RecipeSO recipe)
        {
            return _recipes.TryGetValue(Normalize(kitCode), out recipe);
        }

        private static string Normalize(string kitCode)
        {
            return kitCode?.Trim().ToUpperInvariant() ?? string.Empty;
        }
    }
}
