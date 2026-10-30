using System.Collections.Generic;
using MediKitTwin.Recipes;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 사람이 선반 칸을 골라 키트를 만들 때의 진행 상태: 고른 키트 종류(레시피)와 박스에서 이미 채운 슬롯.
    /// 선반 칸이 그 키트의 구성품인지, 이미 채운 슬롯은 아닌지 판단한다. 순수 클래스라 씬 없이 시험한다.
    /// 박스가 바뀌면(치웠거나 새로 내려옴) 채운 슬롯 기록도 비운다.
    /// </summary>
    public sealed class ManualKittingSession
    {
        private readonly HashSet<int> _filledSlots = new HashSet<int>();
        private object _boxIdentity;

        public RecipeSO Recipe { get; private set; }

        public int FilledCount => _filledSlots.Count;

        public bool IsComplete => Recipe != null && Recipe.Items.Count > 0 && FilledCount >= Recipe.Items.Count;

        /// <summary>키트 종류를 고른다. 채운 슬롯 기록은 비운다(다른 키트의 슬롯 번호와 섞이지 않게 한다).</summary>
        public void SelectRecipe(RecipeSO recipe)
        {
            Recipe = recipe;
            _filledSlots.Clear();
        }

        /// <summary>지금 지그 위 박스를 알린다. 이전과 다른 박스이면 채운 슬롯 기록을 비운다.</summary>
        public void NotifyBox(object boxIdentity)
        {
            if (ReferenceEquals(_boxIdentity, boxIdentity))
            {
                return;
            }

            _boxIdentity = boxIdentity;
            _filledSlots.Clear();
        }

        /// <summary>채운 슬롯 기록을 비운다. 박스를 새로 시작할 때 부른다.</summary>
        public void Reset()
        {
            _filledSlots.Clear();
        }

        /// <summary>이 선반 칸의 구성품을 놓을 레시피 항목을 찾는다. 놓을 수 없으면 이유를 돌려준다.</summary>
        public bool TryPrepare(string shelfSlotId, out RecipeItem item, out string reason)
        {
            item = default;

            if (Recipe == null)
            {
                reason = "키트 종류를 먼저 고른다.";
                return false;
            }

            foreach (RecipeItem candidate in Recipe.Items)
            {
                if (candidate.ShelfSlotId != shelfSlotId)
                {
                    continue;
                }

                if (_filledSlots.Contains(candidate.SlotNumber))
                {
                    reason = $"슬롯 {candidate.SlotNumber}({candidate.ComponentId})는 이미 채웠다.";
                    return false;
                }

                item = candidate;
                reason = string.Empty;
                return true;
            }

            reason = $"{shelfSlotId}는 {Recipe.KitCode} 키트의 구성품이 아니다.";
            return false;
        }

        public void MarkFilled(int slotNumber)
        {
            _filledSlots.Add(slotNumber);
        }
    }
}
