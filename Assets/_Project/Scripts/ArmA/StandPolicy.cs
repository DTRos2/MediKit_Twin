using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스가 어디에 어느 방향으로 서는지에 관한 정책을 한곳에 모은다: 서는 자리(ArmBaseStandPlanner), 시도할 거리·옆 보정값과
    /// 서는 방향 설정(ArmBaseApproachConfigSO). 선반 집기와 박스 놓기가 같은 정책을 읽는다.
    /// 베이스를 못 옮기는 고정 팔(planner가 null)이면 서는 자리는 모두 null이고 보정값은 한 번(0)뿐이다.
    /// </summary>
    public sealed class StandPolicy
    {
        // 베이스를 움직이지 않는 고정 팔은 보정 없이 한 번만 시도한다.
        private static readonly float[] SingleAttempt = { 0f };

        private readonly ArmBaseStandPlanner _planner;
        private readonly ArmBaseApproachConfigSO _config;

        /// <param name="planner">서는 자리를 계산하는 쪽. 고정 팔이면 null.</param>
        /// <param name="config">거리와 방향 설정. planner가 있으면 반드시 지정한다.</param>
        public StandPolicy(ArmBaseStandPlanner planner, ArmBaseApproachConfigSO config)
        {
            _planner = planner;
            _config = config;
        }

        public bool CanMoveBase => _planner != null;

        /// <summary>선반 앞에서 차례로 시도할 거리 보정값(m).</summary>
        public IReadOnlyList<float> PickStandoffOffsets => CanMoveBase ? _config.PickStandoffOffsetsM : SingleAttempt;

        /// <summary>박스 곁에서 차례로 시도할 거리 보정값(m).</summary>
        public IReadOnlyList<float> PlaceStandoffOffsets => CanMoveBase ? _config.PlaceStandoffOffsetsM : SingleAttempt;

        /// <summary>자리를 옆으로 비켜 볼 거리들(m).</summary>
        public IReadOnlyList<float> LateralOffsets => CanMoveBase ? _config.StandLateralOffsetsM : SingleAttempt;

        /// <summary>서는 자리로 들어가기 전에 같은 선 위에서 더 물러나 거치는 거리(m).</summary>
        public float WaypointExtraM => _config != null ? _config.ApproachExtraM : 0f;

        /// <summary>서는 자리 탐색 설정. 미리 가늠할지와 실제로 서 볼 최대 횟수.</summary>
        public StandSearchSettings SearchSettings =>
            new StandSearchSettings(CanMoveBase && _config.PlanStandInAdvance, CanMoveBase ? _config.MaxStandAttempts : 1);

        /// <summary>서는 자리의 점수 계산. 고정 팔이면 null.</summary>
        public StandPlacementCost CreateCost()
        {
            return CanMoveBase ? new StandPlacementCost(_config.DeviationCostPerDeg, _config.ShiftCostPerM) : null;
        }

        /// <summary>선반 칸 정면에서 수직으로 들어가기 위해 거치는 경유지.</summary>
        public Vector3? ShelfWaypoint(Vector3 itemPosition, Vector3 outward, float extra, float lateral)
        {
            return CanMoveBase ? _planner.ForShelfItemApproach(itemPosition, outward, extra, lateral) : (Vector3?)null;
        }

        public Vector3? ShelfStand(Vector3 itemPosition, Vector3 outward, float extra, float lateral)
        {
            return CanMoveBase ? _planner.ForShelfItem(itemPosition, outward, extra, lateral) : (Vector3?)null;
        }

        public Vector3? BoxStand(Vector3 slotPosition, float extra, float lateral)
        {
            return CanMoveBase ? _planner.ForBoxSlot(slotPosition, extra, lateral) : (Vector3?)null;
        }

        /// <summary>선반 칸 앞에서 바라볼 방향. 구역을 정면으로 보고 서는 설정이 꺼져 있으면 정하지 않는다.</summary>
        public float? ShelfFacingYawDeg(Vector3 outward)
        {
            return FacingYawFor(ArmBaseStandPlanner.ShelfItemFacingYawDeg(outward, FacingOffsetDeg));
        }

        /// <summary>박스 곁에서 바라볼 방향.</summary>
        public float? BoxFacingYawDeg()
        {
            return FacingYawFor(_planner?.BoxSlotFacingYawDeg(FacingOffsetDeg));
        }

        // 구역을 정면으로 보고 서는 설정이 꺼져 있으면 방향을 정하지 않는다.
        private float? FacingYawFor(float? facingYawDeg)
        {
            return _config != null && _config.FaceTargetAtStand ? facingYawDeg : null;
        }

        private float FacingOffsetDeg => _config != null ? _config.FacingOffsetDeg : 0f;
    }
}
