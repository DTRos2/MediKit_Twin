using System;
using System.Collections;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 자세가 없는 칸에서 집는 순서: 서는 자리와 도구 방향을 스스로 찾고(StandSearchRoutine), 찾은 방향으로 쥐고 뺀다(ShelfGraspSequence).
    /// 도구의 +Z가 물건 쪽(칸 안쪽)을 향하는 것이 기본 방향이고, 위는 월드 위쪽이다.
    /// </summary>
    public sealed class SearchedPickRoutine
    {
        private readonly StandPolicy _policy;
        private readonly StandSearchRoutine _standSearch;
        private readonly ShelfGraspSequence _grasp;
        private readonly ShelfPickSettings _settings;

        public SearchedPickRoutine(StandPolicy policy, StandSearchRoutine standSearch, ShelfGraspSequence grasp, ShelfPickSettings settings)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _standSearch = standSearch ?? throw new ArgumentNullException(nameof(standSearch));
            _grasp = grasp ?? throw new ArgumentNullException(nameof(grasp));
            _settings = settings;
        }

        public IEnumerator Run(ItemPickRequest request, TransferResult result)
        {
            Vector3 itemPosition = request.Item.Pose.position;
            Vector3 outward = ShelfDirection.Outward(request.Item.ApproachDirection);
            Vector3 nearPosition = itemPosition + outward * _settings.ApproachDistanceM;
            Vector3 retreatPosition = itemPosition + outward * _settings.RetreatDistanceM;

            var plan = new ReachPlan(
                _policy.PickStandoffOffsets,
                _policy.LateralOffsets,
                (extra, lateral) => _policy.ShelfWaypoint(itemPosition, outward, extra, lateral),
                (extra, lateral) => _policy.ShelfStand(itemPosition, outward, extra, lateral),
                Quaternion.LookRotation(-outward, Vector3.up),
                new[] { nearPosition, itemPosition, retreatPosition },
                _settings.Search,
                TransferSteps.ShelfApproachWaypoint,
                TransferSteps.ShelfStandPoint,
                TransferSteps.ShelfNear,
                _policy.ShelfFacingYawDeg(outward));

            // 서는 자리에 도착하면 팔이 닿는 방향을 찾는다. 찾은 방향으로 접근, 집기, 후퇴를 모두 한다.
            var search = new OrientationSearchResult();
            yield return _standSearch.ApproachAndSearch(plan, result, found => search = found);
            if (!search.IsFound)
            {
                yield break;
            }

            yield return _grasp.Run(request, search.Rotation, nearPosition, itemPosition, retreatPosition, null, null, result);
        }
    }
}
