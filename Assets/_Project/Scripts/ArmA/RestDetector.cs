using System;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 움직이던 물건이 멈췄는지 판정한다. 한 번 느려졌다고 멈춘 것으로 보지 않고, 느린 상태가 정해진 횟수만큼 이어져야 멈춘 것으로 본다.
    /// 떨어진 물건은 바닥에 닿는 순간 속도가 잠깐 0에 가까워졌다가 튀어 오르므로, 한 번만 보면 아직 움직이는 물건을 멈춘 것으로 잘못 알 수 있다.
    /// 물건이나 물리 엔진을 모르고 속도만 받으므로 어떤 물건에도 쓸 수 있다.
    /// </summary>
    public sealed class RestDetector
    {
        private readonly float _maxLinearSpeed;
        private readonly float _maxAngularSpeed;
        private readonly int _requiredSamples;
        private int _slowSamples;

        /// <param name="maxLinearSpeed">이 속도(m/s) 이하이면 느린 것으로 본다.</param>
        /// <param name="maxAngularSpeed">이 회전 속도(rad/s) 이하이면 느린 것으로 본다.</param>
        /// <param name="requiredSamples">연달아 느려야 하는 횟수.</param>
        public RestDetector(float maxLinearSpeed, float maxAngularSpeed, int requiredSamples)
        {
            if (maxLinearSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLinearSpeed), "속도 기준은 0 이상이어야 한다.");
            }

            if (maxAngularSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAngularSpeed), "회전 속도 기준은 0 이상이어야 한다.");
            }

            if (requiredSamples < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredSamples), "연달아 느려야 하는 횟수는 1 이상이어야 한다.");
            }

            _maxLinearSpeed = maxLinearSpeed;
            _maxAngularSpeed = maxAngularSpeed;
            _requiredSamples = requiredSamples;
        }

        /// <summary>지금까지 느린 상태가 이어진 횟수.</summary>
        public int SlowSamples => _slowSamples;

        /// <summary>속도를 한 번 넣고, 멈췄다고 볼 수 있으면 true를 돌려준다. 빠르게 움직이면 세던 횟수를 처음부터 다시 센다.</summary>
        public bool Update(float linearSpeed, float angularSpeed)
        {
            bool isSlow = linearSpeed <= _maxLinearSpeed && angularSpeed <= _maxAngularSpeed;
            _slowSamples = isSlow ? _slowSamples + 1 : 0;
            return _slowSamples >= _requiredSamples;
        }

        /// <summary>세던 횟수를 지운다. 다른 물건을 재거나 같은 물건을 다시 잴 때 쓴다.</summary>
        public void Reset()
        {
            _slowSamples = 0;
        }
    }
}
