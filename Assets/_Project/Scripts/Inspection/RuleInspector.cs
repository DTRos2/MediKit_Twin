using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 규칙 판정(FR-06). 슬롯 센서가 만든 KitData.DetectedComponentIds를 레시피와 슬롯별로 비교한다.
    /// 빈 슬롯(누락)과 다른 구성품이 든 슬롯(오구성)을 같은 방식으로 잡아 MissingSlots에 넣는다.
    /// 정답 라벨(GetMissingAnswerForEvaluation)은 읽지 않는다.
    /// </summary>
    public sealed class RuleInspector : IInspector
    {
        private readonly Func<string, RecipeSO> _findRecipe;

        public RuleInspector(Func<string, RecipeSO> findRecipe)
        {
            _findRecipe = findRecipe ?? throw new ArgumentNullException(nameof(findRecipe));
        }

        public JudgeMethod Method => JudgeMethod.Rule;

        /// <summary>
        /// 레시피를 찾지 못하면 판정할 수 없어 false이다. 센서 결과가 없거나 슬롯 수가 어긋나면
        /// 센서 무응답(FR-06 예외)이므로 불량이며 어느 슬롯인지 알 수 없어 MissingSlots는 null이다.
        /// </summary>
        public bool TryJudge(KitData kit, out InspectionVerdict verdict)
        {
            verdict = default;

            RecipeSO recipe = kit != null ? _findRecipe(kit.KitCode) : null;
            if (recipe == null)
            {
                return false;
            }

            string[] detected = kit.DetectedComponentIds;
            if (detected == null || detected.Length != recipe.Items.Count)
            {
                verdict = new InspectionVerdict(false, null, 1f, JudgeMethod.Rule);
                return true;
            }

            var mismatched = new List<int>();
            foreach (RecipeItem item in recipe.Items)
            {
                if (detected[item.SlotNumber - 1] != item.ComponentId)
                {
                    mismatched.Add(item.SlotNumber);
                }
            }

            verdict = new InspectionVerdict(mismatched.Count == 0, mismatched, 1f, JudgeMethod.Rule);
            return true;
        }
    }
}
