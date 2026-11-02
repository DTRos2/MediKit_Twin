using System;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandPlacementCostTests
    {
        [Test]
        public void Of_AddsTheWeightedDeviationAndTheWeightedShift()
        {
            var cost = new StandPlacementCost(2f, 100f);

            Assert.That(cost.Of(15f, 0.1f), Is.EqualTo(15f * 2f + 0.1f * 100f).Within(1e-4f));
        }

        [Test]
        public void Of_WithNoDeviationAndNoShift_IsZero()
        {
            Assert.That(new StandPlacementCost(1f, 100f).Of(0f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void AZeroWeight_IgnoresThatTerm()
        {
            Assert.That(new StandPlacementCost(0f, 100f).Of(90f, 0.1f), Is.EqualTo(10f).Within(1e-4f), "방향을 비틀어도 점수에 넣지 않는다");
            Assert.That(new StandPlacementCost(1f, 0f).Of(30f, 5f), Is.EqualTo(30f).Within(1e-4f), "자리를 옮겨도 점수에 넣지 않는다");
        }

        [Test]
        public void ARaisedShiftWeight_MakesMovingTheStandMoreCostlyThanTwistingTheTool()
        {
            var cheapShift = new StandPlacementCost(1f, 10f);
            var costlyShift = new StandPlacementCost(1f, 1000f);

            // 15도 비트는 것(15)과 0.1m 옮기는 것을 견준다.
            Assert.That(cheapShift.Of(0f, 0.1f), Is.LessThan(cheapShift.Of(15f, 0f)));
            Assert.That(costlyShift.Of(0f, 0.1f), Is.GreaterThan(costlyShift.Of(15f, 0f)));
        }

        [Test]
        public void Constructor_NegativeWeight_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StandPlacementCost(-1f, 100f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StandPlacementCost(1f, -100f));
        }
    }
}
