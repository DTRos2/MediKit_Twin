using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 시뮬레이션 시간과 배속. 보충 대기, 센서 타임아웃, 시작·완료 시각, 로그 시각은 모두 이 시간을 읽는다. (FR-15)
    /// 배속은 Time.timeScale로 반영하므로 Time.deltaTime과 Time.time이 곧 시뮬레이션 시간이다.
    /// 예외는 ROS 토픽 발행 주기와 StationHeartbeat이며, 이 둘은 실제 시간(unscaled)을 쓴다.
    /// </summary>
    public static class SimClock
    {
        /// <summary>선택할 수 있는 배속. 일시정지는 Pause()로 따로 처리한다.</summary>
        public static readonly float[] AllowedSpeeds = { 0.5f, 1f, 2f, 4f };

        /// <summary>시뮬레이션 시작 후 지난 시간(초). 배속과 일시정지가 반영된다.</summary>
        public static float Now => Time.time;

        public static float Speed { get; private set; } = 1f;

        public static bool IsPaused { get; private set; }

        /// <summary>AllowedSpeeds에 없는 값이면 적용하지 않고 false를 돌려준다.</summary>
        public static bool TrySetSpeed(float speed)
        {
            if (System.Array.IndexOf(AllowedSpeeds, speed) < 0)
            {
                return false;
            }

            Speed = speed;
            Apply();
            return true;
        }

        public static void Pause()
        {
            IsPaused = true;
            Apply();
        }

        public static void Resume()
        {
            IsPaused = false;
            Apply();
        }

        private static void Apply()
        {
            Time.timeScale = IsPaused ? 0f : Speed;
        }

        // Domain Reload를 끈 에디터에서 이전 Play의 배속이 남지 않도록 시작할 때 되돌린다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Speed = 1f;
            IsPaused = false;
            Apply();
        }
    }
}
