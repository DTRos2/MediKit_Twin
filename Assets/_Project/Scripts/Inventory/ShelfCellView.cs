using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 선반 칸 하나의 화면. 에디터가 칸 안에 미리 놓아 둔 물건(Item_*)을 슬롯으로 삼고,
    /// 수량이 줄면 앞줄 물건부터 없애고 앞줄이 비면 뒤쪽 물건을 앞으로 미끄러뜨린다.
    /// 어떤 물건을 언제 움직일지는 ShelfCellSlots가 정하고, 이 클래스는 그 결과를 화면에 옮기기만 한다.
    /// </summary>
    public sealed class ShelfCellView : MonoBehaviour
    {
        [SerializeField] private string _shelfSlotId;
        [SerializeField, Min(1)] private int _slotsPerRow = 1;

        [Tooltip("뒤쪽 물건이 앞으로 밀려 나오는 속도(m/s). 시뮬레이션 시간(배속)을 따른다.")]
        [SerializeField, Min(0.01f)] private float _slideSpeed = 0.4f;

        private ShelfCellSlots _slots;
        private Transform[] _itemAtSlot;
        private Vector3[] _slotPositions;
        private readonly Stack<Transform> _spareItems = new Stack<Transform>();
        private SlideAnimator _slider;

        public string ShelfSlotId => _shelfSlotId;

        private void Awake()
        {
            _slider = new SlideAnimator(_slideSpeed);
            CollectItems();
        }

        private void Update()
        {
            _slider.Step(Time.deltaTime);
        }

        /// <summary>
        /// 지금 집을 수 있는 물건(앞쪽 슬롯에서부터 처음 만나는 것)을 돌려준다. 물건이 없으면 false이다.
        /// 아직 미끄러져 오는 중인 물건이면 그 순간의 위치를 돌려준다.
        /// </summary>
        public bool TryGetFrontItem(out Transform item)
        {
            item = null;
            if (_slots == null)
            {
                return false;
            }

            for (int slot = 0; slot < _itemAtSlot.Length; slot++)
            {
                if (_slots.IsOccupied(slot) && _itemAtSlot[slot] != null)
                {
                    item = _itemAtSlot[slot];
                    return true;
                }
            }

            return false;
        }

        /// <summary>에디터가 칸을 만들 때 부른다. 칸 ID와 한 줄의 물건 수를 저장해 프리팹에 남긴다.</summary>
        public void Configure(string shelfSlotId, int slotsPerRow)
        {
            _shelfSlotId = shelfSlotId;
            _slotsPerRow = Mathf.Max(1, slotsPerRow);
        }

        /// <summary>칸의 수량을 화면에 반영한다. 용량(놓아 둔 물건 수)을 넘는 수량은 용량으로 본다.</summary>
        public void SetQuantity(int quantity)
        {
            if (_slots == null)
            {
                return;
            }

            foreach (ShelfCellStep step in _slots.SetQuantity(quantity))
            {
                Apply(step);
            }
        }

        // 놓아 둔 물건을 이름 끝의 번호(Item_<구성품>_<번호>) 순서로 슬롯에 대응시킨다. 번호는 에디터가 앞줄부터 매긴다.
        private void CollectItems()
        {
            var items = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Item_"))
                {
                    items.Add(child);
                }
            }

            items.Sort((a, b) => ItemNumber(a).CompareTo(ItemNumber(b)));

            if (items.Count == 0)
            {
                return;
            }

            _itemAtSlot = items.ToArray();
            _slotPositions = new Vector3[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                _slotPositions[i] = items[i].localPosition;
            }

            _slots = new ShelfCellSlots(items.Count, _slotsPerRow);
        }

        private static int ItemNumber(Transform item)
        {
            string name = item.name;
            int underscore = name.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int number) ? number : int.MaxValue;
        }

        private void Apply(ShelfCellStep step)
        {
            switch (step.Kind)
            {
                case ShelfCellStepKind.Vacate:
                    Vacate(step.FromSlot);
                    break;
                case ShelfCellStepKind.Move:
                    Move(step.FromSlot, step.ToSlot);
                    break;
                case ShelfCellStepKind.Fill:
                    Fill(step.ToSlot);
                    break;
            }
        }

        private void Vacate(int slot)
        {
            Transform item = _itemAtSlot[slot];
            if (item == null)
            {
                return;
            }

            _slider.Stop(item);
            item.gameObject.SetActive(false);
            _spareItems.Push(item);
            _itemAtSlot[slot] = null;
        }

        private void Move(int fromSlot, int toSlot)
        {
            Transform item = _itemAtSlot[fromSlot];
            if (item == null)
            {
                return;
            }

            _itemAtSlot[toSlot] = item;
            _itemAtSlot[fromSlot] = null;
            _slider.SlideTo(item, _slotPositions[toSlot]);
        }

        private void Fill(int slot)
        {
            if (_spareItems.Count == 0)
            {
                return;
            }

            Transform item = _spareItems.Pop();
            item.localPosition = _slotPositions[slot];
            item.gameObject.SetActive(true);
            _itemAtSlot[slot] = item;
        }
    }
}
