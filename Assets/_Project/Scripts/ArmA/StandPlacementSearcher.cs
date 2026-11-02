using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서는 자리 후보마다 팔이 닿는 도구 방향이 있는지 따져 보고, 자리와 방향을 함께 점수로 견줄 수 있게 모은다. 베이스는 움직이지 않는다.
    /// 자리마다 GraspOrientationSearcher로 방향을 찾으므로, 한 자리에서 처음 닿는 방향이 그 자리의 가장 덜 무리한 방향이다.
    /// 후보는 기본 자리에서 가까운 순서로 받는다. 방향을 전혀 비틀지 않아도 지금까지 찾은 최선 이상의 점수가 되는 후보부터는
    /// 뒤 후보가 모두 더 멀리 서므로 이길 수 없어 거기서 멈춘다(방향 탐색은 프레임마다 하나씩이라 시간이 걸린다).
    /// 자리에서 장애물과 겹치지 않는지도 물으면(clearanceAt), 겹치지 않는 방향이 있는 자리를 우선한다. 닿기는 하지만 겹침을 감수해야 하는
    /// 자리는 큰 벌점을 받아 겹치지 않는 자리 뒤로 밀린다. 팔이 다른 자리에 서서 장애물을 피하게 하는 것이다.
    /// 자리에서 닿는지와 겹치는지 묻는 방법은 주입받으므로 씬 없이 시험할 수 있다.
    /// </summary>
    public sealed class StandPlacementSearcher
    {
        // 겹침을 감수해야 하는 자리에 더하는 벌점. 방향 1도의 점수 가중치와 견주어 어떤 거리·방향 이득보다도 크게 잡았다.
        private const float CollisionRiskPenalty = 1000f;

        private readonly Func<StandCandidate, Func<Pose, bool>> _reachabilityAt;
        private readonly Func<StandCandidate, Func<Pose, bool>> _clearanceAt;
        private readonly StandPlacementCost _cost;
        private readonly ClearancePolicy _policy;
        private readonly FrameBudget _budget;

        /// <param name="reachabilityAt">자리 하나를 받아, 팔이 그 자리에 서 있다면 월드 기준 도구 자세에 닿는지 답하는 함수를 돌려준다.</param>
        /// <param name="cost">자리와 방향 조합의 점수 계산.</param>
        /// <param name="clearanceAt">자리 하나를 받아, 그 자리에 서 있다면 월드 기준 도구 자세에서 팔이 장애물과 겹치지 않는지 답하는 함수를 돌려준다. 겹침을 따지지 않으면 null.</param>
        /// <param name="policy">겹치지 않는 방향이 없을 때의 규칙. null이면 기본 규칙.</param>
        /// <param name="budget">한 프레임에 이어서 따질 수 있는 시간. 자리마다의 방향 찾기가 이 예산을 나눠 쓴다. null이면 후보 하나마다 한 프레임을 쉰다.</param>
        public StandPlacementSearcher(
            Func<StandCandidate, Func<Pose, bool>> reachabilityAt,
            StandPlacementCost cost,
            Func<StandCandidate, Func<Pose, bool>> clearanceAt = null,
            ClearancePolicy policy = null,
            FrameBudget budget = null)
        {
            _reachabilityAt = reachabilityAt ?? throw new ArgumentNullException(nameof(reachabilityAt));
            _cost = cost ?? throw new ArgumentNullException(nameof(cost));
            _clearanceAt = clearanceAt;
            _policy = policy ?? ClearancePolicy.Default;
            _budget = budget;
        }

        /// <param name="candidates">기본 자리에서 가까운 순서(ShiftM 오름차순)의 후보.</param>
        /// <param name="positions">접근, 집기, 후퇴처럼 같은 방향으로 지나야 하는 모든 위치(월드).</param>
        public IEnumerator Search(
            IReadOnlyList<StandCandidate> candidates,
            Quaternion nominal,
            IReadOnlyList<Vector3> positions,
            OrientationSearchSettings settings,
            StandPlacementResult result)
        {
            int evaluated = 0;
            float bestCost = float.PositiveInfinity;

            foreach (StandCandidate candidate in candidates)
            {
                // 이 자리는 방향을 하나도 안 비틀어도 이미 찾은 최선 이상이다. 후보가 기본 자리에서 가까운 순서라 뒤도 마찬가지다.
                if (_cost.Of(0f, candidate.ShiftM) >= bestCost)
                {
                    break;
                }

                evaluated++;

                var orientation = new OrientationSearchResult();
                var searcher = new GraspOrientationSearcher(
                    _reachabilityAt(candidate), _clearanceAt?.Invoke(candidate), _policy, budget: _budget);
                yield return searcher.Search(nominal, positions, settings, orientation);

                if (!orientation.IsFound)
                {
                    continue;
                }

                float cost = _cost.Of(orientation.DeviationDeg, candidate.ShiftM) + (orientation.HasCollisionRisk ? CollisionRiskPenalty : 0f);
                result.Add(new StandPlacementOption(candidate, orientation.Rotation, orientation.DeviationDeg, cost, orientation.HasCollisionRisk));
                bestCost = Mathf.Min(bestCost, cost);
            }

            result.Finish(evaluated);
        }
    }
}
