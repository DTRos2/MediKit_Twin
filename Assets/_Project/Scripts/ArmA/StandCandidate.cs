using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>베이스가 서 볼 수 있는 자리 하나. 기본 자리에서 얼마나 물러나고 비켜 섰는지를 함께 들고 있다.</summary>
    public readonly struct StandCandidate
    {
        public StandCandidate(Vector3 armBasePosition, float standoffExtraM, float lateralM)
        {
            ArmBasePosition = armBasePosition;
            StandoffExtraM = standoffExtraM;
            LateralM = lateralM;
        }

        /// <summary>팔 베이스를 놓을 월드 지점(바닥 평면).</summary>
        public Vector3 ArmBasePosition { get; }

        /// <summary>기본 거리에 더한 거리(m). 클수록 더 물러난 자리다.</summary>
        public float StandoffExtraM { get; }

        /// <summary>기본 자리에서 옆으로 비킨 거리(m). 부호는 비킨 쪽이다.</summary>
        public float LateralM { get; }

        /// <summary>기본 자리에서 벗어난 거리(m). 부호와 상관없이 멀수록 크다.</summary>
        public float ShiftM => Mathf.Sqrt(StandoffExtraM * StandoffExtraM + LateralM * LateralM);
    }
}
