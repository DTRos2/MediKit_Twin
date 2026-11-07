using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 벨트 무늬의 UV 오프셋을 쌓아 간다. 벨트가 v m/s로 가면 무늬도 흐름 방향으로 v m/s로 가야 하므로,
    /// 오프셋은 (UV가 1m당 바뀌는 양) × 속도 × 시간만큼 반대로 움직인다(샘플하는 자리가 반대로 가야 무늬가 앞으로 간다).
    /// 값이 한없이 커지지 않게 0~1로 말아 둔다(무늬는 반복되므로 보이는 것은 같다). 속도는 부호가 있어도 된다.
    /// </summary>
    public sealed class BeltScroll
    {
        public Vector2 Offset { get; private set; }

        /// <param name="uvPerMeter">흐름 방향 1m당 (u, v)가 바뀌는 양(BeltUvFlow.PerMeter).</param>
        public void Advance(Vector2 uvPerMeter, float speedMps, float deltaSeconds)
        {
            Vector2 next = Offset - uvPerMeter * (speedMps * deltaSeconds);
            Offset = new Vector2(Wrap(next.x), Wrap(next.y));
        }

        private static float Wrap(float value)
        {
            float wrapped = value - Mathf.Floor(value);

            // 아주 작은 음수는 부동소수점 반올림으로 정확히 1이 될 수 있다. 0~1 범위(1 미만)를 지킨다.
            return wrapped >= 1f ? 0f : wrapped;
        }
    }
}
