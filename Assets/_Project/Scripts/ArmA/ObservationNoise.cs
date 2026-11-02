using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 센서 잡음. 위치에 더할 가우시안 편차(m)를 만든다. 씨앗이 같으면 같은 순서로 같은 값이 나와 시험이 흔들리지 않는다.
    /// 극단값이 팔을 크게 움직이지 않도록 표준편차의 3배까지만 허용한다.
    /// </summary>
    public sealed class ObservationNoise
    {
        private const float MaxSigmaMultiple = 3f;

        private readonly System.Random _random;
        private readonly float _sigmaM;

        public ObservationNoise(float sigmaM, int seed)
        {
            _sigmaM = Mathf.Max(0f, sigmaM);
            _random = new System.Random(seed);
        }

        /// <summary>잡음이 없는 센서.</summary>
        public static ObservationNoise None => new ObservationNoise(0f, 0);

        public Vector3 SampleOffset()
        {
            if (_sigmaM <= 0f)
            {
                return Vector3.zero;
            }

            return new Vector3(SampleAxis(), SampleAxis(), SampleAxis());
        }

        private float SampleAxis()
        {
            // Box-Muller 변환.
            double u1 = 1.0 - _random.NextDouble();
            double u2 = _random.NextDouble();
            double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return Mathf.Clamp((float)normal, -MaxSigmaMultiple, MaxSigmaMultiple) * _sigmaM;
        }
    }
}
