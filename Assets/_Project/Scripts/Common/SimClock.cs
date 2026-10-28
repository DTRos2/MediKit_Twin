using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 시뮬레이션 시간과 배속. 보충 대기, 센서 타임아웃, 시작·완료 시각, 로그 시각은 모두 이 시간을 읽는다. (FR-15)
    /// 배속은 Time.timeScale로 반영하므로 Time.deltaTime과 Time.time이 곧 시뮬레이션 시간이다.
    /// 예외는 ROS 토픽 발행 주기와 StationHeartbeat이며, 이 둘은 실제 시간(unscaled)을 쓴다.
    /// ROS는 Unity의 시간 배율을 모르므로, ROS 제어 모드에서는 SetSpeedLocked(true)로 1배속에 고정한다.
    /// </summary>
    public static class SimClock
    {
        /// <summary>선택할 수 있는 배속. 일시정지는 Pause()로 따로 처리한다.</summary>
        public static readonly float[] AllowedSpeeds = { 0.5f, 1f, 2f, 4f };

        /// <summary>시뮬레이션 시작 후 지난 시간(초). 배속과 일시정지가 반영된다.</summary>
        public static float Now => Time.time;

        public static float Speed { get; private set; } = 1f;

        public static bool IsPaused { get; private set; }

        /// <summary>true이면 배속을 바꿀 수 없다. 배속 버튼은 이 값으로 비활성화한다.</summary>
        public static bool IsSpeedLocked { get; private set; }

        /// <summary>
        /// 배속을 바꾼다. AllowedSpeeds에 없는 값이거나 배속이 고정되어 있으면 적용하지 않고 false를 돌려준다.
        /// </summary>
        public static bool TrySetSpeed(float speed)
        {
            if (IsSpeedLocked || System.Array.IndexOf(AllowedSpeeds, speed) < 0)
            {
                return false;
            }

            Speed = speed;
            Apply();
            return true;
        }

        /// <summary>
        /// 배속 고정을 켜거나 끈다. 켜면 배속이 1배로 돌아가고 TrySetSpeed가 거부된다.
        /// ROS 제어 모드(RosArmTarget 사용 중)가 시작할 때 켜고 끝날 때 끈다.
        /// </summary>
        public static void SetSpeedLocked(bool locked)
        {
            IsSpeedLocked = locked;
            if (locked)
            {
                Speed = 1f;
                Apply();
            }
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
            IsSpeedLocked = false;
            Apply();
        }
    }
}
