using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 자세로 집으러 갈 때 팔이 선반 같은 장애물과 겹치지 않는지 따지고, 겹치면 알려 준 방향 가까이에서 겹치지 않는 도구 방향을 찾는다.
    /// 후보마다 알려 준 관절 모양에서 출발해 접근·집기·후퇴 관절 길을 풀고(SeededRouteBuilder), 그 길이 지금 자세에서부터 겹치지 않는지 묻는다(isRouteClear).
    /// 알려 준 방향이 그대로 겹치지 않으면 그 방향을 쓴다(첫 후보다). 겹침 규칙과 후보를 하나씩 보는 반복은 OrientationSearchLoop가 맡는다.
    /// 방향을 바꿔도 안 되는 경우는 서는 자리를 바꿔야 하므로, 호출하는 쪽이 스스로 찾는 집기로 넘어간다.
    /// </summary>
    public sealed class TaughtClearancePlanner
    {
        private readonly SeededRouteBuilder _routes;
        private readonly Func<GraspTargets, bool> _isRouteClear;
        private readonly Func<GraspTargets, string> _describe;
        private readonly OrientationSearchLoop _loop;
        private readonly OrientationSearchSettings _settings;

        /// <param name="isRouteClear">접근·집기·후퇴 길이 겹치지 않으면 true.</param>
        /// <param name="describe">겹치는 곳의 설명. 필요 없으면 null.</param>
        /// <param name="policy">겹치지 않는 방향이 없을 때의 규칙. null이면 기본 규칙.</param>
        /// <param name="settings">알려 준 방향에서 얼마나 벗어난 방향까지 볼지.</param>
        /// <param name="budget">한 프레임에 이어서 따질 수 있는 시간. null이면 후보 하나마다 한 프레임을 쉰다.</param>
        public TaughtClearancePlanner(
            SeededRouteBuilder routes,
            Func<GraspTargets, bool> isRouteClear,
            Func<GraspTargets, string> describe,
            ClearancePolicy policy,
            OrientationSearchSettings settings,
            FrameBudget budget = null)
        {
            _routes = routes ?? throw new ArgumentNullException(nameof(routes));
            _isRouteClear = isRouteClear ?? throw new ArgumentNullException(nameof(isRouteClear));
            _describe = describe;
            _loop = new OrientationSearchLoop(policy, budget);
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <param name="seedRad">알려 준 관절 각도(rad). 길을 풀 때 출발하는 모양이다.</param>
        public IEnumerator Plan(IReadOnlyList<float> seedRad, TaughtPickPlan plan, ClearGraspPlan result)
        {
            var search = new OrientationSearchResult();
            yield return _loop.Run(plan.ToolRotation, _settings, rotation => Evaluate(seedRad, plan, rotation, result), search);

            if (!search.IsFound)
            {
                result.NotFound(search.TriedCount);
                yield break;
            }

            // 고른 방향의 길은 한 번 더 풀어 받는다(후보를 따질 때 쓴 길은 보관하지 않는다).
            if (!TryBuild(seedRad, plan, search.Rotation, out TaughtJointRoute route))
            {
                result.NotFound(search.TriedCount);
                yield break;
            }

            result.Found(search.Rotation, route, search.DeviationDeg, search.HasCollisionRisk, search.TriedCount);
        }

        private CandidateVerdict Evaluate(IReadOnlyList<float> seedRad, TaughtPickPlan plan, Quaternion rotation, ClearGraspPlan result)
        {
            if (!TryBuild(seedRad, plan, rotation, out TaughtJointRoute route))
            {
                return CandidateVerdict.Unreachable;
            }

            var targets = new GraspTargets(rotation, plan.NearPosition, plan.GraspPosition, plan.RetreatPosition, route);
            if (_isRouteClear(targets))
            {
                return CandidateVerdict.Clear;
            }

            if (_describe != null)
            {
                result.RecordFirstBlock(_describe(targets));
            }

            return CandidateVerdict.Blocked;
        }

        private bool TryBuild(IReadOnlyList<float> seedRad, TaughtPickPlan plan, Quaternion rotation, out TaughtJointRoute route)
        {
            return _routes.TryBuild(
                seedRad,
                new Pose(plan.NearPosition, rotation),
                new Pose(plan.GraspPosition, rotation),
                new Pose(plan.RetreatPosition, rotation),
                out route);
        }
    }
}
