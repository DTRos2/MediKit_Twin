using System;
using NUnit.Framework;

namespace MediKitTwin.Common.Tests
{
    public sealed class FrameBudgetTests
    {
        private double _now;

        [SetUp]
        public void SetUp()
        {
            _now = 0d;
        }

        private FrameBudget Create(double sliceSeconds) => new FrameBudget(sliceSeconds, () => _now);

        [Test]
        public void ANewBudget_IsNotExhausted()
        {
            Assert.That(Create(0.008).IsExhausted, Is.False);
        }

        [Test]
        public void TheBudget_IsExhausted_OnceTheSliceTimeHasPassed()
        {
            FrameBudget budget = Create(0.008);

            _now = 0.007;
            Assert.That(budget.IsExhausted, Is.False);

            _now = 0.008;
            Assert.That(budget.IsExhausted, Is.True, "예산과 같아지면 다 쓴 것이다");
        }

        [Test]
        public void StartSlice_GivesBackTheWholeBudget()
        {
            FrameBudget budget = Create(0.008);
            _now = 0.02;
            Assert.That(budget.IsExhausted, Is.True);

            budget.StartSlice();

            Assert.That(budget.IsExhausted, Is.False);
            _now = 0.027;
            Assert.That(budget.IsExhausted, Is.False);
            _now = 0.028;
            Assert.That(budget.IsExhausted, Is.True);
        }

        [Test]
        public void ASliceOfZeroOrLess_IsAlwaysExhausted()
        {
            Assert.That(Create(0d).IsExhausted, Is.True);
            Assert.That(Create(-1d).IsExhausted, Is.True);
        }

        [Test]
        public void TheBudget_GrowsWithTheSpeed()
        {
            float speed = 1f;
            var budget = new FrameBudget(0.008, () => _now, () => speed);

            speed = 2f;
            _now = 0.015;
            Assert.That(budget.IsExhausted, Is.False, "2배속이면 예산이 0.016초다");

            _now = 0.016;
            Assert.That(budget.IsExhausted, Is.True);
        }

        [Test]
        public void TheBudget_ShrinksWithASlowSpeed()
        {
            var budget = new FrameBudget(0.008, () => _now, () => 0.5f);

            _now = 0.004;

            Assert.That(budget.IsExhausted, Is.True, "0.5배속이면 예산이 0.004초다");
        }

        [Test]
        public void ASpeedOfZeroOrLess_LeavesTheBudgetAsIs()
        {
            var budget = new FrameBudget(0.008, () => _now, () => 0f);

            _now = 0.007;
            Assert.That(budget.IsExhausted, Is.False);

            _now = 0.008;
            Assert.That(budget.IsExhausted, Is.True);
        }

        [Test]
        public void TheClock_IsRequired()
        {
            Assert.Throws<ArgumentNullException>(() => new FrameBudget(0.008, null));
        }
    }
}
