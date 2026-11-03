#if UNITY_EDITOR
using MediKitTwin.Common;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>한 키트가 받은 알림 하나와 그때의 시뮬레이션 시간(초).</summary>
    internal readonly struct FlowEvent
    {
        public FlowEvent(FlowStep step, KitData kit, float time)
        {
            Step = step;
            Kit = kit;
            Time = time;
        }

        public FlowStep Step { get; }

        public KitData Kit { get; }

        public float Time { get; }
    }
}
#endif
