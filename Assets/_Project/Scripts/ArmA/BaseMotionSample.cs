using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 옮기는 동안 물리 스텝 하나에서 잰 값: 그때의 시각, 베이스 루트의 자리와 방향, 쥔 물건이 도구에 대해 어떤 자세인지.
    /// 쥔 자세를 재지 못한 스텝(도구 자세를 못 읽음 등)은 Held가 null이다.
    /// </summary>
    public readonly struct BaseMotionSample
    {
        public BaseMotionSample(float timeSeconds, Vector3 basePosition, float baseYawDeg, HeldItemFrame? held)
        {
            TimeSeconds = timeSeconds;
            BasePosition = basePosition;
            BaseYawDeg = baseYawDeg;
            Held = held;
        }

        public float TimeSeconds { get; }

        public Vector3 BasePosition { get; }

        public float BaseYawDeg { get; }

        public HeldItemFrame? Held { get; }
    }
}
