using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 여러 도구 방향 중, 주어진 모든 위치에서 팔이 닿는 방향을 찾는다. 팔은 움직이지 않고 닿는지만 묻는다(canReach).
    /// 후보는 기본 방향에서 가까운 순서이므로 처음 닿는 방향이 가장 덜 무리한 방향이다.
    /// IK 풀이는 한 번에 여러 개를 하면 화면이 멈추므로 한 프레임에 후보 하나씩 확인한다.
    /// 닿는지 묻는 방법은 주입받으므로 씬 없이 시험할 수 있다.
    /// </summary>
    public sealed class GraspOrientationSearcher
    {
        private readonly Func<Pose, bool> _canReach;

        /// <param name="canReach">월드 기준 도구 자세에 팔이 닿으면 true.</param>
        public GraspOrientationSearcher(Func<Pose, bool> canReach)
        {
            _canReach = canReach ?? throw new ArgumentNullException(nameof(canReach));
        }

        /// <param name="positions">접근, 집기, 후퇴처럼 같은 방향으로 지나야 하는 모든 위치(월드).</param>
        public IEnumerator Search(
            Quaternion nominal, IReadOnlyList<Vector3> positions, OrientationSearchSettings settings, OrientationSearchResult result)
        {
            IReadOnlyList<Quaternion> candidates = OrientationCandidates.Generate(nominal, settings);

            for (int index = 0; index < candidates.Count; index++)
            {
                if (ReachesEverywhere(candidates[index], positions))
                {
                    result.Found(candidates[index], Quaternion.Angle(nominal, candidates[index]), index + 1);
                    yield break;
                }

                yield return null;
            }

            result.NotFound(candidates.Count);
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
    }
}
