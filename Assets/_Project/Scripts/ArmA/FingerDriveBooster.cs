using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건을 쥐고 있는 동안 손가락 관절 구동의 강성, 감쇠, 힘 한계를 배수만큼 키웠다가 놓을 때 원래대로 되돌린다.
    /// 기본 구동으로는 쥔 직후 손가락이 목표보다 몇 mm 더 닫혀 얇은 물건에 파고들기 때문에, 쥔 자리에서 밀리지 않게 단단히 잡는 데 쓴다.
    /// 목표(target)와 한계(limits)는 건드리지 않는다. 목표는 그리퍼가 정한다.
    /// </summary>
    public sealed class FingerDriveBooster
    {
        private readonly IReadOnlyList<ArticulationBody> _joints;
        private readonly float _multiplier;
        private ArticulationDrive[] _normalDrives;

        /// <param name="joints">구동을 키울 관절. 직선·회전 관절 모두 x축 구동을 쓴다.</param>
        /// <param name="multiplier">강성, 감쇠, 힘 한계에 곱할 배수(1 이상). 1이면 그대로다.</param>
        public FingerDriveBooster(IReadOnlyList<ArticulationBody> joints, float multiplier)
        {
            _joints = joints ?? throw new ArgumentNullException(nameof(joints));
            if (multiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier), "배수는 1 이상이어야 한다.");
            }

            _multiplier = multiplier;
        }

        public bool IsBoosted => _normalDrives != null;

        /// <summary>구동을 키운다. 이미 키웠으면 아무것도 하지 않는다(키운 값을 원래 값으로 잘못 저장하지 않게).</summary>
        public void Boost()
        {
            if (IsBoosted)
            {
                return;
            }

            _normalDrives = new ArticulationDrive[_joints.Count];
            for (int i = 0; i < _joints.Count; i++)
            {
                ArticulationDrive drive = _joints[i].xDrive;
                _normalDrives[i] = drive;
                _joints[i].xDrive = Scale(drive, _multiplier);
            }
        }

        /// <summary>키우기 전의 강성, 감쇠, 힘 한계로 되돌린다. 키우지 않았으면 아무것도 하지 않는다.</summary>
        public void Restore()
        {
            if (!IsBoosted)
            {
                return;
            }

            for (int i = 0; i < _joints.Count; i++)
            {
                ArticulationDrive drive = _joints[i].xDrive;
                drive.stiffness = _normalDrives[i].stiffness;
                drive.damping = _normalDrives[i].damping;
                drive.forceLimit = _normalDrives[i].forceLimit;
                _joints[i].xDrive = drive;
            }

            _normalDrives = null;
        }

        /// <summary>강성, 감쇠, 힘 한계에 배수를 곱한 구동을 돌려준다. 나머지 값은 그대로다.</summary>
        public static ArticulationDrive Scale(ArticulationDrive drive, float multiplier)
        {
            drive.stiffness *= multiplier;
            drive.damping *= multiplier;
            drive.forceLimit *= multiplier;
            return drive;
        }
    }
}
