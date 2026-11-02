using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 기본 방향에서 가까운 순서로 도구 방향 후보를 하나씩 따져(evaluate), 닿고 겹치지 않는 첫 방향을 고르는 반복.
    /// 처음 닿는 방향이 겹친다면 정책(ClearancePolicy)이 정한 수만큼 더 후보를 보고, 그래도 겹치지 않는 방향이 없으면
    /// 정책에 따라 처음 닿은 방향을 겹침 위험(HasCollisionRisk)을 달아 돌려주거나 실패로 끝낸다.
    /// IK 풀이는 한 번에 너무 많이 하면 화면이 멈추므로, 시간 예산(FrameBudget)을 다 쓸 때까지만 따지고 한 프레임을 쉰다.
    /// 예산을 주지 않으면 한 프레임에 후보 하나씩 따진다.
    /// 후보를 어떻게 따지는지(어디서 출발해 푸는지, 몇 군데를 보는지)는 호출하는 쪽이 정하므로, 스스로 찾는 집기와 알려 준 자세 고치기가 함께 쓴다.
    /// </summary>
    public sealed class OrientationSearchLoop
    {
        private readonly ClearancePolicy _policy;
        private readonly FrameBudget _budget;

        /// <param name="budget">한 프레임에 이어서 따질 수 있는 시간. null이면 후보 하나마다 한 프레임을 쉰다.</param>
        public OrientationSearchLoop(ClearancePolicy policy, FrameBudget budget = null)
        {
            _policy = policy ?? ClearancePolicy.Default;
            _budget = budget;
        }

        /// <param name="nominal">기본 방향. 첫 후보다.</param>
        /// <param name="evaluate">후보 방향을 따져 닿는지와 겹치는지 답한다.</param>
        public IEnumerator Run(
            Quaternion nominal, OrientationSearchSettings settings, Func<Quaternion, CandidateVerdict> evaluate, OrientationSearchResult result)
        {
            IReadOnlyList<OrientationCandidate> candidates = OrientationCandidates.GenerateRanked(nominal, settings);
            int riskyIndex = -1;
            int unreachable = 0;
            int blocked = 0;

            for (int index = 0; index < candidates.Count; index++)
            {
                if (IsLookaheadOver(riskyIndex, index))
                {
                    break;
                }

                CandidateVerdict verdict = evaluate(candidates[index].Rotation);
                if (verdict == CandidateVerdict.Clear)
                {
                    result.Found(candidates[index], index + 1);
                    result.RecordVerdicts(unreachable, blocked);
                    yield break;
                }

                if (verdict == CandidateVerdict.Unreachable)
                {
                    unreachable++;
                }
                else
                {
                    blocked++;
                }

                if (verdict == CandidateVerdict.Blocked && riskyIndex < 0)
                {
                    riskyIndex = index;
                }

                if (ShouldRest())
                {
                    yield return null;
                    _budget?.StartSlice();
                }
            }

            if (riskyIndex >= 0 && !_policy.RequireClear)
            {
                result.Found(candidates[riskyIndex], riskyIndex + 1, hasCollisionRisk: true);
                result.RecordVerdicts(unreachable, blocked);
                yield break;
            }

            result.NotFound(candidates.Count);
            result.RecordVerdicts(unreachable, blocked);
        }

        // 예산이 없으면 후보마다, 있으면 예산을 다 썼을 때만 한 프레임을 쉰다.
        private bool ShouldRest()
        {
            return _budget == null || _budget.IsExhausted;
        }

        // 겹치는 방향만 닿는다고 나온 뒤 더 볼 만큼 봤는지. 겹침을 감수하지 않는 규칙이면 끝까지 본다.
        private bool IsLookaheadOver(int riskyIndex, int index)
        {
            return riskyIndex >= 0 && !_policy.RequireClear && index - riskyIndex > _policy.LookaheadCandidates;
        }
    }
}
