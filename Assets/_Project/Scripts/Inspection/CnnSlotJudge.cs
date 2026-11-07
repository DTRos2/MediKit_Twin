using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// CNN 판정(FR-07). 박스를 찍어 슬롯별 크롭을 만들고, 분류 결과를 레시피의 구성품과 슬롯별로 비교한다.
    /// 분류가 레시피와 다르면(빈 슬롯이거나 다른 구성품) 그 슬롯을 MissingSlots에 넣는다. 규칙 판정과 같은 기준이다.
    /// Confidence는 슬롯별 신뢰도 중 가장 낮은 값이다. 정답 라벨(GetMissingAnswerForEvaluation)은 읽지 않는다.
    /// </summary>
    public sealed class CnnSlotJudge : IInspector
    {
        private readonly Func<string, RecipeSO> _findRecipe;
        private readonly ISlotCropSource _cropSource;
        private readonly ISlotClassifier _classifier;

        public CnnSlotJudge(Func<string, RecipeSO> findRecipe, ISlotCropSource cropSource, ISlotClassifier classifier)
        {
            _findRecipe = findRecipe ?? throw new ArgumentNullException(nameof(findRecipe));
            _cropSource = cropSource ?? throw new ArgumentNullException(nameof(cropSource));
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        }

        public JudgeMethod Method => JudgeMethod.Cnn;

        /// <summary>레시피, 크롭, 모델 중 하나라도 없으면 판정할 수 없어 false이다(호출하는 쪽이 규칙 판정으로 대체).</summary>
        public bool TryJudge(KitData kit, out InspectionVerdict verdict)
        {
            verdict = default;

            RecipeSO recipe = kit != null ? _findRecipe(kit.KitCode) : null;
            if (recipe == null || !_cropSource.TryGetCrops(kit, recipe, out SlotCropSet crops))
            {
                return false;
            }

            using (crops)
            {
                var images = new List<UnityEngine.Texture2D>(crops.Crops.Count);
                foreach (SlotCrop crop in crops.Crops)
                {
                    images.Add(crop.Image);
                }

                if (images.Count == 0 || !_classifier.TryClassify(images, out SlotPrediction[] predictions)
                    || predictions == null || predictions.Length != images.Count)
                {
                    return false;
                }

                var expectedBySlot = new Dictionary<int, string>();
                foreach (RecipeItem item in recipe.Items)
                {
                    expectedBySlot[item.SlotNumber] = item.ComponentId;
                }

                var mismatched = new List<int>();
                float lowest = 1f;
                for (int i = 0; i < images.Count; i++)
                {
                    int slot = crops.Crops[i].SlotNumber;
                    lowest = Math.Min(lowest, predictions[i].Confidence);
                    if (!expectedBySlot.TryGetValue(slot, out string expected) || predictions[i].Label != expected)
                    {
                        mismatched.Add(slot);
                        UnityEngine.Debug.Log($"[CnnSlotJudge] 로트 {kit.LotNumber} 슬롯 {slot}: 기대 {(expected ?? "(없음)")}, 예측 {predictions[i].Label} ({predictions[i].Confidence:F2})");
                    }
                }

                verdict = new InspectionVerdict(mismatched.Count == 0, mismatched, lowest, JudgeMethod.Cnn);
                return true;
            }
        }
    }
}
