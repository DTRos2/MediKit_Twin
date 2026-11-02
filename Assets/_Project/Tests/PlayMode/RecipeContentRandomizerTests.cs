using System;
using System.Collections.Generic;
using System.Linq;
using MediKitTwin.Recipes;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    public sealed class RecipeContentRandomizerTests
    {
        private const int MinItems = 2;
        private const int MaxItems = 4;

        private static readonly Vector3[] SlotPositions =
        {
            new Vector3(0f, 0f, 0.082f), new Vector3(0f, 0f, 0.0273f),
            new Vector3(0f, 0f, -0.0273f), new Vector3(0f, 0f, -0.082f),
        };

        private readonly List<RecipeSO> _created = new List<RecipeSO>();

        [TearDown]
        public void TearDown()
        {
            foreach (RecipeSO recipe in _created)
            {
                Object.DestroyImmediate(recipe);
            }

            _created.Clear();
        }

        // 실제 레시피처럼 한 행의 네 칸을 열 번호 순서로 쓰는 레시피 에셋 모양의 객체를 만든다.
        // RecipeSO는 값을 넣는 방법이 없어, 덮어쓰기와 같은 방법(RecipeData)으로 채운다. 이 방법이 통하는지도 함께 확인된다.
        private RecipeSO MakeRecipe(string kitCode, int boxSize, char row, params string[] components)
        {
            RecipeSO recipe = ScriptableObject.CreateInstance<RecipeSO>();
            _created.Add(recipe);

            RandomRecipeItem[] items = components
                .Select((component, index) => new RandomRecipeItem(component, $"{row}-{index + 1}", index + 1, SlotPositions[index]))
                .ToArray();
            JsonUtility.FromJsonOverwrite(new RecipeData(kitCode, boxSize, items).ToJson(), recipe);
            return recipe;
        }

        private RecipeSO[] MakeThreeRecipes()
        {
            return new[]
            {
                MakeRecipe("EM", 0, 'A', "gauze", "bandage", "antiseptic", "gloves"),
                MakeRecipe("BL", 1, 'B', "blood_tube", "needle", "alcohol_pad", "tourniquet"),
                MakeRecipe("DX", 1, 'C', "swab", "buffer_vial", "test_cassette", "sample_bag"),
            };
        }

        private static string Snapshot(RecipeSO recipe)
        {
            return JsonUtility.ToJson(recipe);
        }

        [Test]
        public void TheOverwriteMechanismFillsARecipeFromData()
        {
            RecipeSO recipe = MakeRecipe("EM", 0, 'A', "gauze", "bandage", "antiseptic", "gloves");

            Assert.That(recipe.KitCode, Is.EqualTo("EM"));
            Assert.That(recipe.Items.Select(item => item.ComponentId), Is.EqualTo(new[] { "gauze", "bandage", "antiseptic", "gloves" }));
            Assert.That(recipe.Items.Select(item => item.ShelfSlotId), Is.EqualTo(new[] { "A-1", "A-2", "A-3", "A-4" }));
            Assert.That(recipe.Items.Select(item => item.SlotNumber), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(recipe.Items[3].SlotLocalPosition, Is.EqualTo(SlotPositions[3]));
        }

        [Test]
        public void ApplyKeepsTheKitCodeAndBoxSizeAndChangesTheItems()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            string[] before = recipes.Select(Snapshot).ToArray();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);

            randomizer.Apply(1);

            Assert.That(recipes.Select(recipe => recipe.KitCode), Is.EqualTo(new[] { "EM", "BL", "DX" }));
            Assert.That(recipes.Select(recipe => (int)recipe.BoxSize), Is.EqualTo(new[] { 0, 1, 1 }));
            Assert.That(recipes.Select(Snapshot).ToArray(), Is.Not.EqualTo(before), "구성이 하나도 바뀌지 않았다");
        }

        [Test]
        public void TheAppliedRecipesFollowTheRulesOfARecipe()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);

            for (int seed = 0; seed < 30; seed++)
            {
                randomizer.Apply(seed);

                foreach (RecipeSO recipe in recipes)
                {
                    IReadOnlyList<RecipeItem> items = recipe.Items;
                    Assert.That(items.Count, Is.InRange(MinItems, MaxItems), $"시드 {seed} {recipe.KitCode}");
                    Assert.That(items.Select(item => item.ComponentId).Distinct().Count(), Is.EqualTo(items.Count), $"시드 {seed} {recipe.KitCode}: 구성품 겹침");
                    Assert.That(items.Select(item => item.ShelfSlotId).Distinct().Count(), Is.EqualTo(items.Count), $"시드 {seed} {recipe.KitCode}: 칸 겹침");
                    Assert.That(items.Select(item => item.SlotNumber).OrderBy(n => n), Is.EqualTo(Enumerable.Range(1, items.Count)), $"시드 {seed} {recipe.KitCode}: 슬롯 번호");
                    foreach (RecipeItem item in items)
                    {
                        Assert.That(item.SlotLocalPosition, Is.EqualTo(SlotPositions[item.SlotNumber - 1]), $"시드 {seed} {recipe.KitCode}: 슬롯 좌표");
                        Assert.That(recipe.TryGetItemBySlot(item.SlotNumber, out _), Is.True);
                        Assert.That(recipe.TryGetItemByComponent(item.ComponentId, out _), Is.True);
                    }
                }
            }
        }

        [Test]
        public void AFewerItemRecipeReplacesALongerOneWithoutLeavingTheOldItemsBehind()
        {
            // JsonUtility가 목록을 덮어쓸 때 앞쪽만 바꾸고 뒤쪽 옛 항목을 남기면 구성품이 늘어나 슬롯이 겹친다.
            RecipeSO[] recipes = MakeThreeRecipes();
            var randomizer = new RecipeContentRandomizer(recipes, 2, 2);

            randomizer.Apply(3);

            Assert.That(recipes.All(recipe => recipe.Items.Count == 2), Is.True, string.Join(" / ", recipes.Select(recipe => recipe.Items.Count)));
        }

        [Test]
        public void TheSameSeedMakesTheSameContents()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);

            randomizer.Apply(9);
            string first = randomizer.Describe();
            randomizer.Apply(9);

            Assert.That(randomizer.Describe(), Is.EqualTo(first));
        }

        [Test]
        public void TheOrderOfTheGivenRecipesDoesNotChangeTheContents()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            var forward = new RecipeContentRandomizer(recipes, MinItems, MaxItems);
            var backward = new RecipeContentRandomizer(recipes.AsEnumerable().Reverse(), MinItems, MaxItems);

            forward.Apply(5);
            string expected = forward.Describe();
            forward.Restore();
            backward.Apply(5);

            Assert.That(backward.Describe(), Is.EqualTo(expected));
        }

        [Test]
        public void DifferentSeedsMakeDifferentContents()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);

            int distinct = Enumerable.Range(1, 20).Select(seed =>
            {
                randomizer.Apply(seed);
                return randomizer.Describe();
            }).Distinct().Count();

            Assert.That(distinct, Is.GreaterThan(10));
        }

        [Test]
        public void RestoreBringsEveryRecipeBackExactly()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            string[] before = recipes.Select(Snapshot).ToArray();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);

            randomizer.Apply(4);
            randomizer.Apply(8);
            randomizer.Restore();

            Assert.That(recipes.Select(Snapshot).ToArray(), Is.EqualTo(before));
            Assert.That(randomizer.Describe(), Is.Empty);
        }

        [Test]
        public void RestoreWithoutApplyChangesNothing()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            string[] before = recipes.Select(Snapshot).ToArray();

            new RecipeContentRandomizer(recipes, MinItems, MaxItems).Restore();

            Assert.That(recipes.Select(Snapshot).ToArray(), Is.EqualTo(before));
        }

        [Test]
        public void ApplyAfterRestoreStartsFromTheSameMaterialAsTheFirstApply()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);

            randomizer.Apply(6);
            string first = randomizer.Describe();
            randomizer.Restore();
            randomizer.Apply(6);

            Assert.That(randomizer.Describe(), Is.EqualTo(first));
        }

        [Test]
        public void EveryComponentOnTheShelfCanComeUp()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);
            var seen = new HashSet<string>();

            for (int seed = 0; seed < 100; seed++)
            {
                randomizer.Apply(seed);
                foreach (RecipeSO recipe in recipes)
                {
                    foreach (RecipeItem item in recipe.Items)
                    {
                        seen.Add(item.ComponentId);
                    }
                }
            }

            Assert.That(seen.Count, Is.EqualTo(12));
        }

        [Test]
        public void TheDescriptionNamesEveryKitWithItsItemsInPickOrder()
        {
            RecipeSO[] recipes = MakeThreeRecipes();
            var randomizer = new RecipeContentRandomizer(recipes, MinItems, MaxItems);

            randomizer.Apply(2);
            string[] lines = randomizer.Describe().Split('\n');

            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[0], Does.StartWith("BL: 1."));
            Assert.That(lines[1], Does.StartWith("DX: 1."));
            Assert.That(lines[2], Does.StartWith("EM: 1."));
            Assert.That(lines[0], Does.Contain("→슬롯"));
        }

        [Test]
        public void BadArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RecipeContentRandomizer(null, MinItems, MaxItems));
            Assert.Throws<ArgumentException>(() => new RecipeContentRandomizer(new RecipeSO[0], MinItems, MaxItems));
            Assert.Throws<ArgumentException>(() => new RecipeContentRandomizer(new RecipeSO[] { null }, MinItems, MaxItems));
        }
    }
}
