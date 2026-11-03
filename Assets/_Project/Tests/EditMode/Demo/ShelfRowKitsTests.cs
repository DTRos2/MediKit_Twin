using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ShelfRowKitsTests
    {
        [Test]
        public void ARecipesShelfSlots_TellWhichKitTheRowBelongsTo()
        {
            var kits = new ShelfRowKits();
            kits.Add("EM", new[] { "A-1", "A-2", "A-3", "A-4" });
            kits.Add("BL", new[] { "B-1", "B-2" });

            Assert.That(kits.KitCodeOf('A'), Is.EqualTo("EM"));
            Assert.That(kits.KitCodeOf('B'), Is.EqualTo("BL"));
        }

        [Test]
        public void ARowNoRecipeUses_HasNoKit()
        {
            var kits = new ShelfRowKits();
            kits.Add("EM", new[] { "A-1" });

            Assert.That(kits.KitCodeOf('C'), Is.EqualTo(string.Empty));
        }

        [Test]
        public void WhenTwoRecipesShareARow_TheFirstOneWins()
        {
            var kits = new ShelfRowKits();
            kits.Add("EM", new[] { "A-1" });
            kits.Add("DX", new[] { "A-2" });

            Assert.That(kits.KitCodeOf('A'), Is.EqualTo("EM"));
        }

        [Test]
        public void EmptyInput_IsIgnoredWithoutThrowing()
        {
            var kits = new ShelfRowKits();

            Assert.DoesNotThrow(() => kits.Add(null, new[] { "A-1" }));
            Assert.DoesNotThrow(() => kits.Add("EM", null));
            Assert.DoesNotThrow(() => kits.Add("EM", new[] { string.Empty, null }));
            Assert.That(kits.KitCodeOf('A'), Is.EqualTo(string.Empty));
        }

        [Test]
        public void NoRecipes_GiveAnEmptyMap()
        {
            ShelfRowKits kits = ShelfRowKits.FromRecipes(null);

            Assert.That(kits.KitCodeOf('A'), Is.EqualTo(string.Empty));
        }
    }
}
