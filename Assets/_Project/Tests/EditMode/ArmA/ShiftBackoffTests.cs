using System;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ShiftBackoffTests
    {
        [Test]
        public void TheDefault_StartsWithTheWholeShift_AndGetsSmaller()
        {
            var fractions = ShiftBackoff.Default.Fractions;

            Assert.That(fractions[0], Is.EqualTo(1f));
            for (int i = 1; i < fractions.Count; i++)
            {
                Assert.That(fractions[i], Is.LessThan(fractions[i - 1]));
            }
        }

        [Test]
        public void None_TriesOnlyTheWholeShift()
        {
            Assert.That(ShiftBackoff.None.Fractions, Is.EqualTo(new[] { 1f }));
        }

        [Test]
        public void NoFractions_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ShiftBackoff(new float[0]));
        }

        [TestCase(0f)]
        [TestCase(-0.5f)]
        [TestCase(1.5f)]
        public void AFractionOutsideZeroToOne_IsRejected(float fraction)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftBackoff(new[] { fraction }));
        }

        [Test]
        public void FractionsThatDoNotShrink_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new ShiftBackoff(new[] { 0.5f, 0.75f }));
            Assert.Throws<ArgumentException>(() => new ShiftBackoff(new[] { 0.5f, 0.5f }));
        }
    }
}
