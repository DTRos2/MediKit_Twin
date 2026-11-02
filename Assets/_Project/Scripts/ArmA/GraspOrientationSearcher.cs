using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 여러 도구 방향 중, 주어진 모든 위치에서 팔이 닿는 방향을 찾는다. 팔은 움직이지 않고 닿는지만 묻는다(canReach).
    /// 후보는 기본 방향에서 가까운 순서이므로 처음 닿는 방향이 가장 덜 무리한 방향이다.
    /// 장애물과 겹치는지(isClear)도 물으면, 닿는 방향 가운데 겹치지 않는 첫 방향을 고른다. 겹침 규칙과 후보를 하나씩 보는 반복은 OrientationSearchLoop가 맡는다.
    /// 닿는지와 겹치는지 묻는 방법은 주입받으므로 씬 없이 시험할 수 있다.
    /// </summary>
    public sealed class GraspOrientationSearcher
    {
        private readonly Func<Pose, bool> _canReach;
        private readonly Func<Pose, bool> _isClear;
        private readonly Func<Quaternion, IReadOnlyList<Vector3>, bool> _isClearAlong;
        private readonly Func<Quaternion, IReadOnlyList<Vector3>, string> _describeBlock;
        private readonly OrientationSearchLoop _loop;

        /// <param name="canReach">월드 기준 도구 자세에 팔이 닿으면 true.</param>
        /// <param name="isClear">월드 기준 도구 자세에서 팔이 장애물과 겹치지 않으면 true. 겹침을 따지지 않으면 null.</param>
        /// <param name="policy">겹치지 않는 방향이 없을 때의 규칙. null이면 기본 규칙.</param>
        /// <param name="isClearAlong">
        /// (도구 방향, 지나는 위치들) → 위치들을 차례로 지나는 길 전체가 겹치지 않으면 true. 주면 자세마다 따지는 isClear 대신 이것으로 따진다.
        /// 자세 사이를 지나가는 도중에 선반 모서리를 쓸고 가는 것까지 잡으려면 준다. 없으면 null.
        /// </param>
        /// <param name="budget">한 프레임에 이어서 따질 수 있는 시간. null이면 후보 하나마다 한 프레임을 쉰다.</param>
        /// <param name="describeBlock">
        /// (도구 방향, 지나는 위치들) → 닿지만 겹쳐 버려진 방향이 어디서 겹치는지 "링크 ↔ 장애물"로 설명한다. 주면 버린 방향마다 불러 결과에 집계한다.
        /// 못 찾았을 때 무엇이 길을 막았는지 알려 주려는 것이고, 설명하느라 겹침을 한 번 더 따지므로 버려진 방향에만 쓴다. 필요 없으면 null.
        /// </param>
        public GraspOrientationSearcher(
            Func<Pose, bool> canReach,
            Func<Pose, bool> isClear = null,
            ClearancePolicy policy = null,
            Func<Quaternion, IReadOnlyList<Vector3>, bool> isClearAlong = null,
            FrameBudget budget = null,
            Func<Quaternion, IReadOnlyList<Vector3>, string> describeBlock = null)
        {
            _canReach = canReach ?? throw new ArgumentNullException(nameof(canReach));
            _isClear = isClear;
            _isClearAlong = isClearAlong;
            _describeBlock = describeBlock;
            _loop = new OrientationSearchLoop(policy, budget);
        }

        /// <param name="positions">접근, 집기, 후퇴처럼 같은 방향으로 지나야 하는 모든 위치(월드).</param>
        public IEnumerator Search(
            Quaternion nominal, IReadOnlyList<Vector3> positions, OrientationSearchSettings settings, OrientationSearchResult result)
        {
            return _loop.Run(nominal, settings, rotation => Evaluate(rotation, positions, result), result);
        }

        private CandidateVerdict Evaluate(Quaternion rotation, IReadOnlyList<Vector3> positions, OrientationSearchResult result)
        {
            if (!ReachesEverywhere(rotation, positions))
            {
                return CandidateVerdict.Unreachable;
            }

            if (IsClear(rotation, positions))
            {
                return CandidateVerdict.Clear;
            }

            if (_describeBlock != null)
            {
                result.Blockers.Record(_describeBlock(rotation, positions));
            }

            return CandidateVerdict.Blocked;
        }

        private bool IsClear(Quaternion rotation, IReadOnlyList<Vector3> positions)
        {
            if (_isClearAlong != null)
            {
                return _isClearAlong(rotation, positions);
            }

            return _isClear == null || IsClearEverywhere(rotation, positions);
        }

        private bool ReachesEverywhere(Quaternion rotation, IReadOnlyList<Vector3> positions)
        {
            foreach (Vector3 position in positions)
            {
                if (!_canReach(new Pose(position, rotation)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsClearEverywhere(Quaternion rotation, IReadOnlyList<Vector3> positions)
        {
            foreach (Vector3 position in positions)
            {
                if (!_isClear(new Pose(position, rotation)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
