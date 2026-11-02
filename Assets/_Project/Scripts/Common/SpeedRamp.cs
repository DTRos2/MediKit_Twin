using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 속도를 한 프레임에 계단처럼 바꾸지 않고 가속도 한계 안에서 올리고 내린다. 남은 거리로 멈출 수 있는 속도(√(2·가속도·남은 거리))를
    /// 넘지 않게 하므로 목표 앞에서 부드럽게 줄어 멈춘다. 길이(m)든 각도(도)든 단위만 맞으면 같이 쓴다.
    /// 가속도를 0으로 두면 한계 없이 바로 최고 속도로 간다(옛 동작). 프레임 간격이 달라도 멈추는 자리는 남은 거리로 정해지므로 같다.
    /// 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public sealed class SpeedRamp
    {
        private readonly float _maxSpeed;
        private readonly float _acceleration;

        /// <param name="maxSpeed">최고 속도(0 초과). 거리 단위/초.</param>
        /// <param name="acceleration">가속·감속 크기(0 이상). 거리 단위/초². 0이면 가감속 없이 최고 속도로 간다.</param>
        public SpeedRamp(float maxSpeed, float acceleration)
        {
            if (maxSpeed <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(maxSpeed), "최고 속도는 0보다 커야 한다.");
            }

            if (acceleration < 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(acceleration), "가속도는 0 이상이어야 한다.");
            }

            _maxSpeed = maxSpeed;
            _acceleration = acceleration;
        }

        /// <summary>지금 속도(0 이상). 움직이지 않으면 0.</summary>
        public float Speed { get; private set; }

        /// <summary>
        /// 한 프레임 동안 나아갈 거리를 돌려주고 속도를 갱신한다. 남은 거리를 넘지 않는다.
        /// </summary>
        /// <param name="remaining">멈추고 싶은 자리까지 남은 거리. 0 이하이면 멈춘 것으로 보고 0을 돌려준다.</param>
        /// <param name="deltaTime">지난 시간(초).</param>
        public float Advance(float remaining, float deltaTime)
        {
            if (remaining <= 0f || deltaTime <= 0f)
            {
                Speed = 0f;
                return 0f;
            }

            if (_acceleration <= 0f)
            {
                Speed = _maxSpeed;
                return Mathf.Min(Speed * deltaTime, remaining);
            }

            float stoppableSpeed = Mathf.Sqrt(2f * _acceleration * remaining);
            float desired = Mathf.Min(_maxSpeed, stoppableSpeed);
            Speed = Mathf.MoveTowards(Speed, desired, _acceleration * deltaTime);
            return Mathf.Min(Speed * deltaTime, remaining);
        }

        /// <summary>멈춘 것으로 되돌린다. 목표에 닿았거나 다른 움직임으로 바뀔 때 부른다.</summary>
        public void Reset()
        {
            Speed = 0f;
        }
    }
}
