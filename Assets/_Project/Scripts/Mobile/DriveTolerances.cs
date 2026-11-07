using System;

namespace MediKitTwin.Mobile
{
    /// <summary>
    /// 차동 구동이 "닿았다", "방향이 맞았다"를 판정하는 허용 오차 묶음. 값은 MobileBaseDriver의 인스펙터에서 정하고, 만들 때 한 번 검사한다.
    /// </summary>
    public readonly struct DriveTolerances
    {
        /// <param name="arriveM">목표에서 이 거리(m) 안이면 도착으로 본다.</param>
        /// <param name="alignDeg">목표 방향과 이 각도(도) 안으로 맞으면 회전을 멈추고 전진한다.</param>
        /// <param name="finalYawDeg">도착한 뒤 최종 방향이 이 각도(도) 안이면 맞은 것으로 보고 정확한 방향으로 맞춘다.</param>
        /// <param name="stopMarginRatio">
        /// 허용 오차의 이 비율(0 초과 1 이하)만큼 안쪽을 멈출 자리로 삼아 감속한다. 허용 오차 경계에서 속도가 0이 되면 경계에 닿기 전에
        /// 멈춰 서 버릴 수 있고, 경계에서 빠르면 도착 처리 때 속도가 한꺼번에 사라진다. 그 사이에서 천천히 들어가게 하는 값이다.
        /// </param>
        public DriveTolerances(float arriveM, float alignDeg, float finalYawDeg, float stopMarginRatio)
        {
            if (arriveM <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arriveM), "도착 허용 거리는 0보다 커야 한다.");
            }

            if (alignDeg <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(alignDeg), "방향 허용 각도는 0보다 커야 한다.");
            }

            if (finalYawDeg <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(finalYawDeg), "최종 방향 허용 각도는 0보다 커야 한다.");
            }

            if (stopMarginRatio <= 0f || stopMarginRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(stopMarginRatio), "멈춤 여유 비율은 0 초과 1 이하여야 한다.");
            }

            ArriveM = arriveM;
            AlignDeg = alignDeg;
            FinalYawDeg = finalYawDeg;
            StopMarginRatio = stopMarginRatio;
        }

        public float ArriveM { get; }

        public float AlignDeg { get; }

        public float FinalYawDeg { get; }

        public float StopMarginRatio { get; }
    }
}
