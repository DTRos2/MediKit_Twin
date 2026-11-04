using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 개수가 달라지는 목록(주문 줄, 경고 줄, 로그 줄)을 보여 주는 칸 묶음. 칸은 편집기에서 미리 계층에 놓아 둔 오브젝트이고,
    /// 실행 중에는 오브젝트를 만들지도 부수지도 않고 필요한 만큼만 켜서 내용을 채우고 나머지는 끈다.
    /// 칸 수보다 항목이 많으면 앞의 칸 수만큼만 보이고, 못 보인 개수를 돌려주므로 호출하는 쪽이 "외 N건"처럼 알릴 수 있다.
    /// 줄 안에 무엇을 보여 줄지는 호출하는 쪽이 건네는 bind가 정한다. 어떤 도메인의 줄이든 쓸 수 있도록 줄 종류(TView)와 자료 종류(TItem)를 모른다.
    /// </summary>
    public sealed class UiSlotList<TView> where TView : Component
    {
        private readonly IReadOnlyList<TView> _slots;

        /// <param name="slots">미리 놓아 둔 칸들. 앞에서부터 차례로 채운다.</param>
        public UiSlotList(IReadOnlyList<TView> slots)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
        }

        /// <summary>미리 놓아 둔 칸 수. 한 번에 보여 줄 수 있는 최대 항목 수다.</summary>
        public int Capacity => _slots.Count;

        /// <summary>지금 켜져 있는 칸 수.</summary>
        public int ActiveCount { get; private set; }

        /// <summary>목록을 보여 준다. 쓰는 칸은 켜서 채우고 남는 칸은 끈다. 칸에 들어가지 못한 항목의 개수를 돌려준다.</summary>
        public int Show<TItem>(IReadOnlyList<TItem> items, Action<TView, TItem> bind)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (bind == null)
            {
                throw new ArgumentNullException(nameof(bind));
            }

            int shown = Math.Min(items.Count, _slots.Count);
            for (int i = 0; i < _slots.Count; i++)
            {
                bool isUsed = i < shown;
                _slots[i].gameObject.SetActive(isUsed);
                if (isUsed)
                {
                    bind(_slots[i], items[i]);
                }
            }

            ActiveCount = shown;
            return items.Count - shown;
        }
    }
}
