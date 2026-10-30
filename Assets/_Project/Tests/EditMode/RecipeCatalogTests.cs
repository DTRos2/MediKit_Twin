using System;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class RecipeCatalogTests
    {
        [Test]
        public void TryGetRecipe_IsCaseAndWhitespaceInsensitive()
        {
            var em = RecipeTestFactory.Create("EM");
            var catalog = new RecipeCatalog(new[] { em });

            Assert.That(catalog.TryGetRecipe(" em ", out var found), Is.True);
            Assert.That(found, Is.SameAs(em));
        }

        [Test]
        public void TryGetRecipe_UnknownCode_ReturnsFalse()
        {
            var catalog = new RecipeCatalog(new[] { RecipeTestFactory.Create("EM") });

            Assert.That(catalog.TryGetRecipe("XX", out var found), Is.False);
            Assert.That(found, Is.Null);
        }

        [Test]
        public void KitCodes_ListsRegisteredCodes()
        {
            var catalog = new RecipeCatalog(new[] { RecipeTestFactory.Create("EM"), RecipeTestFactory.Create("DX") });

            Assert.That(catalog.KitCodes, Is.EquivalentTo(new[] { "EM", "DX" }));
        }

        [Test]
        public void Constructor_DuplicateKitCode_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new RecipeCatalog(new[] { RecipeTestFactory.Create("EM"), RecipeTestFactory.Create("em") }));
        }

        [Test]
        public void Constructor_EmptyKitCode_Throws()
        {
            Assert.Throws<ArgumentException>(() => new RecipeCatalog(new[] { RecipeTestFactory.Create("") }));
        }

        [Test]
        public void Constructor_NullEntries_AreSkipped()
        {
            var catalog = new RecipeCatalog(new[] { null, RecipeTestFactory.Create("EM") });

            Assert.That(catalog.KitCodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void KitCodes_FeedOrderValidatorSoNewRecipeNeedsNoCodeChange()
        {
            // NFR-07: 레시피 하나를 더하면 주문 파일 검증도 그 코드를 받아들인다.
            var catalog = new RecipeCatalog(new[] { RecipeTestFactory.Create("EM"), RecipeTestFactory.Create("NEW") });
            var validator = new KnownKitCodeValidator(catalog.KitCodes);

            bool isValid = validator.TryValidate(new OrderRecord(1, "A", "NEW", 1), 1, new OrderValidationContext(), out _);

            Assert.That(isValid, Is.True);
        }
    }
}
