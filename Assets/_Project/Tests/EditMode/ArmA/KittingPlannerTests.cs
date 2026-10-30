using System.Linq;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class KittingPlannerTests
    {
        private static KitData Kit() => new KitData(1, 1, "LOT-0001", "병원", "EM");

        // 정해진 슬롯을 빠뜨리거나 아무것도 빠뜨리지 않는 시험용 규칙.
        private sealed class FixedRule : IMissingRule
        {
            private readonly int? _slot;

            internal FixedRule(int? slot)
            {
                _slot = slot;
            }

            public int? SelectMissingSlot(KitData kit, RecipeSO recipe) => _slot;
        }

        [Test]
        public void Plan_ListsItemsInRecipePickingOrder()
        {
            var plan = new KittingPlanner(new FixedRule(null)).Plan(Kit(), KittingTestRecipes.Em());

            Assert.That(plan.Items.Select(i => i.ComponentId), Is.EqualTo(new[] { "gauze", "bandage", "antiseptic" }));
            Assert.That(plan.Items.Select(i => i.ShelfSlotId), Is.EqualTo(new[] { "A-1", "A-2", "A-3" }));
            Assert.That(plan.Items.Select(i => i.SlotNumber), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Plan_NoMissing_SkipsNothingAndRecordsNoAnswer()
        {
            var kit = Kit();

            var plan = new KittingPlanner(new FixedRule(null)).Plan(kit, KittingTestRecipes.Em());

            Assert.That(plan.Items.Any(i => i.IsSkipped), Is.False);
            Assert.That(plan.MissingSlot, Is.Null);
            Assert.That(kit.GetMissingAnswerForEvaluation(), Is.Null);
        }

        [Test]
        public void Plan_MissingSlot_MarksOnlyThatItemAsSkippedAndRecordsTheAnswer()
        {
            var kit = Kit();

            var plan = new KittingPlanner(new FixedRule(2)).Plan(kit, KittingTestRecipes.Em());

            Assert.That(plan.Items.Where(i => i.IsSkipped).Select(i => i.ComponentId), Is.EqualTo(new[] { "bandage" }));
            Assert.That(plan.MissingSlot, Is.EqualTo(2));
            Assert.That(kit.GetMissingAnswerForEvaluation(), Is.EqualTo(2));
        }

        [Test]
        public void Plan_KeepsKitAndRecipe()
        {
            var kit = Kit();
            var recipe = KittingTestRecipes.Em();

            var plan = new KittingPlanner(new FixedRule(null)).Plan(kit, recipe);

            Assert.That(plan.Kit, Is.SameAs(kit));
            Assert.That(plan.Recipe, Is.SameAs(recipe));
        }

        [Test]
        public void Constructor_NullRule_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new KittingPlanner(null));
        }
    }
}
