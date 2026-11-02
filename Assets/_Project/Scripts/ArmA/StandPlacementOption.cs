using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>팔이 닿는 것으로 가늠된 서는 자리 하나와, 거기서 닿는 도구 방향과 점수.</summary>
    public sealed class StandPlacementOption
    {
        public StandPlacementOption(StandCandidate candidate, Quaternion rotation, float deviationDeg, float cost, bool hasCollisionRisk = false)
        {
            Candidate = candidate;
            Rotation = rotation;
            DeviationDeg = deviationDeg;
            Cost = cost;
            HasCollisionRisk = hasCollisionRisk;
        }

        /// <summary>그 자리에서 닿는 방향이 팔이 장애물과 겹칠 수 있는 방향뿐이면 true. 점수에 큰 벌점이 더해져 겹치지 않는 자리 뒤로 밀린다.</summary>
        public bool HasCollisionRisk { get; }

        public StandCandidate Candidate { get; }

        /// <summary>그 자리에서 닿는 도구 방향.</summary>
        public Quaternion Rotation { get; }

        /// <summary>도구 방향이 기본 방향에서 벌어진 정도(도). 손목 비틀기는 가중치만큼 줄여 센다.</summary>
        public float DeviationDeg { get; }

        /// <summary>점수. 작을수록 좋다.</summary>
        public float Cost { get; }
    }
}
