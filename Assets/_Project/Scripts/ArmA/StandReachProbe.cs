using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 지금 팔에게 "다른 자리에 서 있다면 이 자세에 닿는가", "거기서 팔이 장애물과 겹치지 않는가"를 묻는 방법을 만든다. 베이스는 움직이지 않는다.
    /// 후보 자리에 섰을 때의 팔 베이스 자세를 예측하고(ArmBasePosePredictor), 목표를 그 자세 기준으로 지금 베이스 기준에 옮겨(PoseRelocator)
    /// 지금 팔의 도달 판정(probe)에 넘긴다. 겹침 판정에는 옮긴 목표와 함께 후보 자리의 팔 베이스 자세를 넘겨, 팔이 거기 서 있을 때의 공간으로 따진다.
    /// 판정은 주입받으므로 씬 없이 시험한다.
    /// </summary>
    public sealed class StandReachProbe
    {
        private readonly Func<Pose, bool> _canReachFromCurrentBase;
        private readonly Func<Pose, Pose, bool> _isClearAtBase;
        private readonly Pose _currentArmBase;
        private readonly float _currentRootYawDeg;
        private readonly float _facingYawDeg;

        /// <param name="canReachFromCurrentBase">월드 기준 도구 자세에 지금 자리의 팔이 닿으면 true.</param>
        /// <param name="currentArmBase">팔 베이스 링크의 지금 월드 자세.</param>
        /// <param name="currentRootYawDeg">베이스 루트의 지금 방향(도).</param>
        /// <param name="facingYawDeg">후보 자리에 도착했을 때 루트가 보는 방향(도).</param>
        /// <param name="isClearAtBase">(지금 베이스 기준으로 옮긴 도구 자세, 후보 자리의 팔 베이스 자세) → 팔이 장애물과 겹치지 않으면 true. 겹침을 따지지 않으면 null.</param>
        public StandReachProbe(
            Func<Pose, bool> canReachFromCurrentBase,
            Pose currentArmBase,
            float currentRootYawDeg,
            float facingYawDeg,
            Func<Pose, Pose, bool> isClearAtBase = null)
        {
            _canReachFromCurrentBase = canReachFromCurrentBase ?? throw new ArgumentNullException(nameof(canReachFromCurrentBase));
            _isClearAtBase = isClearAtBase;
            _currentArmBase = currentArmBase;
            _currentRootYawDeg = currentRootYawDeg;
            _facingYawDeg = facingYawDeg;
        }

        /// <summary>후보 자리에 서 있다면 월드 기준 도구 자세에 닿는지 답하는 함수.</summary>
        public Func<Pose, bool> At(StandCandidate candidate)
        {
            Pose plannedBase = PlannedBase(candidate);
            return target => _canReachFromCurrentBase(PoseRelocator.Relocate(target, _currentArmBase, plannedBase));
        }

        /// <summary>후보 자리에 서 있다면 월드 기준 도구 자세에서 팔이 장애물과 겹치지 않는지 답하는 함수. 겹침을 따지지 않는 probe이면 null.</summary>
        public Func<Pose, bool> ClearAt(StandCandidate candidate)
        {
            if (_isClearAtBase == null)
            {
                return null;
            }

            Pose plannedBase = PlannedBase(candidate);
            return target => _isClearAtBase(PoseRelocator.Relocate(target, _currentArmBase, plannedBase), plannedBase);
        }

        private Pose PlannedBase(StandCandidate candidate)
        {
            return ArmBasePosePredictor.Predict(_currentArmBase, _currentRootYawDeg, candidate.ArmBasePosition, _facingYawDeg);
        }
    }
}
