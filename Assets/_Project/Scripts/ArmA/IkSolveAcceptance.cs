using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// IK 결과를 "이 풀이로 움직여도 된다"고 볼 기준. 기본은 엄격해서 IK가 수렴해야만 쓴다.
    /// 느슨한 기준은 수렴하지 못했어도 도구 끝점 오차가 허용 범위 안이고, 관절을 너무 많이 돌지 않는 풀이를 쓴다.
    /// 지나가는 자리는 몇 cm 빗나가도 되는데 IK는 2mm 안으로 수렴해야만 성공으로 치면, 가늠할 때는 풀리던 자리가
    /// 실행할 때 아슬아슬하게 못 풀려 작업이 멈춘다. 이 기준이 그 차이를 메운다.
    /// 관절 이동 제한을 확인하는 이유: IK가 수렴하지 못했을 때 돌려주는 풀이는 현재 자세에서 먼 자세일 수 있다.
    /// 오차만 보고 쓰면 팔이 뒤집힌 자세로 가다가 선반에 걸린다.
    /// </summary>
    public readonly struct IkSolveAcceptance
    {
        public IkSolveAcceptance(float positionToleranceM, float rotationToleranceDeg)
        {
            PositionToleranceM = positionToleranceM;
            RotationToleranceDeg = rotationToleranceDeg;
        }

        /// <summary>수렴한 풀이만 쓰는 기준.</summary>
        public static IkSolveAcceptance Strict => default;

        public float PositionToleranceM { get; }

        public float RotationToleranceDeg { get; }

        /// <summary>수렴하지 못한 풀이도 허용 오차 안이면 쓰는 기준인지. 두 허용 오차가 모두 양수일 때만 느슨하다.</summary>
        public bool IsLoose => PositionToleranceM > 0f && RotationToleranceDeg > 0f;

        /// <param name="result">IK 결과.</param>
        /// <param name="largestJointTravelRad">현재 자세에서 풀이까지 가장 많이 돌아야 하는 관절의 각도(rad).</param>
        /// <param name="maxJointTravelRad">관절이 돌 수 있는 최대 각도(rad). 0 이하면 제한이 없다.</param>
        public bool IsUsable(IkResult result, float largestJointTravelRad, float maxJointTravelRad)
        {
            if (result.Converged)
            {
                return true;
            }

            if (!IsLoose)
            {
                return false;
            }

            if (result.PositionError > PositionToleranceM || result.RotationErrorDeg > RotationToleranceDeg)
            {
                return false;
            }

            return maxJointTravelRad <= 0f || largestJointTravelRad <= maxJointTravelRad;
        }
    }
}
