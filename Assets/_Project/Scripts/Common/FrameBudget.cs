using System;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 한 프레임에서 오래 걸리는 계산(IK 풀이, 충돌 확인 같은 반복)을 얼마나 이어서 해도 되는지 알려 주는 시간 예산.
    /// 계산하는 쪽이 후보 하나를 끝낼 때마다 IsExhausted를 보고, 예산을 다 썼으면 한 프레임 쉬고 돌아와 StartSlice로 새 구간을 시작한다.
    /// 후보를 프레임마다 하나씩 하는 것보다 빨리 끝나면서 한 프레임이 예산보다 크게 늘지 않는다.
    /// 시계는 주입받으므로 씬 없이 시험할 수 있고, 여러 반복이 한 예산을 나눠 써도 한 프레임의 합이 예산을 넘지 않는다.
    /// 배속이 오르면 한 프레임이 시뮬레이션 시간으로 그만큼 길어지므로, 예산도 배속만큼 늘려 같은 계산이 시뮬레이션 시간으로는 같게 걸리게 한다.
    /// 그러지 않으면 배속을 올려도 계산에 쓰는 프레임 수는 그대로라 그 시간이 배속 배로 늘어, 소요 시간이 배속만큼 줄지 않는다. (FR-15)
    /// </summary>
    public sealed class FrameBudget
    {
        private readonly double _sliceSeconds;
        private readonly Func<double> _now;
        private readonly Func<float> _speed;
        private double _sliceStartedAt;

        /// <param name="sliceSeconds">1배속에서 한 프레임에 이어서 계산해도 되는 시간(초). 0 이하이면 항상 다 쓴 것으로 본다.</param>
        /// <param name="now">지금 시각(초). 프레임 시간이 아니라 실제 흐른 시간이어야 한다(예: Time.realtimeSinceStartupAsDouble).</param>
        /// <param name="speed">지금 배속. 예산에 곱한다. 없으면 1배속으로 본다.</param>
        public FrameBudget(double sliceSeconds, Func<double> now, Func<float> speed = null)
        {
            _sliceSeconds = sliceSeconds;
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _speed = speed;
            _sliceStartedAt = _now();
        }

        /// <summary>지금 구간에서 예산을 다 썼으면 true.</summary>
        public bool IsExhausted => _now() - _sliceStartedAt >= _sliceSeconds * CurrentSpeed;

        // 배속을 알 수 없거나 0 이하이면(멈춤 등) 예산을 늘리거나 줄이지 않는다.
        private double CurrentSpeed
        {
            get
            {
                float speed = _speed != null ? _speed() : 1f;
                return speed > 0f ? speed : 1d;
            }
        }

        /// <summary>새 구간을 시작한다. 한 프레임을 쉬고 돌아온 직후에 부른다.</summary>
        public void StartSlice()
        {
            _sliceStartedAt = _now();
        }
    }
}
