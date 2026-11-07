using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class IkSolveAcceptanceTests
    {
        private const float PositionToleranceM = 0.015f;
        private const float RotationToleranceDeg = 5f;
        private const float MaxTravelRad = 2f;

        private static IkSolveAcceptance Loose => new IkSolveAcceptance(PositionToleranceM, RotationToleranceDeg);

        private static IkResult Unconverged(float positionErrorM, float rotationErrorDeg)
        {
            return new IkResult(false, positionErrorM, rotationErrorDeg, 60);
        }

        [Test]
        public void Strict_UsesAConvergedSolution()
        {
            Assert.That(IkSolveAcceptance.Strict.IsUsable(new IkResult(true, 0.001f, 0.1f, 5), 0f, MaxTravelRad), Is.True);
        }

        [Test]
        public void Strict_RejectsAnUnconvergedSolution_EvenWhenTheErrorIsTiny()
        {
            Assert.That(IkSolveAcceptance.Strict.IsUsable(Unconverged(0.0021f, 0.1f), 0f, MaxTravelRad), Is.False);
        }

        [Test]
        public void Loose_UsesAConvergedSolution_WithoutLookingAtTheTravel()
        {
            // 수렴한 풀이의 이동량은 IK가 이미 제한 안으로 걸러 준다.
            Assert.That(Loose.IsUsable(new IkResult(true, 0.001f, 0.1f, 5), MaxTravelRad * 10f, MaxTravelRad), Is.True);
        }

        [Test]
        public void Loose_UsesAnUnconvergedSolution_WhenTheErrorIsInsideBothTolerances()
        {
            Assert.That(Loose.IsUsable(Unconverged(0.0136f, 0.2f), 1f, MaxTravelRad), Is.True);
        }

        [Test]
        public void Loose_UsesAnUnconvergedSolution_WhenTheErrorIsExactlyTheTolerance()
        {
            Assert.That(Loose.IsUsable(Unconverged(PositionToleranceM, RotationToleranceDeg), 1f, MaxTravelRad), Is.True);
        }

        [Test]
        public void Loose_RejectsAnUnconvergedSolution_WhenThePositionErrorIsTooBig()
        {
            Assert.That(Loose.IsUsable(Unconverged(PositionToleranceM + 0.001f, 0.2f), 1f, MaxTravelRad), Is.False);
        }

        [Test]
        public void Loose_RejectsAnUnconvergedSolution_WhenTheRotationErrorIsTooBig()
        {
            Assert.That(Loose.IsUsable(Unconverged(0.004f, RotationToleranceDeg + 0.5f), 1f, MaxTravelRad), Is.False);
        }

        [Test]
        public void Loose_RejectsAnUnconvergedSolution_WhenAJointWouldTurnPastTheLimit()
        {
            // 오차가 작아도 팔이 뒤집힌 먼 자세라면 쓰지 않는다.
            Assert.That(Loose.IsUsable(Unconverged(0.001f, 0.1f), MaxTravelRad + 0.1f, MaxTravelRad), Is.False);
        }

        [Test]
        public void Loose_UsesAnUnconvergedSolution_WhenThereIsNoTravelLimit()
        {
            Assert.That(Loose.IsUsable(Unconverged(0.004f, 0.1f), 100f, 0f), Is.True);
        }

        [TestCase(0f, 5f)]
        [TestCase(0.015f, 0f)]
        [TestCase(0f, 0f)]
        public void IsLoose_IsFalse_WhenEitherToleranceIsZero(float positionToleranceM, float rotationToleranceDeg)
        {
            var acceptance = new IkSolveAcceptance(positionToleranceM, rotationToleranceDeg);

            Assert.That(acceptance.IsLoose, Is.False);
            Assert.That(acceptance.IsUsable(Unconverged(0.001f, 0.1f), 0f, MaxTravelRad), Is.False);
        }

        [Test]
        public void Strict_IsNotLoose()
        {
            Assert.That(IkSolveAcceptance.Strict.IsLoose, Is.False);
        }
    }
}
