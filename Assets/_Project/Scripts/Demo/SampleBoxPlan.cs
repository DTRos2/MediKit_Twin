using System;
using System.Collections.Generic;
using MediKitTwin.Recipes;

namespace MediKitTwin.Demo
{
    /// <summary>검사 구역에 만들어 볼 샘플 박스의 종류.</summary>
    public enum SampleVariant
    {
        Normal,
        Missing,
        WrongItem,
        SensorFailure
    }

    /// <summary>샘플 박스의 슬롯별 내용을 정한다. 장면과 무관한 계산이라 따로 두어 테스트한다.</summary>
    public static class SampleBoxPlan
    {
        /// <summary>
        /// 슬롯 번호 - 1을 인덱스로 하는 구성품 ID 목록을 만든다. 빈 슬롯은 null이다.
        /// 누락은 targetSlot을 비우고, 오구성은 targetSlot에 allComponentIds 중 다른 구성품을 넣는다.
        /// 정상과 센서 무응답은 레시피대로 채운다.
        /// </summary>
        public static string[] Plan(RecipeSO recipe, SampleVariant variant, int targetSlot, IReadOnlyList<string> allComponentIds, Random rng)
        {
            var contents = new string[recipe.Items.Count];
            foreach (RecipeItem item in recipe.Items)
            {
                contents[item.SlotNumber - 1] = item.ComponentId;
            }

            if (variant == SampleVariant.Missing)
            {
                contents[targetSlot - 1] = null;
            }
            else if (variant == SampleVariant.WrongItem)
            {
                contents[targetSlot - 1] = PickOther(contents[targetSlot - 1], allComponentIds, rng);
            }

            return contents;
        }

        private static string PickOther(string correct, IReadOnlyList<string> allComponentIds, Random rng)
        {
            var others = new List<string>();
            foreach (string id in allComponentIds)
            {
                if (id != correct && !others.Contains(id))
                {
                    others.Add(id);
                }
            }

            if (others.Count == 0)
            {
                throw new InvalidOperationException("바꿔 넣을 다른 구성품이 없다.");
            }

            return others[rng.Next(others.Count)];
        }
    }
}
