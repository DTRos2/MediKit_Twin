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
        private const float MinHeadingTravel = 0.05f;
        private const int SettleFrames = 2;

        private float _time;
        private FakeMobileBase _mobile;

        [SetUp]
        public void SetUp()
        {
            _time = 0f;
            _mobile = new FakeMobileBase();
        }

        // 베이스 루트 위치와 방향, 루트 기준으로 고정된 팔 베이스 자리를 흉내 낸다. 루트가 돌면 팔 베이스가 반지름만큼 휘둘린다.
        private sealed class FakeMobileBase : IMobileBase, IArmBaseLocator
        {
            private Vector3 _pending;
            private float _pendingYaw;
            private bool _hasPendingYaw;

            /// <summary>루트 기준 팔 베이스의 위치.</summary>
            internal Vector3 ArmOffsetInRoot = new Vector3(0f, 0f, -0.6f);

            /// <summary>베이스가 한 번 도착할 때마다 팔 오프셋이 이만큼 바뀐다(모델에 없는 미끄러짐 같은 어긋남).</summary>
            internal Vector3 OffsetDriftPerArrival;

            internal bool NeverArrives;
            internal int DriveCount;
            internal int StopCount;

            public bool IsMoving { get; private set; }

            public Vector3 Position { get; private set; }

            public float YawDeg { get; private set; }

            public Vector3 BaseWorldPosition => Position + Quaternion.Euler(0f, YawDeg, 0f) * ArmOffsetInRoot;

            public event Action Arrived;

            public void DriveTo(Vector3 worldPosition)
            {
                DriveCount++;
                _pending = worldPosition;
                _hasPendingYaw = false;
                IsMoving = true;
            }

            public void DriveTo(Vector3 worldPosition, float finalYawDeg)
            {
                DriveCount++;
                _pending = worldPosition;
                _pendingYaw = finalYawDeg;
                _hasPendingYaw = true;
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
                if (_hasPendingYaw)
                {
                    YawDeg = _pendingYaw;
                }

                ArmOffsetInRoot += OffsetDriftPerArrival;
                IsMoving = false;
                Arrived?.Invoke();
            }
        }

        private string _lastFailureReason = string.Empty;

        private bool Move(Vector3 target, int maxCorrections = MaxCorrections)
        {
            var positioner = new MobileArmBasePositioner(
                _mobile, _mobile, new MountedBaseGoalPlanner(MinHeadingTravel), () => _time, Tolerance, maxCorrections, TimeoutSeconds, SettleFrames);
            bool result = false;

            CoroutineRunner.Drain(positioner.MoveArmBaseTo(target, done => result = done), () =>
            {
                _time += 1f;
                _mobile.Step();
            });
            _lastFailureReason = positioner.LastFailureReason;
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
        public void ReachesTheTargetInOneMove_EvenThoughTheArmBaseSwingsWhenTheRootTurns()
        {
            // 팔 베이스가 루트 뒤 0.6m에 있다. 목표가 옆(+x)이면 루트는 90도 돌아야 하고, 그러면 팔 베이스가 크게 휘둘린다.
            // 도착 방향까지 정해 보내므로 보정 없이 한 번에 닿아야 한다.
            var target = new Vector3(3f, 0f, 0f);

            bool result = Move(target);

            Assert.That(result, Is.True);
            Assert.That(_mobile.DriveCount, Is.EqualTo(1));
            Assert.That(ArmErrorTo(target), Is.LessThanOrEqualTo(Tolerance));
        }

        [Test]
        public void ReachesTargetsInEveryDirection_WithoutCorrections()
        {
            foreach (Vector3 target in new[] { new Vector3(3f, 0f, 2f), new Vector3(-2f, 0f, 1f), new Vector3(0f, 0f, -3f), new Vector3(-1f, 0f, -1f) })
            {
                SetUp();

                bool result = Move(target);

                Assert.That(result, Is.True, $"목표 {target}");
                Assert.That(_mobile.DriveCount, Is.EqualTo(1), $"목표 {target}");
                Assert.That(ArmErrorTo(target), Is.LessThanOrEqualTo(Tolerance), $"목표 {target}");
            }
        }

        [Test]
        public void IgnoresTheTargetHeight()
        {
            bool result = Move(new Vector3(1f, 99f, 1f));

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenSomethingShiftsTheArmBaseAfterArriving_CorrectsByDrivingAgain()
        {
            // 첫 도착에서만 팔 베이스가 0.2m 어긋난다(모델에 없는 요인). 한 번 더 보정하면 닿는다.
            _mobile.OffsetDriftPerArrival = new Vector3(0.2f, 0f, 0f);
            _mobile.Arrived += () => _mobile.OffsetDriftPerArrival = Vector3.zero;
            var target = new Vector3(3f, 0f, 2f);

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
            Assert.That(_lastFailureReason, Does.Contain("끝나지 않았다"), "시간 초과임이 이유에 드러나야 한다");
        }

        [Test]
        public void WhenTheErrorNeverShrinks_GivesUpAfterTheCorrectionLimit()
        {
            // 도착할 때마다 팔이 계속 어긋나서 수렴하지 못하는 경우. 무한히 돌지 않고 실패로 끝낸다.
            _mobile.OffsetDriftPerArrival = new Vector3(1f, 0f, 1f);

            bool result = Move(new Vector3(3f, 0f, 2f), maxCorrections: 2);

            Assert.That(result, Is.False);
            Assert.That(_mobile.DriveCount, Is.EqualTo(3), "처음 이동 1번과 보정 2번");
            Assert.That(_lastFailureReason, Does.Contain("오차"), "수렴하지 못했음이 이유에 드러나야 한다");
        }

        [Test]
        public void Success_LeavesTheFailureReasonEmpty()
        {
            Move(new Vector3(3f, 0f, 2f));

            Assert.That(_lastFailureReason, Is.Empty);
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            var planner = new MountedBaseGoalPlanner(MinHeadingTravel);

            Assert.Throws<ArgumentNullException>(() => new MobileArmBasePositioner(null, _mobile, planner, () => 0f, Tolerance, 1, 1f, SettleFrames));
            Assert.Throws<ArgumentNullException>(() => new MobileArmBasePositioner(_mobile, null, planner, () => 0f, Tolerance, 1, 1f, SettleFrames));
            Assert.Throws<ArgumentNullException>(() => new MobileArmBasePositioner(_mobile, _mobile, null, () => 0f, Tolerance, 1, 1f, SettleFrames));
            Assert.Throws<ArgumentNullException>(() => new MobileArmBasePositioner(_mobile, _mobile, planner, null, Tolerance, 1, 1f, SettleFrames));
        }
    }
}
