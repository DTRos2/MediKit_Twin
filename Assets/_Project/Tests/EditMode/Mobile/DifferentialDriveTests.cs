using System;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Mobile.Tests
{
    public sealed class DifferentialDriveTests
    {
        private const float Dt = 0.02f;
        private const float MaxSpeed = 1.2f;
        private const float MaxTurnSpeed = 180f;

        private SpeedRamp _moveRamp;
        private SpeedRamp _turnRamp;
        private DifferentialDrive _drive;

        [SetUp]
        public void SetUp()
        {
            _moveRamp = new SpeedRamp(MaxSpeed, 2f);
            _turnRamp = new SpeedRamp(MaxTurnSpeed, 720f);
            _drive = new DifferentialDrive(_moveRamp, _turnRamp, new DriveTolerances(0.02f, 3f, 0.5f, 0.9f));
        }

        [Test]
        public void Step_TurnsFirst_WhenTheTargetIsBehind()
        {
            DriveStep step = _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, -5f), null, Dt);

            Assert.That(step.Action, Is.EqualTo(DriveAction.Turn));
            Assert.That(step.YawDeg, Is.GreaterThan(0f).And.LessThan(MaxTurnSpeed * Dt));
        }

        [Test]
        public void Step_TurnsTowardTheShorterSide()
        {
            // 방향 0도에서 목표가 왼쪽(-x)이면 음수 쪽으로 돈다.
            DriveStep step = _drive.Step(Vector3.zero, 0f, new Vector3(-5f, 0f, 0f), null, Dt);

            Assert.That(step.Action, Is.EqualTo(DriveAction.Turn));
            Assert.That(step.YawDeg, Is.LessThan(0f));
        }

        [Test]
        public void Step_MovesAhead_WhenAlignedWithTheTarget()
        {
            DriveStep step = _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, 5f), null, Dt);

            Assert.That(step.Action, Is.EqualTo(DriveAction.Move));
            Assert.That(step.Translation.z, Is.GreaterThan(0f));
            Assert.That(step.Translation.x, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(step.Translation.magnitude, Is.LessThanOrEqualTo(MaxSpeed * Dt));
        }

        [Test]
        public void Step_DoesNotMoveVertically_EvenWhenTheTargetIsHigher()
        {
            DriveStep step = _drive.Step(Vector3.zero, 0f, new Vector3(0f, 3f, 5f), null, Dt);

            Assert.That(step.Action, Is.EqualTo(DriveAction.Move));
            Assert.That(step.Translation.y, Is.EqualTo(0f));
        }

        [Test]
        public void Step_ArrivesWithoutSnapping_WhenThereIsNoFinalYaw()
        {
            DriveStep step = _drive.Step(Vector3.zero, 37f, new Vector3(0.01f, 0f, 0f), null, Dt);

            Assert.That(step.Action, Is.EqualTo(DriveAction.Arrive));
            Assert.That(step.SnapsYaw, Is.False);
        }

        [Test]
        public void Step_TurnsToTheFinalYaw_AfterReachingThePosition()
        {
            DriveStep step = _drive.Step(Vector3.zero, 0f, Vector3.zero, 90f, Dt);

            Assert.That(step.Action, Is.EqualTo(DriveAction.Turn));
            Assert.That(step.YawDeg, Is.GreaterThan(0f));
        }

        [Test]
        public void Step_SnapsToTheFinalYaw_WhenWithinTheFinalTolerance()
        {
            DriveStep step = _drive.Step(Vector3.zero, 89.8f, Vector3.zero, 90f, Dt);

            Assert.That(step.Action, Is.EqualTo(DriveAction.Arrive));
            Assert.That(step.SnapsYaw, Is.True);
            Assert.That(step.YawDeg, Is.EqualTo(90f));
        }

        [Test]
        public void Turning_StopsTheForwardSpeed()
        {
            _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, 5f), null, Dt);
            Assume.That(_moveRamp.Speed, Is.GreaterThan(0f));

            _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, -5f), null, Dt);

            Assert.That(_moveRamp.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void Moving_StopsTheTurnSpeed()
        {
            _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, -5f), null, Dt);
            Assume.That(_turnRamp.Speed, Is.GreaterThan(0f));

            _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, 5f), null, Dt);

            Assert.That(_turnRamp.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void Arriving_StopsBothSpeeds()
        {
            _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, 5f), null, Dt);
            Assume.That(_moveRamp.Speed, Is.GreaterThan(0f));

            _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, 0.01f), null, Dt);

            Assert.That(_moveRamp.Speed, Is.EqualTo(0f));
            Assert.That(_turnRamp.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_StopsBothSpeeds()
        {
            _drive.Step(Vector3.zero, 0f, new Vector3(0f, 0f, 5f), null, Dt);
            Assume.That(_moveRamp.Speed, Is.GreaterThan(0f));

            _drive.Reset();

            Assert.That(_moveRamp.Speed, Is.EqualTo(0f));
            Assert.That(_turnRamp.Speed, Is.EqualTo(0f));
        }

        [TestCase(0.02f)]
        [TestCase(0.0333f)]
        [TestCase(0.1f)]
        public void Run_ArrivesInsideTheTolerance_RegardlessOfTheFrameInterval(float dt)
        {
            var target = new Vector3(2f, 0f, -3f);
            const float finalYaw = 135f;
            Vector3 position = Vector3.zero;
            float yaw = 20f;
            bool arrived = false;

            for (int i = 0; i < 5000 && !arrived; i++)
            {
                DriveStep step = _drive.Step(position, yaw, target, finalYaw, dt);
                switch (step.Action)
                {
                    case DriveAction.Turn:
                        yaw = step.YawDeg;
                        break;
                    case DriveAction.Move:
                        position += step.Translation;
                        break;
                    case DriveAction.Arrive:
                        yaw = step.SnapsYaw ? step.YawDeg : yaw;
                        arrived = true;
                        break;
                }
            }

            Assert.That(arrived, Is.True, "시간 안에 도착하지 못했다");
            Assert.That(Vector3.Distance(position, target), Is.LessThanOrEqualTo(0.02f));
            Assert.That(Mathf.DeltaAngle(yaw, finalYaw), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Constructor_RejectsMissingRamps()
        {
            var tolerances = new DriveTolerances(0.02f, 3f, 0.5f, 0.9f);

            Assert.Throws<ArgumentNullException>(() => new DifferentialDrive(null, _turnRamp, tolerances));
            Assert.Throws<ArgumentNullException>(() => new DifferentialDrive(_moveRamp, null, tolerances));
        }
    }
}
