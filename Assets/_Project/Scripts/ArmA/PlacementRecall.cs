using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 박스 슬롯에 놓기에 성공했던 방식. 다음에 같은 슬롯에 놓을 때 처음부터 찾지 않고 이 방식이 지금도 닿는지만 먼저 본다.
    /// 놓는 높이는 값이 아니라 높이 목록의 순번으로 남긴다(목록이 바뀌면 기억도 같이 버려지므로 값을 따로 들고 있을 이유가 없다).
    /// 도구 방향은 기본 방향에서 얼마나 벗어났는지(기울이기, 좌우, 비틀기)로 남긴다. 기본 방향은 쥔 물건마다 다르므로 절대 방향을 남기면 어긋난다.
    /// </summary>
    public readonly struct PlacementRecall
    {
        public PlacementRecall(
            int heightIndex, bool usesRestingOrientation, float pitchDeg, float yawDeg, float rollDeg, float standoffExtraM, float lateralM)
        {
            HeightIndex = heightIndex;
            UsesRestingOrientation = usesRestingOrientation;
            PitchDeg = pitchDeg;
            YawDeg = yawDeg;
            RollDeg = rollDeg;
            StandoffExtraM = standoffExtraM;
            LateralM = lateralM;
        }

        /// <summary>놓는 높이 목록에서 성공한 높이의 순번.</summary>
        public int HeightIndex { get; }

        /// <summary>물건이 선반에 놓여 있던 방향을 기본 방향으로 삼았으면 true. false면 똑바로 아래를 보는 방향이 기본 방향이다.</summary>
        public bool UsesRestingOrientation { get; }

        /// <summary>기본 방향에서 기울인 각도(도).</summary>
        public float PitchDeg { get; }

        /// <summary>기본 방향에서 좌우로 돌린 각도(도).</summary>
        public float YawDeg { get; }

        /// <summary>기본 방향에서 손목을 비튼 각도(도).</summary>
        public float RollDeg { get; }

        /// <summary>서는 자리를 기본 거리에서 더 물러난 거리(m).</summary>
        public float StandoffExtraM { get; }

        /// <summary>서는 자리를 옆으로 비킨 거리(m).</summary>
        public float LateralM { get; }

        /// <summary>기본 방향에 기억한 기울이기, 좌우, 비틀기를 더한 도구 방향. 방향을 찾을 때와 같은 순서로 곱해 같은 방향이 나오게 한다.</summary>
        public Quaternion ToolRotation(Quaternion nominal)
        {
            // 비틀기 가중치는 후보를 줄 세울 때만 쓰고 방향 계산에는 영향이 없다.
            return OrientationCandidate.Create(nominal, PitchDeg, YawDeg, RollDeg, 1f).Rotation;
        }
    }
}
