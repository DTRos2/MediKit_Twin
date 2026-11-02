using System;
using System.Collections.Generic;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 옮기려던 양을 그대로 못 옮길 때 어디까지 줄여 가며 시도할지 정한다. 전부 옮기는 것부터 시작해 점점 줄인 비율을 차례로 내놓는다.
    /// 예를 들어 카메라로 본 물건 쪽으로 8cm 내리려다 선반 바닥판에 닿는다면, 6cm, 4cm, 2cm 순으로 줄여 닿지 않는 첫 양을 쓰게 한다.
    /// 무엇이 "안 된다"인지는 모르고 비율 목록만 안다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public sealed class ShiftBackoff
    {
        private readonly float[] _fractions;

        /// <param name="fractions">시도할 비율. (0, 1] 범위이고 앞에서부터 큰 값에서 작은 값 순서여야 한다. 맨 앞은 보통 1(전부)이다.</param>
        public ShiftBackoff(IReadOnlyList<float> fractions)
        {
            if (fractions == null || fractions.Count == 0)
            {
                throw new ArgumentException("비율이 하나 이상 있어야 한다.", nameof(fractions));
            }

            _fractions = new float[fractions.Count];
            for (int i = 0; i < fractions.Count; i++)
            {
                float fraction = fractions[i];
                if (fraction <= 0f || fraction > 1f)
                {
                    throw new ArgumentOutOfRangeException(nameof(fractions), "비율은 0보다 크고 1 이하여야 한다.");
                }

                if (i > 0 && fraction >= fractions[i - 1])
                {
                    throw new ArgumentException("비율은 점점 작아져야 한다.", nameof(fractions));
                }

                _fractions[i] = fraction;
            }
        }

        /// <summary>전부, 3/4, 1/2, 1/4. 줄이는 단계가 거칠수록 닿는 곳에 못 미치고, 잘수록 풀이 횟수가 늘어난다.</summary>
        public static ShiftBackoff Default { get; } = new ShiftBackoff(new[] { 1f, 0.75f, 0.5f, 0.25f });

        /// <summary>전부 옮기는 것만 시도한다. 줄여 보지 않는다.</summary>
        public static ShiftBackoff None { get; } = new ShiftBackoff(new[] { 1f });

        /// <summary>큰 비율부터 작은 비율 순서.</summary>
        public IReadOnlyList<float> Fractions => _fractions;
    }
}
