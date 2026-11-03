using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>슬롯 크롭 하나의 분류 결과. Label은 "empty" 또는 구성품 ID이다.</summary>
    public readonly struct SlotPrediction
    {
        public SlotPrediction(string label, float confidence)
        {
            Label = label;
            Confidence = confidence;
        }

        public string Label { get; }

        /// <summary>선택한 클래스의 softmax 확률(0~1).</summary>
        public float Confidence { get; }
    }

    /// <summary>슬롯 크롭을 분류하는 접점. 구현은 Sentis 모델이고, 판정 로직과 테스트는 이 인터페이스만 안다.</summary>
    public interface ISlotClassifier
    {
        /// <summary>
        /// 크롭마다 분류 결과를 돌려준다(입력과 같은 순서). 모델이 없거나 추론에 실패하면 false이다. (FR-07 예외 처리)
        /// </summary>
        bool TryClassify(IReadOnlyList<Texture2D> crops, out SlotPrediction[] predictions);
    }

    /// <summary>판정할 키트의 박스를 찍어 슬롯 크롭을 만드는 접점. 호출한 쪽이 SlotCropSet을 Dispose한다.</summary>
    public interface ISlotCropSource
    {
        bool TryGetCrops(Common.KitData kit, Recipes.RecipeSO recipe, out SlotCropSet crops);
    }
}
