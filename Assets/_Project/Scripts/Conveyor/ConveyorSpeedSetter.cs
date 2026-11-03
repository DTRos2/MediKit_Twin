using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 씬의 모든 컨베이어 구간에 같은 이송 속도를 넣는다. 플레이를 시작할 때 인스펙터의 값 하나로 정하고,
    /// 실행 중에는 속도 조절 패널이 IConveyorSpeedControl로 바꾼다(컨베이어는 속도를 매 프레임 읽으므로 바로 반영된다).
    /// 컨베이어 에셋의 기본 속도(초속 2m)는 키트 박스를 나르기에 너무 빨라 박스와 안의 물건이 휘청이고 지켜보기도 어렵다.
    /// 구간을 바꿔 끼워도(예: 멈춤 구간으로 교체) 넣을 때마다 구간을 새로 찾으므로 구간마다 속도를 손으로 맞출 필요가 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConveyorSpeedSetter : MonoBehaviour, IConveyorSpeedControl
    {
        /// <summary>새로 붙일 때의 속도(m/s). 컨베이어 에셋 기본(2)보다 느리게 잡았다.</summary>
        public const float DefaultSpeedMps = 0.8f;

        /// <summary>속도 조절 범위의 기본 최소(m/s). 이보다 느리면 박스가 기어가 시험을 지켜보기 답답하다.</summary>
        public const float DefaultMinSpeedMps = 0.2f;

        /// <summary>속도 조절 범위의 기본 최대(m/s). 컨베이어 에셋 기본 속도와 같다.</summary>
        public const float DefaultMaxSpeedMps = 2f;

        [Tooltip("컨베이어 이송 속도(m/s). 작을수록 천천히 간다. 씬의 모든 컨베이어 구간에 적용된다.")]
        [SerializeField, Min(ConveyorSpeedRange.SmallestMinMps)] private float _speedMps = DefaultSpeedMps;

        [Tooltip("속도 조절 패널과 이 컴포넌트가 받아들이는 가장 느린 속도(m/s).")]
        [SerializeField, Min(ConveyorSpeedRange.SmallestMinMps)] private float _minSpeedMps = DefaultMinSpeedMps;

        [Tooltip("속도 조절 패널과 이 컴포넌트가 받아들이는 가장 빠른 속도(m/s).")]
        [SerializeField, Min(ConveyorSpeedRange.SmallestMinMps)] private float _maxSpeedMps = DefaultMaxSpeedMps;

        public float SpeedMps => _speedMps;

        public float MinSpeedMps => Range.Min;

        public float MaxSpeedMps => Range.Max;

        private ConveyorSpeedRange Range => new ConveyorSpeedRange(_minSpeedMps, _maxSpeedMps);

        public void SetSpeed(float speedMps)
        {
            _speedMps = Range.Clamp(speedMps);
            ApplyToConveyors();
        }

        private void Awake()
        {
            // 씬에 저장된 값이 범위 밖이어도 시작할 때 범위 안으로 맞춘다.
            _speedMps = Range.Clamp(_speedMps);
            ApplyToConveyors();
        }

        // 비활성 구간도 포함해, 나중에 켜지는 구간이 에셋 기본 속도로 남지 않게 한다.
        private void ApplyToConveyors()
        {
            ConveyorSpeed.Apply(FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include), _speedMps);
        }
    }
}
