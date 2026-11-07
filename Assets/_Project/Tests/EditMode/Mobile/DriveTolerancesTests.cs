using System;
using NUnit.Framework;

namespace MediKitTwin.Mobile.Tests
{
    public sealed class DriveTolerancesTests
    {
        [Test]
        public void Constructor_KeepsTheGivenValues()
        {
            var tolerances = new DriveTolerances(0.02f, 3f, 0.5f, 0.9f);

            Assert.That(tolerances.ArriveM, Is.EqualTo(0.02f));
            Assert.That(tolerances.AlignDeg, Is.EqualTo(3f));
            Assert.That(tolerances.FinalYawDeg, Is.EqualTo(0.5f));
            Assert.That(tolerances.StopMarginRatio, Is.EqualTo(0.9f));
        }

        [Test]
        public void Constructor_AllowsAStopMarginOfExactlyOne()
        {
            Assert.DoesNotThrow(() => new DriveTolerances(0.02f, 3f, 0.5f, 1f));
        }

        [TestCase(0f, 3f, 0.5f, 0.9f)]
        [TestCase(-0.02f, 3f, 0.5f, 0.9f)]
        [TestCase(0.02f, 0f, 0.5f, 0.9f)]
        [TestCase(0.02f, 3f, 0f, 0.9f)]
        [TestCase(0.02f, 3f, 0.5f, 0f)]
        [TestCase(0.02f, 3f, 0.5f, 1.01f)]
        public void Constructor_RejectsValuesThatCannotWork(float arriveM, float alignDeg, float finalYawDeg, float stopMarginRatio)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DriveTolerances(arriveM, alignDeg, finalYawDeg, stopMarginRatio));
        }
    }
}
