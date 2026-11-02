using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ManualBaseMotionTests
    {
        private const float MoveSpeed = 1f;
        private const float TurnSpeed = 90f;
        private const float Fast = 4f;

        private static ManualBaseMotion Create() => new ManualBaseMotion(MoveSpeed, TurnSpeed, Fast);

        private static BasePlacement At(float x, float z, float yawDeg) => new BasePlacement(new Vector3(x, 0.3f, z), yawDeg);

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(1e-4f), $"기대 {expected}, 실제 {actual}");
        }

        [Test]
        public void Forward_MovesAlongTheDirectionTheBaseIsFacing()
        {
            BasePlacement facingZ = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(1f, 0f, 0f, false), 0.5f);
            BasePlacement facingX = Create().Advance(At(0f, 0f, 90f), new BaseMoveInput(1f, 0f, 0f, false), 0.5f);

            AssertVector(facingZ.Position, new Vector3(0f, 0.3f, 0.5f));
            AssertVector(facingX.Position, new Vector3(0.5f, 0.3f, 0f));
        }

        [Test]
        public void Back_MovesOppositeToTheFacing()
        {
            BasePlacement moved = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(-1f, 0f, 0f, false), 1f);

            AssertVector(moved.Position, new Vector3(0f, 0.3f, -1f));
        }

        [Test]
        public void Right_MovesToTheRightOfTheFacing()
        {
            BasePlacement facingZ = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(0f, 1f, 0f, false), 1f);
            BasePlacement facingX = Create().Advance(At(0f, 0f, 90f), new BaseMoveInput(0f, 1f, 0f, false), 1f);

            AssertVector(facingZ.Position, new Vector3(1f, 0.3f, 0f));
            AssertVector(facingX.Position, new Vector3(0f, 0.3f, -1f));
        }

        [Test]
        public void Diagonal_IsNotFasterThanStraight()
        {
            BasePlacement moved = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(1f, 1f, 0f, false), 1f);

            Assert.That(Vector3.Distance(moved.Position, new Vector3(0f, 0.3f, 0f)), Is.EqualTo(MoveSpeed).Within(1e-4f));
        }

        [Test]
        public void TheDistanceMovedGrowsWithTheTimePassed()
        {
            BasePlacement half = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(1f, 0f, 0f, false), 0.1f);
            BasePlacement full = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(1f, 0f, 0f, false), 0.2f);

            Assert.That(full.Position.z, Is.EqualTo(half.Position.z * 2f).Within(1e-5f));
        }

        [Test]
        public void NoInput_LeavesThePlacementAsItWas()
        {
            BasePlacement start = At(1f, 2f, 45f);

            BasePlacement after = Create().Advance(start, new BaseMoveInput(0f, 0f, 0f, false), 1f);

            AssertVector(after.Position, start.Position);
            Assert.That(after.YawDeg, Is.EqualTo(45f).Within(1e-4f));
        }

        [Test]
        public void TheHeightIsNeverChanged()
        {
            BasePlacement after = Create().Advance(At(0f, 0f, 30f), new BaseMoveInput(1f, 1f, 1f, true), 1f);

            Assert.That(after.Position.y, Is.EqualTo(0.3f));
        }

        [Test]
        public void Turn_RightRaisesTheYaw_LeftLowersIt()
        {
            BasePlacement right = Create().Advance(At(0f, 0f, 10f), new BaseMoveInput(0f, 0f, 1f, false), 0.5f);
            BasePlacement left = Create().Advance(At(0f, 0f, 100f), new BaseMoveInput(0f, 0f, -1f, false), 0.5f);

            Assert.That(right.YawDeg, Is.EqualTo(55f).Within(1e-3f));
            Assert.That(left.YawDeg, Is.EqualTo(55f).Within(1e-3f));
        }

        [Test]
        public void Turn_PastAFullCircle_WrapsBackIntoZeroTo360()
        {
            BasePlacement past = Create().Advance(At(0f, 0f, 359f), new BaseMoveInput(0f, 0f, 1f, false), 0.0333333f);
            BasePlacement before = Create().Advance(At(0f, 0f, 1f), new BaseMoveInput(0f, 0f, -1f, false), 0.0333333f);

            Assert.That(past.YawDeg, Is.EqualTo(2f).Within(0.05f));
            Assert.That(before.YawDeg, Is.EqualTo(358f).Within(0.05f));
        }

        [Test]
        public void Fast_MultipliesBothTheMoveAndTheTurn()
        {
            BasePlacement normal = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(1f, 0f, 1f, false), 0.1f);
            BasePlacement fast = Create().Advance(At(0f, 0f, 0f), new BaseMoveInput(1f, 0f, 1f, true), 0.1f);

            Assert.That(fast.Position.z, Is.EqualTo(normal.Position.z * Fast).Within(1e-4f));
            Assert.That(fast.YawDeg, Is.EqualTo(normal.YawDeg * Fast).Within(1e-3f));
        }

        [Test]
        public void Constructor_InvalidSpeeds_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManualBaseMotion(0f, 90f, 4f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManualBaseMotion(1f, 0f, 4f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManualBaseMotion(1f, 90f, 0.5f));
        }
    }
}
