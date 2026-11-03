using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 슬롯 자리와 구성품(선반 칸)마다 놓기에 성공한 방식(PlacementRecall)을 기억한다.
    /// 슬롯 하나에 놓는 방식은 팔과 박스 사이의 기하로 거의 정해져서(손목이 벽을 넘는 높이, 팔이 닿는 방향) 키트를 거듭해도 같다.
    /// 그런데 매번 낮은 높이부터 방향을 처음부터 찾고, 내려가지 못하면 시간 초과를 기다리느라 한 슬롯에 1분 가까이 걸린 일이 있어 기억해 둔다.
    /// 박스가 지그에 멈추는 자리가 조금씩 다르므로 같은 슬롯인지는 거리(slotMatchRadiusM)로 알아본다.
    /// 구성품을 따로 기억하는 것은 같은 슬롯이라도 구성품마다 쥐는 자세가 달라 닿는 방향이 다를 수 있어서다.
    /// 구성품은 다르지만 같은 슬롯에 성공한 적이 있다면 그중 가장 낮은 높이를 시작점으로 알려 주어(TryGetLowestHeightIndex) 더 낮은 높이를 헛되이 시도하지 않게 한다.
    /// Unity 씬을 쓰지 않아 시험으로 확인한다.
    /// </summary>
    public sealed class PlacementMemory
    {
        private readonly float _slotMatchRadiusM;
        private readonly List<Entry> _entries = new List<Entry>();

        /// <param name="slotMatchRadiusM">이 거리 안이면 같은 슬롯으로 본다(m). 이웃 슬롯 사이 간격보다 작아야 한다.</param>
        public PlacementMemory(float slotMatchRadiusM)
        {
            if (slotMatchRadiusM <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(slotMatchRadiusM), "같은 슬롯으로 볼 거리는 0보다 커야 한다.");
            }

            _slotMatchRadiusM = slotMatchRadiusM;
        }

        /// <summary>기억해 둔 방식의 수.</summary>
        public int Count => _entries.Count;

        /// <summary>같은 슬롯에 같은 구성품을 놓았던 방식이 있으면 true.</summary>
        public bool TryRecall(Vector3 slotPosition, string itemKey, out PlacementRecall recall)
        {
            Entry? found = Find(slotPosition, itemKey);
            recall = found.HasValue ? found.Value.Recall : default;
            return found.HasValue;
        }

        /// <summary>
        /// 같은 슬롯에 성공한 적이 있는 방식 중 가장 낮은 놓는 높이의 순번. 구성품은 따지지 않는다. 기억이 없으면 false.
        /// 그보다 낮은 높이는 이 슬롯에서 어느 구성품도 성공한 적이 없다.
        /// </summary>
        public bool TryGetLowestHeightIndex(Vector3 slotPosition, out int heightIndex)
        {
            heightIndex = int.MaxValue;
            foreach (Entry entry in _entries)
            {
                if (IsSameSlot(entry.SlotPosition, slotPosition))
                {
                    heightIndex = Math.Min(heightIndex, entry.Recall.HeightIndex);
                }
            }

            bool isFound = heightIndex != int.MaxValue;
            if (!isFound)
            {
                heightIndex = 0;
            }

            return isFound;
        }

        /// <summary>성공한 방식을 기억한다. 같은 슬롯과 구성품의 이전 기억은 이것으로 바꾼다.</summary>
        public void Remember(Vector3 slotPosition, string itemKey, PlacementRecall recall)
        {
            string key = Normalize(itemKey);
            int index = IndexOf(slotPosition, key);
            if (index >= 0)
            {
                _entries[index] = new Entry(_entries[index].SlotPosition, key, recall);
                return;
            }

            _entries.Add(new Entry(slotPosition, key, recall));
        }

        /// <summary>그 슬롯과 구성품의 기억을 잊는다. 기억한 방식이 더는 통하지 않을 때 부른다.</summary>
        public void Forget(Vector3 slotPosition, string itemKey)
        {
            int index = IndexOf(slotPosition, Normalize(itemKey));
            if (index >= 0)
            {
                _entries.RemoveAt(index);
            }
        }

        private Entry? Find(Vector3 slotPosition, string itemKey)
        {
            int index = IndexOf(slotPosition, Normalize(itemKey));
            return index >= 0 ? _entries[index] : (Entry?)null;
        }

        private int IndexOf(Vector3 slotPosition, string key)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ItemKey == key && IsSameSlot(_entries[i].SlotPosition, slotPosition))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsSameSlot(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude <= _slotMatchRadiusM * _slotMatchRadiusM;
        }

        // 구성품을 알려 주지 않는 호출은 서로 같은 구성품으로 본다.
        private static string Normalize(string itemKey)
        {
            return itemKey ?? string.Empty;
        }

        private readonly struct Entry
        {
            public Entry(Vector3 slotPosition, string itemKey, PlacementRecall recall)
            {
                SlotPosition = slotPosition;
                ItemKey = itemKey;
                Recall = recall;
            }

            public Vector3 SlotPosition { get; }

            public string ItemKey { get; }

            public PlacementRecall Recall { get; }
        }
    }
}
