using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 도구 끝점의 목표 자세를 IK로 풀어 관절 목표 각도(IArmTarget)로 넘긴다. Unity 단독 제어 경로(FR-17 필수 구현체)다.
    /// 목표는 SetToolTarget으로 주거나 Follow Target Transform을 지정해 따라가게 한다.
    /// ArmController의 Target Source로 지정한다.
    /// </summary>
    public class IkArmTarget : MonoBehaviour, IArmTarget, IToolPoseTarget
    {
        [SerializeField] private ArmController _arm;

        [Tooltip("지정하면 이 Transform의 자세를 도구 끝점 목표로 따라간다. 접근 방향이 +Z(forward)다.")]
        [SerializeField] private Transform _followTarget;

        [Tooltip("IK가 수렴하지 못해도 가장 가까운 해로 움직인다. 끄면 닿지 않는 목표에서는 제자리에 둔다.")]
        [SerializeField] private bool _moveWhenUnreachable;

        private IInverseKinematics _solver;
        private float[] _seed;
        private float[] _solution;
        private Pose _targetWorld;
        private bool _hasTarget;
        private bool _dirty;
        private bool _hasSolution;

        public IReadOnlyList<string> JointNames => _arm.JointNames;
        public bool HasToolTarget => _hasTarget;
        public bool LastSolveSucceeded { get; private set; }
        public IkResult LastResult { get; private set; }

        public void SetToolTarget(Pose worldPose)
        {
            _targetWorld = worldPose;
            _hasTarget = true;
            _dirty = true;
        }

        public void ClearToolTarget()
        {
            _hasTarget = false;
            _hasSolution = false;
            LastSolveSucceeded = false;
        }

        public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg)
        {
            if (!_hasTarget)
            {
                return false;
            }

            Pose tool = _arm.ToolPose;
            return Vector3.Distance(tool.position, _targetWorld.position) <= positionToleranceM
                && Quaternion.Angle(tool.rotation, _targetWorld.rotation) <= rotationToleranceDeg;
        }

        public bool TryGetTargets(float[] anglesRad)
        {
            if (_followTarget != null)
            {
                FollowTransform();
            }

            if (!_hasTarget)
            {
                return false;
            }

            if (_dirty)
            {
                Solve();
            }

            if (!_hasSolution)
            {
                return false;
            }

            System.Array.Copy(_solution, anglesRad, _solution.Length);
            return true;
        }

        private void FollowTransform()
        {
            var pose = new Pose(_followTarget.position, _followTarget.rotation);
            bool changed = !_hasTarget
                || (pose.position - _targetWorld.position).sqrMagnitude > 1e-8f
                || Quaternion.Angle(pose.rotation, _targetWorld.rotation) > 1e-3f;

            if (changed)
            {
                SetToolTarget(pose);
            }
        }

        private void Solve()
        {
            EnsureSolver();

            // 시드는 현재 자세다. 가까운 해를 찾아 팔이 갑자기 다른 자세로 뒤집히는 것을 막는다.
            _arm.GetJointPositions(_seed);

            Transform baseLink = _arm.BaseLink;
            var targetInBase = new Pose(
                baseLink.InverseTransformPoint(_targetWorld.position),
                Quaternion.Inverse(baseLink.rotation) * _targetWorld.rotation);

            IkResult result = _solver.Solve(targetInBase, _seed, _solution);
            LastResult = result;
            LastSolveSucceeded = result.Converged;
            _hasSolution = result.Converged || _moveWhenUnreachable;
            _dirty = false;
        }

        private void EnsureSolver()
        {
            if (_solver != null)
            {
                return;
            }

            DlsIkSettings settings = _arm.Profile.Ik;
            _solver = new MultiStartInverseKinematics(
                new DlsInverseKinematics(_arm.Chain, settings),
                _arm.Chain,
                settings.Restarts,
                settings.RestartSeed);
            _seed = new float[_arm.JointCount];
            _solution = new float[_arm.JointCount];
        }
    }
}
