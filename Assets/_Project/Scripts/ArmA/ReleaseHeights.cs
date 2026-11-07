using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건을 놓을 때 시도할 높이들(슬롯 중심 위로 몇 m인지)을 낮은 것부터 모아 둔다.
    /// 낮게 놓을수록 물건이 덜 떨어져 조심스럽지만 손가락이 벽과 이웃 물건 사이로 들어가야 한다.
    /// 그래서 슬롯마다 닿는 가장 낮은 높이를 고르려고, 호출하는 쪽이 이 순서대로 시도한다.
    /// 값은 0 이상으로 고치고 오름차순으로 정렬하며 같은 값은 하나로 합친다. 비어 있으면 슬롯 중심(0) 하나로 본다.
    /// </summary>
    public sealed class ReleaseHeights : IReadOnlyList<float>
    {
        // 이보다 가까운 두 높이는 같은 높이로 보고 한 번만 시도한다(사람이 인스펙터에 적은 값의 오차).
        private const float SameHeightToleranceM = 1e-4f;

        private readonly float[] _ascending;

        /// <summary>높이를 따로 정하지 않았을 때: 슬롯 중심에서 놓는다.</summary>
        public static ReleaseHeights AtSlotCenter { get; } = new ReleaseHeights(null);

        public ReleaseHeights(IEnumerable<float> heightsM)
        {
            _ascending = Normalize(heightsM);
        }

        public int Count => _ascending.Length;

        public float this[int index] => _ascending[index];

        /// <summary>가장 높은 값. 낮은 높이가 모두 안 될 때 마지막으로 시도하는 높이다.</summary>
        public float Highest => _ascending[_ascending.Length - 1];

        public IEnumerator<float> GetEnumerator()
        {
            return ((IEnumerable<float>)_ascending).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static float[] Normalize(IEnumerable<float> heightsM)
        {
            var values = new List<float>();
            if (heightsM != null)
            {
                foreach (float height in heightsM)
                {
                    // 슬롯 중심보다 아래에서 놓을 수는 없고, 숫자가 아닌 값은 버린다.
                    if (!float.IsNaN(height) && !float.IsInfinity(height))
                    {
                        values.Add(Mathf.Max(0f, height));
                    }
                }
            }

            values.Sort();

            var distinct = new List<float>();
            foreach (float value in values)
            {
                if (distinct.Count == 0 || value - distinct[distinct.Count - 1] > SameHeightToleranceM)
                {
                    distinct.Add(value);
                }
            }

            if (distinct.Count == 0)
            {
                distinct.Add(0f);
            }

            return distinct.ToArray();
        }
    }
}
