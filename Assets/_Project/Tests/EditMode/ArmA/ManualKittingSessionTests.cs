using MediKitTwin.Recipes;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ManualKittingSessionTests
    {
        private ManualKittingSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = new ManualKittingSession();
            _session.SelectRecipe(KittingTestRecipes.Em());
        }

        [Test]
        public void TryPrepare_WithoutARecipe_Refuses()
        {
            var empty = new ManualKittingSession();

            bool ok = empty.TryPrepare("A-1", out _, out string reason);

            Assert.That(ok, Is.False);
            Assert.That(reason, Does.Contain("키트 종류"));
        }

        [Test]
        public void TryPrepare_ShelfSlotOfTheRecipe_ReturnsItsRecipeItem()
        {
            bool ok = _session.TryPrepare("A-2", out RecipeItem item, out _);

            Assert.That(ok, Is.True);
            Assert.That(item.SlotNumber, Is.EqualTo(2));
            Assert.That(item.ComponentId, Is.EqualTo("bandage"));
        }

        [Test]
        public void TryPrepare_ShelfSlotNotInTheRecipe_RefusesWithTheKitCode()
        {
            bool ok = _session.TryPrepare("B-1", out _, out string reason);

            Assert.That(ok, Is.False);
            Assert.That(reason, Does.Contain("B-1").And.Contain("EM"));
        }

        [Test]
        public void TryPrepare_AlreadyFilledSlot_Refuses()
        {
            _session.MarkFilled(1);

            bool ok = _session.TryPrepare("A-1", out _, out string reason);

            Assert.That(ok, Is.False);
            Assert.That(reason, Does.Contain("이미"));
        }

        [Test]
        public void IsComplete_OnlyWhenEverySlotIsFilled()
        {
            _session.MarkFilled(1);
            _session.MarkFilled(2);
            Assert.That(_session.IsComplete, Is.False);

            _session.MarkFilled(3);

            Assert.That(_session.IsComplete, Is.True);
            Assert.That(_session.FilledCount, Is.EqualTo(3));
        }

        [Test]
        public void SelectRecipe_ClearsTheFilledSlots()
        {
            _session.MarkFilled(1);

            _session.SelectRecipe(KittingTestRecipes.Em());

            Assert.That(_session.FilledCount, Is.EqualTo(0));
        }

        [Test]
        public void NotifyBox_SameBox_KeepsTheFilledSlots_NewBox_ClearsThem()
        {
            var firstBox = new object();
            _session.NotifyBox(firstBox);
            _session.MarkFilled(1);

            _session.NotifyBox(firstBox);
            Assert.That(_session.FilledCount, Is.EqualTo(1), "같은 박스이면 기록을 지키지 않는다");

            _session.NotifyBox(new object());
            Assert.That(_session.FilledCount, Is.EqualTo(0), "새 박스이면 채운 슬롯이 없다");
        }

        [Test]
        public void Reset_ClearsTheFilledSlots()
        {
            _session.MarkFilled(2);

            _session.Reset();

            Assert.That(_session.FilledCount, Is.EqualTo(0));
        }
    }
}
