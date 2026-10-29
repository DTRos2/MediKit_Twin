using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 관절들의 구동을 맡는다. 목표(goal)를 향해 최대 속도로 명령(command)을 옮기고, 그 값을 ArticulationBody의 구동 목표로 쓴다.
    /// 밖으로 주고받는 각도는 모두 URDF 기준이고, 실제 ArticulationBody 값과의 기준 차이(offset)를 여기서 보정한다.
    /// 링크를 찾는 일(ArmRig), 기준 차이를 알아내는 일(ReferenceOffsetCalibrator), 순기구학은 다른 클래스가 맡는다.
    /// </summary>
    public sealed class JointBank
    {
        private readonly ArticulationBody[] _joints;
        private readonly float[] _command;
        private readonly float[] _goal;
        private readonly float[] _lowerRad;
        private readonly float[] _upperRad;
        private readonly float[] _offsetRad;

        public JointBank(ArticulationBody[] joints)
        {
            _joints = joints;
            int count = joints.Length;
            _command = new float[count];
            _goal = new float[count];
            _lowerRad = new float[count];
            _upperRad = new float[count];
            _offsetRad = new float[count];

            for (int i = 0; i < count; i++)
            {
                _lowerRad[i] = ArticulationJointLimits.LowerRad(joints[i]);
                _upperRad[i] = ArticulationJointLimits.UpperRad(joints[i]);
            }
        }

        public int Count => _joints.Length;

        /// <summary>목표 각도 버퍼(URDF 기준, rad). IArmTarget이 이 배열을 직접 채운다.</summary>
        public float[] Goal => _goal;

        /// <summary>URDF 각도 = ArticulationBody 값 + offset. 기준 판별이 끝난 뒤 한 번 정한다.</summary>
        public void SetOffsets(float[] offsetsRad)
        {
            Array.Copy(offsetsRad, _offsetRad, _offsetRad.Length);
        }

        /// <summary>명령과 목표를 지금 관절 각도로 맞춘다. 초기화 직후 갑자기 움직이지 않게 한다.</summary>
        public void SyncToCurrent()
        {
            ReadAngles(_command);
            Array.Copy(_command, _goal, _command.Length);
        }

        public void ReadAngles(float[] urdfAnglesRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                urdfAnglesRad[i] = _joints[i].jointPosition[0] + _offsetRad[i];
            }
        }

        public void ReadVelocities(float[] velocitiesRadPerSec)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                velocitiesRadPerSec[i] = _joints[i].jointVelocity[0];
            }
        }

        public void GetLimits(float[] lowerRad, float[] upperRad)
        {
            Array.Copy(_lowerRad, lowerRad, _lowerRad.Length);
            Array.Copy(_upperRad, upperRad, _upperRad.Length);
        }

        public void SetGoal(float[] urdfAnglesRad)
        {
            Array.Copy(urdfAnglesRad, _goal, _goal.Length);
        }

        /// <summary>명령을 목표 쪽으로 maxStepRad만큼 옮기고 구동 목표에 반영한다. 매 FixedUpdate에 한 번 부른다.</summary>
        public void Step(float maxStepRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                float goal = Mathf.Clamp(_goal[i], _lowerRad[i], _upperRad[i]);
                _command[i] = Mathf.MoveTowards(_command[i], goal, maxStepRad);
                ApplyDriveTarget(i, _command[i]);
            }
        }

        /// <summary>관절을 즉시 이 각도로 옮기고 속도를 0으로 만든다.</summary>
        public void ResetTo(float[] urdfAnglesRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                float angle = Mathf.Clamp(urdfAnglesRad[i], _lowerRad[i], _upperRad[i]);

                _joints[i].jointPosition = new ArticulationReducedSpace(angle - _offsetRad[i]);
                _joints[i].jointVelocity = new ArticulationReducedSpace(0f);
                ApplyDriveTarget(i, angle);

                _command[i] = angle;
                _goal[i] = angle;
            }
        }

        public bool IsAtGoal(float toleranceRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                float current = _joints[i].jointPosition[0] + _offsetRad[i];
                if (Mathf.Abs(current - _goal[i]) > toleranceRad)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyDriveTarget(int index, float urdfAngle)
        {
            ArticulationDrive drive = _joints[index].xDrive;
            drive.target = (urdfAngle - _offsetRad[index]) * Mathf.Rad2Deg;
            _joints[index].xDrive = drive;
        }
    }
}
