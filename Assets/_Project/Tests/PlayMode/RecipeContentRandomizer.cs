using System;
using System.Collections.Generic;
using System.Linq;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 씬이 쓰는 레시피의 구성품을 메모리에서 바꿔 끼우고, 시험이 끝나면 원래대로 되돌린다.
    /// 레시피가 정한 구성품 구성(어떤 칸에서 어떤 순서로 집어 어느 슬롯에 놓는가)이 달라져도 팔이 키트를 만드는지 보려는 것이다.
    /// 바꾸는 값은 구성품 목록뿐이고 키트 코드와 상자 크기는 그대로 둔다. 주문이 쓰는 코드가 바뀌면 주문 검사가 어긋나기 때문이다.
    /// 에셋 파일은 건드리지 않는다(메모리의 객체만 고치고, 저장 표시를 하지 않는다). 그래도 시험이 끝나면 반드시 Restore를 불러야
    /// 같은 에디터 세션의 다음 시험이 바뀐 레시피를 물려받지 않는다.
    /// </summary>
    public sealed class RecipeContentRandomizer
    {
        // 주문을 섞는 난수와 같은 시드를 쓰면 두 난수열이 같아 주문과 구성이 서로 묶여 보이므로, 시드를 한 번 비튼다.
        private const int SeedSalt = 7919;

        private readonly RecipeSO[] _recipes;
        private readonly string[] _originals;
        private readonly IReadOnlyList<Vector3>[] _slotPositions;
        private readonly RandomRecipeGenerator _generator;
        private readonly List<RandomRecipe> _applied = new List<RandomRecipe>();

        /// <param name="recipes">바꿀 레시피. 같은 레시피가 겹쳐도 한 번만 쓴다.</param>
        /// <param name="minItems">레시피 하나의 최소 구성품 수.</param>
        /// <param name="maxItems">레시피 하나의 최대 구성품 수(박스 슬롯 수까지).</param>
        public RecipeContentRandomizer(IEnumerable<RecipeSO> recipes, int minItems, int maxItems)
        {
            if (recipes == null)
            {
                throw new ArgumentNullException(nameof(recipes));
            }

            // 열거 순서는 실행마다 다를 수 있으므로, 같은 시드가 같은 구성을 내도록 키트 코드 순으로 고정한다.
            _recipes = recipes
                .Where(recipe => recipe != null)
                .Distinct()
                .OrderBy(recipe => recipe.KitCode, StringComparer.Ordinal)
                .ToArray();

            if (_recipes.Length == 0)
            {
                throw new ArgumentException("바꿀 레시피가 하나도 없다.", nameof(recipes));
            }

            // 원래 값은 처음 한 번만 적어 둔다. 칸 안의 물건과 슬롯 좌표는 원래 레시피에서 읽어야 몇 번을 바꿔도 같은 재료로 시작한다.
            _originals = _recipes.Select(recipe => JsonUtility.ToJson(recipe)).ToArray();
            _slotPositions = _recipes
                .Select(recipe => (IReadOnlyList<Vector3>)recipe.Items.OrderBy(item => item.SlotNumber).Select(item => item.SlotLocalPosition).ToArray())
                .ToArray();

            IEnumerable<ShelfCellContent> cells = _recipes
                .SelectMany(recipe => recipe.Items)
                .Select(item => new ShelfCellContent(item.ShelfSlotId, item.ComponentId));
            _generator = new RandomRecipeGenerator(cells, minItems, maxItems);
        }

        /// <summary>시드로 정한 구성으로 모든 레시피를 바꿔 끼운다. 같은 시드면 같은 구성이 된다.</summary>
        public void Apply(int seed)
        {
            _applied.Clear();
            var random = new System.Random(seed ^ SeedSalt);

            for (int i = 0; i < _recipes.Length; i++)
            {
                RecipeSO recipe = _recipes[i];
                RandomRecipe made = _generator.Generate(random, recipe.KitCode, (int)recipe.BoxSize, _slotPositions[i]);

                JsonUtility.FromJsonOverwrite(RecipeData.From(made).ToJson(), recipe);
                _applied.Add(made);
            }
        }

        /// <summary>처음 모습으로 되돌린다. Apply를 하지 않았어도 부를 수 있다.</summary>
        public void Restore()
        {
            for (int i = 0; i < _recipes.Length; i++)
            {
                JsonUtility.FromJsonOverwrite(_originals[i], _recipes[i]);
            }

            _applied.Clear();
        }

        /// <summary>바꿔 끼운 구성을 키트마다 한 줄로 적는다. 실패 메시지에 붙여 같은 시드로 재현할 수 있게 한다.</summary>
        public string Describe()
        {
            return string.Join("\n", _applied.Select(recipe => recipe.Describe()));
        }
    }
}
