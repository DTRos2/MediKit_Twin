using System.Linq;
using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class RandomMissingRuleTests
    {
        private static KitData Kit(int sequence) => new KitData(sequence, 1, "LOT-" + sequence, "병원", "EM");

        private static RandomMissingRule Rule(float percent, int fixedKit = 0, int fixedSlot = 0, int seed = 1234)
        {
            return new RandomMissingRule(new MissingRuleSettings(percent, fixedKit, fixedSlot, seed));
        }

        [Test]
        public void ZeroPercentAndNoFixedKit_NeverMissing()
        {
            var rule = Rule(0f);
            var recipe = KittingTestRecipes.Em();

            Assert.That(Enumerable.Range(1, 50).All(n => rule.SelectMissingSlot(Kit(n), recipe) == null), Is.True);
        }

        [Test]
        public void HundredPercent_AlwaysMissingWithASlotOfTheRecipe()
        {
            var rule = Rule(100f);
            var recipe = KittingTestRecipes.Em();

            foreach (int n in Enumerable.Range(1, 30))
            {
                int? slot = rule.SelectMissingSlot(Kit(n), recipe);
                Assert.That(slot, Is.Not.Null);
                Assert.That(slot.Value, Is.InRange(1, 3));
            }
        }

        [Test]
        public void FixedKit_IsAlwaysMissingEvenAtZeroPercent()
        {
            var rule = Rule(0f, fixedKit: 3);
            var recipe = KittingTestRecipes.Em();

            Assert.That(rule.SelectMissingSlot(Kit(3), recipe), Is.Not.Null);
            Assert.That(rule.SelectMissingSlot(Kit(2), recipe), Is.Null);
            Assert.That(rule.SelectMissingSlot(Kit(4), recipe), Is.Null);
        }

        [Test]
        public void FixedKitWithFixedSlot_UsesThatSlot()
        {
            var rule = Rule(0f, fixedKit: 3, fixedSlot: 2);

            Assert.That(rule.SelectMissingSlot(Kit(3), KittingTestRecipes.Em()), Is.EqualTo(2));
        }

        [Test]
        public void FixedSlotNotInRecipe_FallsBackToARecipeSlot()
        {
            var rule = Rule(0f, fixedKit: 3, fixedSlot: 9);

            int? slot = rule.SelectMissingSlot(Kit(3), KittingTestRecipes.Em());

            Assert.That(slot.Value, Is.InRange(1, 3));
        }

        [Test]
        public void SameSeed_GivesSameResultForTheSameKitEvenInAnotherOrder()
        {
            // NFR-06: 같은 시드면 같은 키트에서 같은 누락이 나야 하고, 키트를 묻는 순서에 좌우되지 않는다.
            var recipe = KittingTestRecipes.Em();
            var first = Rule(50f);
            var second = Rule(50f);

            var forward = Enumerable.Range(1, 40).Select(n => first.SelectMissingSlot(Kit(n), recipe)).ToList();
            var backward = Enumerable.Range(1, 40).Reverse().Select(n => second.SelectMissingSlot(Kit(n), recipe)).Reverse().ToList();

            Assert.That(backward, Is.EqualTo(forward));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentMissingPatterns()
        {
            var recipe = KittingTestRecipes.Em();
            var a = Enumerable.Range(1, 60).Select(n => Rule(50f, seed: 1).SelectMissingSlot(Kit(n), recipe)).ToList();
            var b = Enumerable.Range(1, 60).Select(n => Rule(50f, seed: 2).SelectMissingSlot(Kit(n), recipe)).ToList();

            Assert.That(b, Is.Not.EqualTo(a));
        }

        [Test]
        public void Probability_RoughlyMatchesTheConfiguredPercent()
        {
            var rule = Rule(10f);
            var recipe = KittingTestRecipes.Em();

            int missing = Enumerable.Range(1, 2000).Count(n => rule.SelectMissingSlot(Kit(n), recipe) != null);

            Assert.That(missing, Is.InRange(140, 270));
        }

        [Test]
        public void OutOfRangePercent_IsClampedToTheNearestEnd()
        {
            var recipe = KittingTestRecipes.Em();

            Assert.That(Rule(-5f).SelectMissingSlot(Kit(1), recipe), Is.Null);
            Assert.That(Rule(500f).SelectMissingSlot(Kit(1), recipe), Is.Not.Null);
        }

        [Test]
        public void RecipeWithoutItems_IsNeverMissing()
        {
            var empty = KittingTestRecipes.Create("EM");

            Assert.That(Rule(100f).SelectMissingSlot(Kit(1), empty), Is.Null);
        }

        [Test]
        public void ResumedKitWithNewSequence_IsDrawnAgain()
        {
            // 재고 소진으로 다시 만드는 키트는 새 투입 순서를 받으므로 그 순서로 새로 추첨한다.
            var rule = Rule(0f, fixedKit: 3);
            var recipe = KittingTestRecipes.Em();

            Assert.That(rule.SelectMissingSlot(Kit(3), recipe), Is.Not.Null);
            Assert.That(rule.SelectMissingSlot(Kit(6), recipe), Is.Null);
        }
    }
}
