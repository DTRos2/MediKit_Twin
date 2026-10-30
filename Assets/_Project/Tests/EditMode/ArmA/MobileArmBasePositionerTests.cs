using System;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class MobileArmBasePositionerTests
    {
        private const float Tolerance = 0.03f;
        private const int MaxCorrections = 3;
        private const float TimeoutSeconds = 30f;

        private float _time;
        private FakeMobileBase _mobile;

        [SetUp]
        public void SetUp()
        {
            _time = 0f;
            _mobile = new FakeMobileBase();
        }

        // 베이스 루트 위치와, 팔 베이스가 루트에서 떨어진 정도(베이스가 돌면 달라질 수 있다)를 흉내 낸다.
        private sealed class FakeMobileBase : IMobileBase, IArmBaseLocator
        {
            private Vector3 _pending;

            internal Vector3 ArmOffset = new Vector3(0f, 0f, -0.6f);

            /// <summary>베이스가 한 번 도착할 때마다 팔 오프셋이 이만큼 바뀐다(회전으로 팔 베이스가 어긋나는 상황).</summary>
            internal Vector3 OffsetDriftPerArrival;

            internal bool NeverArrives;
            internal int DriveCount;
            internal int StopCount;

            public bool IsMoving { get; private set; }

            public Vector3 Position { get; private set; }

            public Vector3 BaseWorldPosition => Position + ArmOffset;

            public event Action Arrived;

            public void DriveTo(Vector3 worldPosition)
            {
                DriveCount++;
                _pending = worldPosition;
                IsMoving = true;
            }

            public void Stop()
            {
                StopCount++;
                IsMoving = false;
            }

            /// <summary>한 프레임 흐름. 이동 중이면 도착한다.</summary>
            internal void Step()
            {
                if (!IsMoving || NeverArrives)
                {
                    return;
                }

                Position = new Vector3(_pending.x, Position.y, _pending.z);
                ArmOffset += OffsetDriftPerArrival;
                IsMoving = false;
                Arrived?.Invoke();
            }
        }

        private bool Move(Vector3 target, int maxCorrections = MaxCorrections)
        {
            var positioner = new MobileArmBasePositioner(_mobile, _mobile, () => _time, Tolerance, maxCorrections, TimeoutSeconds);
            bool result = false;

            CoroutineRunner.Drain(positioner.MoveArmBaseTo(target, done => result = done), () =>
            {
                _time += 1f;
                _mobile.Step();
            });
            return result;
        }

        private float ArmErrorTo(Vector3 target)
        {
            Vector3 difference = target - _mobile.BaseWorldPosition;
            return new Vector3(difference.x, 0f, difference.z).magnitude;
        }

        [Test]
        public void AlreadyThere_SucceedsWithoutDriving()
        {
            bool result = Move(_mobile.BaseWorldPosition);

            Assert.That(result, Is.True);
            Assert.That(_mobile.DriveCount, Is.EqualTo(0));
        }

        [Test]
        public void DrivesTheBaseRootByTheArmBaseError_NotToTheTargetItself()
        {
            // 팔 베이스는 루트에서 0.6m 떨어져 있다. 목표 (3, 2)에 팔 베이스를 놓으려면 루트는 (3, 2.6)으로 가야 한다.
            var target = new Vector3(3f, 0f, 2f);

            bool result = Move(target);

            Assert.That(result, Is.True);
            Assert.That(_mobile.DriveCount, Is.EqualTo(1));
            Assert.That(ArmErrorTo(target), Is.LessThanOrEqualTo(Tolerance));
            Assert.That(_mobile.Position.z, Is.EqualTo(2.6f).Within(1e-4f));
        }

        [Test]
        public void IgnoresTheTargetHeight()
        {
            bool result = Move(new Vector3(1f, 99f, 1f));

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenTurningShiftsTheArmBase_CorrectsByDrivingAgain()
        {
            // 첫 이동 뒤 베이스가 돌아 팔 베이스가 0.6m 옆으로 어긋난다. 한 번 더 보정하면 닿는다.
            _mobile.OffsetDriftPerArrival = new Vector3(0.6f, 0f, 0.6f);
            var target = new Vector3(3f, 0f, 2f);

            // 어긋남은 첫 도착에서만 생기고 그 뒤에는 안정된다.
            _mobile.Arrived += () => _mobile.OffsetDriftPerArrival = Vector3.zero;

            bool result = Move(target);

            Assert.That(result, Is.True);
            Assert.That(_mobile.DriveCount, Is.EqualTo(2), "처음 이동 1번과 보정 1번");
            Assert.That(ArmErrorTo(target), Is.LessThanOrEqualTo(Tolerance));
        }

        [Test]
        public void WhenTheBaseNeverArrives_TimesOutStopsAndFails()
        {
            _mobile.NeverArrives = true;

            bool result = Move(new Vector3(3f, 0f, 2f));

            Assert.That(result, Is.False);
            Assert.That(_mobile.StopCount, Is.EqualTo(1));
            Assert.That(_time, Is.GreaterThan(TimeoutSeconds));
        }

        [Test]
        public void WhenTheErrorNeverShrinks_GivesUpAfterTheCorrectionLimit()
        {
            // 도착할 때마다 팔이 계속 어긋나서 수렴하지 못하는 경우. 무한히 돌지 않고 실패로 끝낸다.
            _mobile.OffsetDriftPerArrival = new Vector3(1f, 0f, 1f);

            bool result = Move(new Vector3(3f, 0f, 2f), maxCorrections: 2);

            Assert.That(result, Is.False);
            Assert.That(_mobile.DriveCount, Is.EqualTo(3), "처음 이동 1번과 보정 2번");
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MobileArmBasePositioner(null, _mobile, () => 0f, Tolerance, 1, 1f));
            Assert.Throws<ArgumentNullException>(() => new MobileArmBasePositioner(_mobile, null, () => 0f, Tolerance, 1, 1f));
            Assert.Throws<ArgumentNullException>(() => new MobileArmBasePositioner(_mobile, _mobile, null, Tolerance, 1, 1f));
        }
    }
}
