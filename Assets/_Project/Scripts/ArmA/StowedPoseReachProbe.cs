using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 월드의 도구 자세에 팔이 닿는지, 접힌 자세에서 출발했을 때를 기준으로 답한다.
    /// 베이스가 움직이기 전에 팔은 접히고, 서는 자리에서 실제로 방향을 찾는 것은 그 접힌 자세에서 출발한다.
    /// 그런데 서 보기 전에 가늠할 때 지금 자세(선반 깊이 뻗은 모양 등)에서 출발해 물으면 닿는 방향이 거의 없다고 나와
    /// 불필요하게 오래 찾게 된다. 접는 자세를 모르거나 풀이를 쓸 수 없으면 주어진 기본 판정(지금 자세 기준)으로 답한다.
    /// 씬 없이 시험한다.
    /// </summary>
    public sealed class StowedPoseReachProbe
    {
        private readonly IArmStowPoseSource _stowPose;
        private readonly ISeededToolSolver _solver;
        private readonly Func<Pose, bool> _fallback;
        private float[] _scratch;

        /// <param name="stowPose">접는 자세를 알려 주는 쪽. 없으면 null.</param>
        /// <param name="solver">출발 자세를 고를 수 있는 풀이. 없으면 null.</param>
        /// <param name="fallback">접힌 자세 기준으로 물을 수 없을 때 쓰는 판정(지금 자세 기준).</param>
        public StowedPoseReachProbe(IArmStowPoseSource stowPose, ISeededToolSolver solver, Func<Pose, bool> fallback)
        {
            _stowPose = stowPose;
            _solver = solver;
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public bool CanReach(Pose worldPose)
        {
            if (_stowPose == null || _solver == null || !_stowPose.TryGetStowPose(out float[] stow) || stow == null)
            {
                return _fallback(worldPose);
            }

            if (_scratch == null || _scratch.Length != stow.Length)
            {
                _scratch = new float[stow.Length];
            }

            return _solver.TrySolveFrom(worldPose, stow, _scratch);
        }
    }
}
