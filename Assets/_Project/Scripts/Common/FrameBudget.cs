using System;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 한 프레임에서 오래 걸리는 계산(IK 풀이, 충돌 확인 같은 반복)을 얼마나 이어서 해도 되는지 알려 주는 시간 예산.
    /// 계산하는 쪽이 후보 하나를 끝낼 때마다 IsExhausted를 보고, 예산을 다 썼으면 한 프레임 쉬고 돌아와 StartSlice로 새 구간을 시작한다.
    /// 후보를 프레임마다 하나씩 하는 것보다 빨리 끝나면서 한 프레임이 예산보다 크게 늘지 않는다.
    /// 시계는 주입받으므로 씬 없이 시험할 수 있고, 여러 반복이 한 예산을 나눠 써도 한 프레임의 합이 예산을 넘지 않는다.
    /// </summary>
    public sealed class FrameBudget
    {
        private readonly double _sliceSeconds;
        private readonly Func<double> _now;
        private double _sliceStartedAt;

        /// <param name="sliceSeconds">한 프레임에서 이어서 계산해도 되는 시간(초). 0 이하이면 항상 다 쓴 것으로 본다.</param>
        /// <param name="now">지금 시각(초). 프레임 시간이 아니라 실제 흐른 시간이어야 한다(예: Time.realtimeSinceStartupAsDouble).</param>
        public FrameBudget(double sliceSeconds, Func<double> now)
        {
            _sliceSeconds = sliceSeconds;
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _sliceStartedAt = _now();
        }

        /// <summary>지금 구간에서 예산을 다 썼으면 true.</summary>
        public bool IsExhausted => _now() - _sliceStartedAt >= _sliceSeconds;

        /// <summary>새 구간을 시작한다. 한 프레임을 쉬고 돌아온 직후에 부른다.</summary>
        public void StartSlice()
        {
            _sliceStartedAt = _now();
        }
    }
}
