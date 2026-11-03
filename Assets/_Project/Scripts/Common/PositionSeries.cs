using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 시간 순서로 모은 위치들(m)에서 "얼마나 퍼졌는지"와 "얼마나 떨리는지"를 잰다. 도구, 팔 베이스, 도구 기준 물건 위치처럼
    /// 위치를 프레임마다 모아 흔들림을 따지는 곳에서 같은 계산을 쓰도록 한 곳에 둔 것이다. 씬 없이 시험한다.
    /// 떨림은 연속한 세 표본의 2차 차분(p[i+1] - 2p[i] + p[i-1])으로 잰다. 일정한 속도로 움직이면 0이고 진동하면 커진다.
    /// </summary>
    public sealed class PositionSeries
    {
        // 2차 차분은 세 표본이 있어야 하나 나온다.
        private const int MinSamplesForJitter = 3;

        private readonly List<Vector3> _values = new List<Vector3>();

        public int Count => _values.Count;

        public void Add(Vector3 value)
        {
            _values.Add(value);
        }

        public void Clear()
        {
            _values.Clear();
        }

        /// <summary>모은 값들이 축마다 얼마나 퍼졌는지(최댓값 - 최솟값, m). 비어 있으면 0.</summary>
        public Vector3 Span()
        {
            if (_values.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 min = _values[0];
            Vector3 max = _values[0];
            foreach (Vector3 value in _values)
            {
                min = Vector3.Min(min, value);
                max = Vector3.Max(max, value);
            }

            return max - min;
        }

        /// <summary>2차 차분 크기의 제곱평균 제곱근과 최댓값(m). 표본이 세 개 미만이면 값이 없는 것으로 돌려준다.</summary>
        public JitterStats Jitter()
        {
            if (_values.Count < MinSamplesForJitter)
            {
                return JitterStats.None;
            }

            double sumSquares = 0;
            float max = 0f;
            for (int i = 1; i < _values.Count - 1; i++)
            {
                float magnitude = (_values[i + 1] - 2f * _values[i] + _values[i - 1]).magnitude;
                sumSquares += magnitude * magnitude;
                max = Mathf.Max(max, magnitude);
            }

            double rms = Math.Sqrt(sumSquares / (_values.Count - 2));
            return new JitterStats((float)rms, max);
        }
    }

    /// <summary>PositionSeries.Jitter의 결과. 표본이 모자라 못 잰 경우 HasValue가 false다.</summary>
    public readonly struct JitterStats
    {
        public JitterStats(float rmsM, float maxM)
        {
            HasValue = true;
            RmsM = rmsM;
            MaxM = maxM;
        }

        public static JitterStats None => default;

        public bool HasValue { get; }

        public float RmsM { get; }

        public float MaxM { get; }
    }
}
