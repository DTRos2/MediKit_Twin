using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// IMobileBase(굴러가는 베이스)로 팔 베이스를 목표 지점에 가져다 놓는다.
    /// 팔 베이스는 루트에서 떨어져 있어 루트가 돌면 어긋나므로, 도착 방향까지 정해서 루트가 설 자리를 MountedBaseGoalPlanner로 구해
    /// 보낸다. 그래도 남는 오차(미끄러짐 등)는 다시 재서 정해진 횟수까지 보정한다. 도착을 못 하거나 수렴하지 못하면 실패로 끝낸다.
    /// 시간은 주입받아 시험에서 흐름을 직접 정한다.
    /// </summary>
    public sealed class MobileArmBasePositioner : IArmBasePositioner
    {
        public const float DefaultFacingToleranceDeg = 3f;

        private readonly IMobileBase _mobile;
        private readonly IArmBaseLocator _armBase;
        private readonly MountedBaseGoalPlanner _goalPlanner;
        private readonly Func<float> _now;
        private readonly float _toleranceM;
        private readonly int _maxCorrections;
        private readonly float _timeoutSeconds;
        private readonly int _settleFrames;
        private readonly float _facingToleranceDeg;

        /// <param name="facingToleranceDeg">도착 방향을 정해 준 이동에서, 베이스 방향이 이 각도 안이면 맞은 것으로 본다.</param>
        public MobileArmBasePositioner(
            IMobileBase mobile,
            IArmBaseLocator armBase,
            MountedBaseGoalPlanner goalPlanner,
            Func<float> now,
            float toleranceM,
            int maxCorrections,
            float timeoutSeconds,
            int settleFrames,
            float facingToleranceDeg = DefaultFacingToleranceDeg)
        {
            _facingToleranceDeg = facingToleranceDeg;
            _mobile = mobile ?? throw new ArgumentNullException(nameof(mobile));
            _armBase = armBase ?? throw new ArgumentNullException(nameof(armBase));
            _goalPlanner = goalPlanner ?? throw new ArgumentNullException(nameof(goalPlanner));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _toleranceM = toleranceM;
            _maxCorrections = maxCorrections;
            _timeoutSeconds = timeoutSeconds;
            _settleFrames = settleFrames;
        }

        public string LastFailureReason { get; private set; } = string.Empty;

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            return Move(worldPosition, null, done);
        }

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, float facingYawDeg, Action<bool> done)
        {
            return Move(worldPosition, facingYawDeg, done);
        }

        // facingYawDeg가 없으면 도착 방향은 이동 방향이다. 있으면 위치뿐 아니라 그 방향까지 맞아야 도착으로 본다.
        private IEnumerator Move(Vector3 worldPosition, float? facingYawDeg, Action<bool> done)
        {
            LastFailureReason = string.Empty;

            // 시작 전에도 기다려 팔이 베이스를 따라와 자리를 잡은 값으로 재게 한다.
            yield return Settle();

            // 처음 이동 1번에 보정 최대 횟수를 더한 만큼 보낸다.
            for (int attempt = 0; attempt <= _maxCorrections; attempt++)
            {
                if (IsArrived(worldPosition, facingYawDeg))
                {
                    done(true);
                    yield break;
                }

                BaseGoal goal = PlanGoal(worldPosition, facingYawDeg);
                _mobile.DriveTo(goal.Position, goal.YawDeg);

                float startedAt = _now();
                while (_mobile.IsMoving)
                {
                    if (_now() - startedAt > _timeoutSeconds)
                    {
                        _mobile.Stop();
                        LastFailureReason = $"{_timeoutSeconds:0}초가 지나도록 베이스 이동이 끝나지 않았다(보정 {attempt}회째)";
                        done(false);
                        yield break;
                    }

                    yield return null;
                }

                // 도착한 직후에는 관절 로봇의 위치가 아직 따라오지 못했을 수 있으므로 몇 프레임 기다린 뒤에 다시 잰다.
                yield return Settle();
            }

            float remaining = FlatError(worldPosition).magnitude;
            bool isArrived = IsArrived(worldPosition, facingYawDeg);
            if (!isArrived)
            {
                LastFailureReason = $"보정 {_maxCorrections}회 뒤에도 팔 베이스 오차 {remaining:0.000}m가 허용치 {_toleranceM:0.000}m보다 크거나 방향이 맞지 않는다";
            }

            done(isArrived);
        }

        private BaseGoal PlanGoal(Vector3 worldPosition, float? facingYawDeg)
        {
            return facingYawDeg.HasValue
                ? _goalPlanner.Plan(_mobile.Position, _mobile.YawDeg, _armBase.BaseWorldPosition, worldPosition, facingYawDeg.Value)
                : _goalPlanner.Plan(_mobile.Position, _mobile.YawDeg, _armBase.BaseWorldPosition, worldPosition);
        }

        private bool IsArrived(Vector3 worldPosition, float? facingYawDeg)
        {
            if (FlatError(worldPosition).magnitude > _toleranceM)
            {
                return false;
            }

            return !facingYawDeg.HasValue
                || Mathf.Abs(Mathf.DeltaAngle(_mobile.YawDeg, facingYawDeg.Value)) <= _facingToleranceDeg;
        }

        private IEnumerator Settle()
        {
            for (int frame = 0; frame < _settleFrames; frame++)
            {
                yield return null;
            }
        }

        // 팔 베이스에서 목표까지의 수평 오차. 높이는 베이스가 바꿀 수 없으므로 뺀다.
        private Vector3 FlatError(Vector3 target)
        {
            Vector3 difference = target - _armBase.BaseWorldPosition;
            return new Vector3(difference.x, 0f, difference.z);
        }
    }
}
