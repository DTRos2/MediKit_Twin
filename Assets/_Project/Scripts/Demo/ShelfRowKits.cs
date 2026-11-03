using System.Collections.Generic;
using MediKitTwin.Recipes;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 행마다 어떤 키트 종류의 재료가 놓이는지(A행은 EM, B행은 BL …). 키트 레시피가 구성품의 선반 칸을 이미 알고 있으므로
    /// 행과 종류의 짝을 코드에 박아 두지 않고 레시피에서 읽어 온다. 레시피가 바뀌면 화면도 따라 바뀐다.
    /// 한 행을 여러 레시피가 쓰면 먼저 등록한 것이 이긴다.
    /// </summary>
    public sealed class ShelfRowKits
    {
        private readonly Dictionary<char, string> _kitCodeByRow = new Dictionary<char, string>();

        /// <summary>레시피 목록으로 만든다. 비어 있는 항목은 건너뛴다.</summary>
        public static ShelfRowKits FromRecipes(IEnumerable<RecipeSO> recipes)
        {
            var kits = new ShelfRowKits();
            if (recipes == null)
            {
                return kits;
            }

            foreach (RecipeSO recipe in recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                var slotIds = new List<string>();
                foreach (RecipeItem item in recipe.Items)
                {
                    slotIds.Add(item.ShelfSlotId);
                }

                kits.Add(recipe.KitCode, slotIds);
            }

            return kits;
        }

        /// <summary>이 키트 종류의 구성품이 놓이는 칸들로 행과 종류를 짝짓는다.</summary>
        public void Add(string kitCode, IEnumerable<string> shelfSlotIds)
        {
            if (string.IsNullOrEmpty(kitCode) || shelfSlotIds == null)
            {
                return;
            }

            foreach (string slotId in shelfSlotIds)
            {
                if (string.IsNullOrEmpty(slotId))
                {
                    continue;
                }

                // 먼저 등록한 짝을 지키려고 이미 있으면 덮어쓰지 않는다.
                if (!_kitCodeByRow.ContainsKey(slotId[0]))
                {
                    _kitCodeByRow.Add(slotId[0], kitCode);
                }
            }
        }

        /// <summary>그 행의 키트 종류. 알 수 없으면 빈 글자다.</summary>
        public string KitCodeOf(char rowLetter)
        {
            return _kitCodeByRow.TryGetValue(rowLetter, out string kitCode) ? kitCode : string.Empty;
        }
    }
}
