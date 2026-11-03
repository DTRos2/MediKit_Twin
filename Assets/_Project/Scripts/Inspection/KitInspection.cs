using System;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 키트 1개를 판정하는 순서. 주 판정기(CNN)가 있으면 먼저 시도하고, 판정할 수 없으면 경고를 남긴 뒤
    /// 슬롯 센서를 읽어 규칙 판정으로 대체한다. (FR-07 예외 처리) 규칙 판정까지 못 하면 불량으로 본다.
    /// 판정 결과를 KitData.Verdict에 넣고 InspectionResult를 보내는 일은 호출하는 쪽이 한다.
    /// </summary>
    public sealed class KitInspection
    {
        public const string SensorWarningCode = "SLOT_SENSOR_NO_RESPONSE";
        public const string FallbackWarningCode = "INSPECTION_FALLBACK";
        public const string RecipeWarningCode = "INSPECTION_RECIPE_MISSING";

        private readonly Func<string, RecipeSO> _findRecipe;
        private readonly IInspector _primary;
        private readonly RuleInspector _rule;
        private readonly Action<string, string> _warn;

        public KitInspection(Func<string, RecipeSO> findRecipe, IInspector primary, Action<string, string> warn)
        {
            _findRecipe = findRecipe ?? throw new ArgumentNullException(nameof(findRecipe));
            _primary = primary;
            _warn = warn ?? ((code, message) => { });
            _rule = new RuleInspector(findRecipe);
        }

        public InspectionVerdict Run(KitData kit, ISlotReader reader)
        {
            if (_primary != null)
            {
                if (_primary.TryJudge(kit, out InspectionVerdict primaryVerdict))
                {
                    return primaryVerdict;
                }

                _warn(FallbackWarningCode, $"{_primary.Method} 판정을 할 수 없어 규칙 판정으로 대체한다. (로트 {kit.LotNumber})");
            }

            RecipeSO recipe = _findRecipe(kit.KitCode);
            if (recipe != null)
            {
                string[] ids = null;
                bool read = reader != null && reader.TryRead(recipe.Items.Count, out ids);
                kit.DetectedComponentIds = read ? ids : null;
                if (!read)
                {
                    _warn(SensorWarningCode, $"슬롯 센서가 응답하지 않아 불량으로 판정한다. (로트 {kit.LotNumber})");
                }
            }

            if (_rule.TryJudge(kit, out InspectionVerdict verdict))
            {
                return verdict;
            }

            _warn(RecipeWarningCode, $"키트 코드 '{kit.KitCode}'의 레시피가 없어 불량으로 판정한다. (로트 {kit.LotNumber})");
            return new InspectionVerdict(false, null, 1f, JudgeMethod.Rule);
        }
    }
}
