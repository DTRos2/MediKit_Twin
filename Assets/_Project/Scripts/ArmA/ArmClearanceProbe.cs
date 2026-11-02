using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// "도구가 이 자세에 있을 때 팔이 장애물과 겹치지 않는가"를 답한다. 도구 자세를 출발 자세(시드)에서 관절 각도로 풀고(ISeededToolSolver),
    /// 그 관절 각도에서 팔이 차지하는 공간을 ArmCollisionChecker로 따진다.
    /// 닿는지(도달) 자체는 따지지 않는다. 관절 각도를 풀 수 없는 자세는 겹침을 판단할 수 없으므로 "겹치지 않음"으로 답하고, 닿는지는 도달 판정이 가른다.
    /// 출발 자세와 팔 베이스 자세는 부르는 쪽이 준다: 지금 서 있는 자리의 팔과 서 보기 전에 가늠하는 자리가 같은 코드를 쓴다.
    /// </summary>
    public sealed class ArmClearanceProbe
    {
        private readonly ISeededToolSolver _solver;
        private readonly Func<float[]> _seedProvider;
        private readonly ArmCollisionChecker _checker;
        private readonly Func<Pose> _currentArmBase;
        private readonly float[] _solution;
        private readonly float[] _nearJoints;
        private readonly float[] _graspJoints;
        private readonly float[] _retreatJoints;

        // 자세 사이의 길을 이만큼(rad, 약 5.7도)씩 끊어 따진다. 이보다 크게 건너뛰면 얇은 선반 모서리를 지나치기 쉽다.
        private const float PathStepRad = 0.1f;

        /// <param name="seedProvider">풀이를 시작할 관절 각도(rad)를 돌려준다. 돌려준 배열은 읽기만 한다.</param>
        /// <param name="currentArmBase">팔 베이스 링크의 지금 월드 자세.</param>
        /// <param name="jointCount">관절 수.</param>
        public ArmClearanceProbe(
            ISeededToolSolver solver, Func<float[]> seedProvider, ArmCollisionChecker checker, Func<Pose> currentArmBase, int jointCount)
        {
            _solver = solver ?? throw new ArgumentNullException(nameof(solver));
            _seedProvider = seedProvider ?? throw new ArgumentNullException(nameof(seedProvider));
            _checker = checker ?? throw new ArgumentNullException(nameof(checker));
            _currentArmBase = currentArmBase ?? throw new ArgumentNullException(nameof(currentArmBase));
            _solution = new float[jointCount];
            _nearJoints = new float[jointCount];
            _graspJoints = new float[jointCount];
            _retreatJoints = new float[jointCount];
        }

        /// <summary>팔이 지금 서 있는 자리에서 worldTool에 닿는 자세가 장애물과 겹치지 않으면 true.</summary>
        public bool IsClear(Pose worldTool)
        {
            return IsClear(worldTool, _currentArmBase());
        }

        /// <summary>
        /// worldTool은 지금 서 있는 팔 베이스를 기준으로 푸는 도구 자세다(서 보기 전에 가늠할 때는 후보 자리에 서 있을 때의 자세를 지금 베이스 기준으로 옮긴 것).
        /// 푼 관절 각도에서 팔이 차지하는 공간은 armBaseWorld(겹침을 따질 때 팔이 서 있는 자리)에 놓고 따진다.
        /// </summary>
        public bool IsClear(Pose worldTool, Pose armBaseWorld)
        {
            float[] seed = _seedProvider();
            if (seed == null || !_solver.TrySolveFrom(worldTool, seed, _solution))
            {
                return true;
            }

            return !_checker.Collides(_solution, armBaseWorld);
        }

        /// <summary>팔이 지금 서 있는 자리에서 이 관절 각도가 장애물과 겹치지 않으면 true.</summary>
        public bool IsJointsClear(float[] jointsRad)
        {
            return !_checker.Collides(jointsRad, _currentArmBase());
        }

        /// <summary>
        /// 지금 자세에서 접근, 쥐는 자리, 후퇴로 차례로 움직이는 동안 팔이 한 번도 겹치지 않으면 true. 세 자리뿐 아니라 그 사이를 지나는 길도 따진다.
        /// 관절 길이 있으면 그 각도로, 없으면 도구 자세를 앞 자리의 관절 각도에서 이어 풀어 따진다.
        /// </summary>
        public bool IsClear(GraspTargets targets)
        {
            return !TryFindCollision(targets, out _);
        }

        /// <summary>IsClear(targets)가 거짓일 때 어떤 링크가 어떤 장애물과 어디서 겹치는지 설명한다. 겹치지 않으면 빈 글자.</summary>
        public string DescribeCollision(GraspTargets targets)
        {
            return TryFindCollision(targets, out string description) ? description : string.Empty;
        }

        private bool TryFindCollision(GraspTargets targets, out string description)
        {
            float[] start = _seedProvider();
            if (targets.Route != null)
            {
                return TryFindAlong(start, new[] { targets.Route.Near, targets.Route.Grasp, targets.Route.Retreat }, out description);
            }

            // 풀이는 앞 자리에서 이어서 푼다. 실제로도 접근한 자세에서 쥐는 자리로, 쥐는 자세에서 후퇴로 움직이기 때문이다.
            float[] near = SolveFrom(targets.NearPose, start, _nearJoints);
            float[] grasp = SolveFrom(targets.GraspPose, near ?? start, _graspJoints);
            float[] retreat = SolveFrom(targets.RetreatPose, grasp ?? near ?? start, _retreatJoints);
            return TryFindAlong(start, new[] { near, grasp, retreat }, out description);
        }

        // 풀 수 없는 자세는 겹침을 판단할 수 없으므로 null로 두고, 그 자리와 그 앞뒤 구간은 따지지 않는다(닿는지는 도달 판정이 가른다).
        private float[] SolveFrom(Pose worldTool, float[] seed, float[] buffer)
        {
            return seed != null && _solver.TrySolveFrom(worldTool, seed, buffer) ? buffer : null;
        }

        private bool TryFindAlong(float[] start, float[][] waypoints, out string description)
        {
            description = string.Empty;
            Pose armBase = _currentArmBase();
            float[] previous = start;

            foreach (float[] joints in waypoints)
            {
                if (joints == null)
                {
                    previous = null;
                    continue;
                }

                if (_checker.TryFindCollision(joints, armBase, out description))
                {
                    return true;
                }

                if (previous != null && _checker.PathCollides(previous, joints, armBase, PathStepRad, out description))
                {
                    return true;
                }

                previous = joints;
            }

            return false;
        }

        /// <summary>겹치는 첫 링크와 장애물을 설명한다. 겹치지 않거나 풀 수 없으면 빈 글자.</summary>
        public string DescribeCollision(Pose worldTool)
        {
            float[] seed = _seedProvider();
            if (seed == null || !_solver.TrySolveFrom(worldTool, seed, _solution))
            {
                return string.Empty;
            }

            return _checker.TryFindCollision(_solution, _currentArmBase(), out string description) ? description : string.Empty;
        }
    }
}
