using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 한 번의 "서고, 닿는 방향을 찾는" 시도에 필요한 것들을 묶는다. 선반에서 집기와 박스에 놓기가 같은 절차(StandSearchRoutine)를 쓰기 위해서다.
    /// </summary>
    public readonly struct ReachPlan
    {
        public ReachPlan(
            IReadOnlyList<float> standoffOffsets,
            IReadOnlyList<float> lateralOffsets,
            Func<float, float, Vector3?> waypointFor,
            Func<float, float, Vector3?> standFor,
            Quaternion nominal,
            IReadOnlyList<Vector3> positions,
            OrientationSearchSettings search,
            string waypointStep,
            string standStep,
            string reachStep,
            float? facingYawDeg)
        {
            StandoffOffsets = standoffOffsets;
            LateralOffsets = lateralOffsets;
            WaypointFor = waypointFor;
            StandFor = standFor;
            Nominal = nominal;
            Positions = positions;
            Search = search;
            WaypointStep = waypointStep;
            StandStep = standStep;
            ReachStep = reachStep;
            FacingYawDeg = facingYawDeg;
        }

        /// <summary>기본 거리에 더해 볼 거리들(m).</summary>
        public IReadOnlyList<float> StandoffOffsets { get; }

        /// <summary>옆으로 비켜 볼 거리들(m). 미리 가늠할 때만 쓴다.</summary>
        public IReadOnlyList<float> LateralOffsets { get; }

        /// <summary>(물러나는 거리, 비키는 거리)에 해당하는, 서는 자리로 들어가기 전에 거치는 경유지. 없으면 null.</summary>
        public Func<float, float, Vector3?> WaypointFor { get; }

        /// <summary>(물러나는 거리, 비키는 거리)에 해당하는 서는 자리. 이동하지 않는 고정 팔이면 null을 돌려준다.</summary>
        public Func<float, float, Vector3?> StandFor { get; }

        public Quaternion Nominal { get; }

        public IReadOnlyList<Vector3> Positions { get; }

        public OrientationSearchSettings Search { get; }

        public string WaypointStep { get; }

        public string StandStep { get; }

        public string ReachStep { get; }

        /// <summary>서는 자리에서 바라볼 방향(월드 Y축 기준 도). 없으면 마지막 이동 방향으로 선다.</summary>
        public float? FacingYawDeg { get; }
    }
}
