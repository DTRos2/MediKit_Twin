using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 접근·집기·후퇴 도구 자세를 출발 자세(시드)에서 푼 관절 각도 길(TaughtJointRoute)로 바꾼다.
    /// 알려 준 자세로 집을 때도, 카메라로 본 위치에 맞춰 자리를 옮긴 뒤 길을 다시 풀 때도 같은 방식으로 풀도록 한 곳에 모았다.
    /// </summary>
    public sealed class SeededRouteBuilder
    {
        private readonly ISeededToolSolver _solver;

        public SeededRouteBuilder(ISeededToolSolver solver)
        {
            _solver = solver ?? throw new ArgumentNullException(nameof(solver));
        }

        /// <param name="seedRad">쥐는 자리를 풀 때 출발할 관절 각도(rad).</param>
        /// <returns>세 자리 모두 풀렸으면 true. 하나라도 못 풀면 false이고 route는 null이다.</returns>
        public bool TryBuild(IReadOnlyList<float> seedRad, Pose near, Pose grasp, Pose retreat, out TaughtJointRoute route)
        {
            return TaughtJointRoute.TryBuild(seedRad, near, grasp, retreat, SolveFromSeed, out route);
        }

        private float[] SolveFromSeed(Pose worldPose, IReadOnlyList<float> seedRad)
        {
            var solution = new float[seedRad.Count];
            return _solver.TrySolveFrom(worldPose, seedRad, solution) ? solution : null;
        }
    }
}
