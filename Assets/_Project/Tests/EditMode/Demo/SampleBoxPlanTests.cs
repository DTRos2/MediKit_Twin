using System;
using System.Collections.Generic;
using System.Reflection;
using MediKitTwin.Recipes;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class SampleBoxPlanTests
    {
        private static readonly string[] AllIds = { "gauze", "bandage", "antiseptic", "gloves", "swab", "needle" };

        private RecipeSO _recipe;

        [SetUp]
        public void SetUp()
        {
            _recipe = ScriptableObject.CreateInstance<RecipeSO>();
            var items = new List<RecipeItem>();
            string[] ids = { "gauze", "bandage", "antiseptic", "gloves" };
            for (int i = 0; i < ids.Length; i++)
            {
                object item = new RecipeItem();
                typeof(RecipeItem).GetField("_componentId", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(item, ids[i]);
                typeof(RecipeItem).GetField("_slotNumber", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(item, i + 1);
                items.Add((RecipeItem)item);
            }

            typeof(RecipeSO).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_recipe, items);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_recipe);
        }

        [Test]
        public void Normal_FillsEverySlotAsInTheRecipe()
        {
            string[] contents = SampleBoxPlan.Plan(_recipe, SampleVariant.Normal, 2, AllIds, new System.Random(1));

            CollectionAssert.AreEqual(new[] { "gauze", "bandage", "antiseptic", "gloves" }, contents);
        }

        [Test]
        public void Missing_EmptiesOnlyTheTargetSlot()
        {
            string[] contents = SampleBoxPlan.Plan(_recipe, SampleVariant.Missing, 3, AllIds, new System.Random(1));

            CollectionAssert.AreEqual(new[] { "gauze", "bandage", null, "gloves" }, contents);
        }

        [Test]
        public void WrongItem_ReplacesTheTargetWithADifferentComponent()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                string[] contents = SampleBoxPlan.Plan(_recipe, SampleVariant.WrongItem, 2, AllIds, new System.Random(seed));

                Assert.AreNotEqual("bandage", contents[1]);
                Assert.IsNotNull(contents[1]);
                Assert.AreEqual("gauze", contents[0]);
                Assert.AreEqual("antiseptic", contents[2]);
                Assert.AreEqual("gloves", contents[3]);
            }
        }

        [Test]
        public void SensorFailure_StillFillsTheBoxAsInTheRecipe()
        {
            string[] contents = SampleBoxPlan.Plan(_recipe, SampleVariant.SensorFailure, 1, AllIds, new System.Random(1));

            CollectionAssert.AreEqual(new[] { "gauze", "bandage", "antiseptic", "gloves" }, contents);
        }

        [Test]
        public void WrongItem_WithNoOtherComponent_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SampleBoxPlan.Plan(_recipe, SampleVariant.WrongItem, 1, new[] { "gauze" }, new System.Random(1)));
        }

        [Test]
        public void SameSeed_GivesTheSameWrongItem()
        {
            string a = SampleBoxPlan.Plan(_recipe, SampleVariant.WrongItem, 1, AllIds, new System.Random(7))[0];
            string b = SampleBoxPlan.Plan(_recipe, SampleVariant.WrongItem, 1, AllIds, new System.Random(7))[0];

            Assert.AreEqual(a, b);
        }
    }
}
