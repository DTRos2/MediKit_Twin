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
    public class IkArmTarget : MonoBehaviour, IArmTarget, IToolPoseTarget, IToolSolveToleranceTarget, IToolReachFrame,
        IToolTrackingDiagnostics, IManualHold, IJointPoseTarget, ISeededToolSolver, IArmModelSource
    {
        // 목표 각도와 이만큼(도) 이상 다른 관절만 못 따라간 것으로 적는다.
        private const float TrackingErrorThresholdDeg = 2f;

        // 못 따라간 관절의 현재 각도가 한계에서 이만큼(도) 안이면 한계에 붙었다고 적는다.
        private const float NearLimitDeg = 3f;

        // 기준 관절 각도와 이만큼(도) 이상 다른 관절이 있으면 다른 모양으로 닿았다고 본다.
        private const float DeviationThresholdDeg = 5f;

        [Tooltip("로봇의 운동학 정보를 주는 쪽(IArmModel 구현체, 보통 ArmController).")]
        [SerializeField] private MonoBehaviour _arm;

        [Tooltip("지정하면 이 Transform의 자세를 도구 끝점 목표로 따라간다. 접근 방향이 +Z(forward)다.")]
        [SerializeField] private Transform _followTarget;

        [Tooltip("IK가 수렴하지 못해도 가장 가까운 해로 움직인다. 끄면 닿지 않는 목표에서는 제자리에 둔다.")]
        [SerializeField] private bool _moveWhenUnreachable;

        [Tooltip("목표를 풀 때마다 관절이 몇 도씩 움직이라고 나왔는지 Console에 남긴다. 어느 관절이 쓰이는지 확인할 때만 켠다.")]
        [SerializeField] private bool _logJointDeltas;

        private IArmModel _model;
        private IArmDriver _driver;
        private IInverseKinematics _solver;
        private float[] _seed;
        private float[] _solution;
        private float[] _probeSolution;
        private Pose _targetWorld;
        private IkSolveAcceptance _acceptance = IkSolveAcceptance.Strict;
        private bool _hasTarget;
        private bool _dirty;
        private bool _hasSolution;

        public IReadOnlyList<string> JointNames => _model.JointNames;
        public IArmModel Model => _model;
        public bool HasToolTarget => _hasTarget;
        public bool LastSolveSucceeded { get; private set; }
        public IkResult LastResult { get; private set; }

        private void Awake()
        {
            _model = _arm as IArmModel;
            if (_model == null)
            {
                Debug.LogError($"{name}: Arm이 IArmModel을 구현해야 한다.", this);
                enabled = false;
            }

            // 관절 각도로 직접 보내는 일에 쓴다. 구현하지 않으면 관절 이동만 쓰지 못한다.
            _driver = _arm as IArmDriver;
        }

        public bool IsHeld { get; private set; }

        // 사람이 직접 조작하는 동안에는 새 목표를 받지 않는다. 풀이가 성공하지 않은 상태로 두어, 기다리던 쪽이 곧바로 실패로 끝낸다.
        public void SetToolTarget(Pose worldPose)
        {
            SetToolTarget(worldPose, 0f, 0f);
        }

        // 느슨한 풀이 기준은 이 목표에만 붙는다. 다음 목표를 SetToolTarget(Pose)로 주면 다시 엄격해진다.
        public void SetToolTarget(Pose worldPose, float solvePositionToleranceM, float solveRotationToleranceDeg)
        {
            if (IsHeld)
            {
                LastSolveSucceeded = false;
                return;
            }

            _targetWorld = worldPose;
            _acceptance = new IkSolveAcceptance(solvePositionToleranceM, solveRotationToleranceDeg);
            _hasTarget = true;
            _dirty = true;
        }

        public void BeginHold()
        {
            if (IsHeld)
            {
                return;
            }

            // 남은 목표가 있으면 IK가 매 물리 스텝 관절 목표를 덮어쓰므로 먼저 지운다.
            ClearToolTarget();
            IsHeld = true;
        }

        public void EndHold()
        {
            IsHeld = false;
        }

        public void ClearToolTarget()
        {
            _hasTarget = false;
            _hasSolution = false;
            LastSolveSucceeded = false;
        }

        // 남은 도구 끝점 목표를 먼저 지운다. 남겨 두면 IK가 매 물리 스텝 관절 목표를 덮어써서 관절 목표가 먹히지 않는다.
        public bool SetJointTarget(IReadOnlyList<float> anglesRad)
        {
            if (IsHeld || _driver == null || anglesRad == null || anglesRad.Count != _model.JointNames.Count)
            {
                return false;
            }

            ClearToolTarget();

            var goal = new float[anglesRad.Count];
            for (int i = 0; i < goal.Length; i++)
            {
                goal[i] = anglesRad[i];
            }

            _driver.SetJointTargets(goal);
            return true;
        }

        public bool HasReachedJointTarget(float toleranceRad)
        {
            return _driver != null && _driver.IsAtTarget(toleranceRad);
        }

        public bool TrySolveFrom(Pose worldPose, IReadOnlyList<float> seedRad, float[] solutionRad)
        {
            int count = _model.JointNames.Count;
            if (seedRad == null || solutionRad == null || seedRad.Count != count || solutionRad.Length != count)
            {
                return false;
            }

            EnsureSolver();
            for (int i = 0; i < count; i++)
            {
                _seed[i] = seedRad[i];
            }

            // 목표와 현재 풀이 상태는 건드리지 않는다. 풀이 결과만 받는다.
            return _solver.Solve(ToBaseFrame(worldPose), _seed, solutionRad).Converged;
        }

        public Vector3 OffsetFromArmBase(Vector3 worldPoint)
        {
            return worldPoint - _model.BaseLink.position;
        }

        public IkResult Probe(Pose worldPose)
        {
            EnsureSolver();

            // 풀이 결과는 쓰지 않고 별도 배열에 받는다. 목표와 현재 풀이 상태는 건드리지 않는다.
            _model.GetJointPositions(_seed);
            return _solver.Solve(ToBaseFrame(worldPose), _seed, _probeSolution);
        }

        public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg)
        {
            if (!_hasTarget)
            {
                return false;
            }

            Pose tool = _model.ToolPose;
            return Vector3.Distance(tool.position, _targetWorld.position) <= positionToleranceM
                && Quaternion.Angle(tool.rotation, _targetWorld.rotation) <= rotationToleranceDeg;
        }

        public string DescribeTracking()
        {
            if (!_hasTarget || !_hasSolution)
            {
                return string.Empty;
            }

            Pose tool = _model.ToolPose;
            float positionError = Vector3.Distance(tool.position, _targetWorld.position);
            float rotationError = Quaternion.Angle(tool.rotation, _targetWorld.rotation);

            int count = _model.JointNames.Count;
            var current = new float[count];
            var lower = new float[count];
            var upper = new float[count];
            _model.GetJointPositions(current);
            _model.GetJointLimits(lower, upper);

            string joints = JointTrackingReport.Describe(
                _model.JointNames, _solution, current, lower, upper, TrackingErrorThresholdDeg, NearLimitDeg);
            return $"도구 끝점 남은 오차 {positionError:F3}m/{rotationError:F1}도, 관절 {joints}";
        }

        public string DescribeJointDeviation(IReadOnlyList<float> referenceRad)
        {
            int count = _model.JointNames.Count;
            if (referenceRad == null || referenceRad.Count != count)
            {
                return string.Empty;
            }

            var current = new float[count];
            _model.GetJointPositions(current);
            if (!JointTrackingReport.HasLagging(referenceRad, current, DeviationThresholdDeg))
            {
                return string.Empty;
            }

            var lower = new float[count];
            var upper = new float[count];
            _model.GetJointLimits(lower, upper);
            return JointTrackingReport.Describe(_model.JointNames, referenceRad, current, lower, upper, DeviationThresholdDeg, NearLimitDeg);
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
            _model.GetJointPositions(_seed);

            IkResult result = _solver.Solve(ToBaseFrame(_targetWorld), _seed, _solution);
            LastResult = result;

            // 수렴하지 못한 풀이는 현재 자세에서 먼 자세일 수 있으므로, 느슨한 기준일 때만 관절 이동량을 재서 확인한다.
            float travel = result.Converged || !_acceptance.IsLoose ? 0f : JointTravel.Largest(_seed, _solution);
            bool isUsable = _acceptance.IsUsable(result, travel, _model.IkSettings.MaxJointTravelRad);
            LastSolveSucceeded = isUsable;
            _hasSolution = isUsable || _moveWhenUnreachable;
            _dirty = false;

            if (_logJointDeltas)
            {
                string verdict = result.Converged
                    ? "수렴"
                    : $"수렴 못함(위치 오차 {result.PositionError:F3}m){(isUsable ? ", 허용 오차 안이라 사용" : string.Empty)}";
                Debug.Log($"{name}: IK {verdict}. {JointDeltaReport.Format(_model.JointNames, _seed, _solution)}", this);
            }
        }

        private Pose ToBaseFrame(Pose worldPose)
        {
            Transform baseLink = _model.BaseLink;
            return new Pose(
                baseLink.InverseTransformPoint(worldPose.position),
                Quaternion.Inverse(baseLink.rotation) * worldPose.rotation);
        }

        private void EnsureSolver()
        {
            if (_solver != null)
            {
                return;
            }

            DlsIkSettings settings = _model.IkSettings;
            _solver = new MultiStartInverseKinematics(
                new DlsInverseKinematics(_model.Chain, settings),
                _model.Chain,
                settings.Restarts,
                settings.RestartSeed,
                settings.MaxJointTravelRad);

            int count = _model.JointNames.Count;
            _seed = new float[count];
            _solution = new float[count];
            _probeSolution = new float[count];
        }
    }
}
