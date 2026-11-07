using System;
using System.Collections.Generic;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 선반 칸 안 슬롯의 점유 상태. 슬롯 번호는 앞줄 왼쪽부터 매긴다(0 ~ 용량-1, 한 줄에 slotsPerRow개).
    /// 물건은 앞줄에서부터 집히고, 앞줄이 모두 비면 뒤쪽 물건들이 한 줄씩 앞으로 밀려 나온다.
    /// 화면과 무관하게 규칙만 다루므로 표시 방식(애니메이션)을 바꿔도 이 규칙은 바뀌지 않는다.
    /// </summary>
    public sealed class ShelfCellSlots
    {
        private readonly bool[] _occupied;
        private readonly int _slotsPerRow;

        /// <summary>처음에는 모든 슬롯이 차 있다. 에디터가 용량만큼 물건을 배치해 두기 때문이다.</summary>
        public ShelfCellSlots(int capacity, int slotsPerRow)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "용량은 1 이상이어야 한다.");
            }

            if (slotsPerRow < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotsPerRow), "한 줄의 슬롯 수는 1 이상이어야 한다.");
            }

            _occupied = new bool[capacity];
            _slotsPerRow = slotsPerRow;
            for (int i = 0; i < capacity; i++)
            {
                _occupied[i] = true;
            }

            Quantity = capacity;
        }

        public int Capacity => _occupied.Length;

        public int Quantity { get; private set; }

        public bool IsOccupied(int slot) => slot >= 0 && slot < _occupied.Length && _occupied[slot];

        /// <summary>
        /// 수량을 맞춘다(용량을 넘으면 용량으로 줄인다). 줄일 때는 앞줄부터 하나씩 집고, 앞줄이 비면 뒤쪽을 앞으로 민다.
        /// 늘릴 때는 앞쪽 빈 슬롯부터 채운다. 일어난 변화를 순서대로 돌려준다.
        /// </summary>
        public IReadOnlyList<ShelfCellStep> SetQuantity(int quantity)
        {
            int target = Math.Max(0, Math.Min(quantity, Capacity));
            var steps = new List<ShelfCellStep>();

            while (Quantity > target)
            {
                int slot = FirstSlot(isOccupied: true);
                _occupied[slot] = false;
                Quantity--;
                steps.Add(ShelfCellStep.Vacate(slot));
                SlideForward(steps);
            }

            while (Quantity < target)
            {
                int slot = FirstSlot(isOccupied: false);
                _occupied[slot] = true;
                Quantity++;
                steps.Add(ShelfCellStep.Fill(slot));
            }

            return steps;
        }

        private int FirstSlot(bool isOccupied)
        {
            for (int i = 0; i < _occupied.Length; i++)
            {
                if (_occupied[i] == isOccupied)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("조건에 맞는 슬롯이 없다.");
        }

        // 앞줄이 비었는데 뒤에 물건이 남아 있으면 모든 물건을 한 줄씩 앞으로 옮긴다.
        // 번호가 작은 슬롯부터 옮기므로 도착 슬롯은 항상 이미 비어 있다.
        private void SlideForward(List<ShelfCellStep> steps)
        {
            while (Quantity > 0 && IsRowEmpty(0))
            {
                for (int slot = _slotsPerRow; slot < _occupied.Length; slot++)
                {
                    if (!_occupied[slot])
                    {
                        continue;
                    }

                    int destination = slot - _slotsPerRow;
                    _occupied[slot] = false;
                    _occupied[destination] = true;
                    steps.Add(ShelfCellStep.Move(slot, destination));
                }
            }
        }

        private bool IsRowEmpty(int row)
        {
            int start = row * _slotsPerRow;
            int end = Math.Min(start + _slotsPerRow, _occupied.Length);
            for (int i = start; i < end; i++)
            {
                if (_occupied[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
