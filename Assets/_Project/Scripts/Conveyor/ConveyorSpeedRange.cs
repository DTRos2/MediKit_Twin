using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 속도가 가질 수 있는 범위(m/s)와 값을 범위 안으로 맞추는 규칙. 인스펙터에서 잘못 넣은 값(0 이하, 최소가 최대보다 큼)도
    /// 예외 없이 쓸 수 있는 범위로 바로잡는다. 속도 0은 박스가 멈춰 서므로 절대 허용하지 않는다. Unity 없이 시험할 수 있다.
    /// </summary>
    public readonly struct ConveyorSpeedRange
    {
        /// <summary>범위의 최소가 될 수 있는 가장 작은 값(m/s). 이보다 느리면 사실상 멈춘 것과 같다.</summary>
        public const float SmallestMinMps = 0.05f;

        public ConveyorSpeedRange(float minMps, float maxMps)
        {
            Min = Mathf.Max(SmallestMinMps, minMps);
            Max = Mathf.Max(Min, maxMps);
        }

        public float Min { get; }

        public float Max { get; }

        /// <summary>속도를 범위 안으로 맞춘다. 숫자가 아닌 값(NaN)은 가장 느린 속도로 본다.</summary>
        public float Clamp(float speedMps)
        {
            return float.IsNaN(speedMps) ? Min : Mathf.Clamp(speedMps, Min, Max);
        }
    }
}
