using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 손으로 정하는 관절 목표 각도를 들고 있다. 슬라이더와 +/- 버튼이 어떤 값을 내든 관절 한계 안으로 잘라 주고,
    /// 화면은 도(degree)로, 로봇에는 라디안으로 넘기도록 단위를 한곳에서 바꾼다. 화면을 그리는 코드와 분리해 씬 없이 시험한다.
    /// </summary>
    public sealed class JointTargetEditor
    {
        private readonly float[] _lowerRad;
        private readonly float[] _upperRad;
        private readonly float[] _targetRad;

        public JointTargetEditor(IReadOnlyList<float> lowerRad, IReadOnlyList<float> upperRad)
        {
            if (lowerRad == null || upperRad == null)
            {
                throw new ArgumentNullException(lowerRad == null ? nameof(lowerRad) : nameof(upperRad));
            }

            if (lowerRad.Count != upperRad.Count)
            {
                throw new ArgumentException("아래 한계와 위 한계의 개수가 같아야 한다.");
            }

            _lowerRad = Copy(lowerRad);
            _upperRad = Copy(upperRad);
            _targetRad = new float[_lowerRad.Length];
        }

        public int Count => _targetRad.Length;

        public float LowerDeg(int index) => _lowerRad[index] * Mathf.Rad2Deg;

        public float UpperDeg(int index) => _upperRad[index] * Mathf.Rad2Deg;

        public float TargetDeg(int index) => _targetRad[index] * Mathf.Rad2Deg;

        /// <summary>모든 목표를 주어진 각도(rad)로 맞춘다. 한계 밖의 값은 한계로 자른다.</summary>
        public void Reset(IReadOnlyList<float> anglesRad)
        {
            if (anglesRad == null || anglesRad.Count != Count)
            {
                throw new ArgumentException($"각도 {Count}개가 필요하다.", nameof(anglesRad));
            }

            for (int i = 0; i < Count; i++)
            {
                _targetRad[i] = Mathf.Clamp(anglesRad[i], _lowerRad[i], _upperRad[i]);
            }
        }

        /// <summary>한 관절의 목표를 도 단위로 정한다. 한계로 잘린 뒤의 값(도)을 돌려준다.</summary>
        public float SetTargetDeg(int index, float degrees)
        {
            _targetRad[index] = Mathf.Clamp(degrees * Mathf.Deg2Rad, _lowerRad[index], _upperRad[index]);
            return TargetDeg(index);
        }

        /// <summary>한 관절의 목표를 지금 값에서 deltaDeg(도)만큼 옮긴다. 한계로 잘린 뒤의 값(도)을 돌려준다.</summary>
        public float NudgeDeg(int index, float deltaDeg)
        {
            return SetTargetDeg(index, TargetDeg(index) + deltaDeg);
        }

        /// <summary>목표를 라디안 배열로 복사한다. destination 길이는 Count와 같아야 한다.</summary>
        public void CopyTargetsTo(float[] destination)
        {
            if (destination == null || destination.Length != Count)
            {
                throw new ArgumentException($"길이 {Count}인 배열이 필요하다.", nameof(destination));
            }

            Array.Copy(_targetRad, destination, Count);
        }

        private static float[] Copy(IReadOnlyList<float> source)
        {
            var copy = new float[source.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }
}
