using System.Collections.Generic;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 검사 카메라로 찍은 박스 전체 이미지에서 레시피의 슬롯 위치별로 크롭을 잘라 낸다. (FR-07)
    /// 합성 데이터 생성기가 쓰는 계산(TopDownCameraModel.TryGetCropRect, 같은 크롭 크기와 기준 높이)을 그대로 써서
    /// 학습 때 본 크롭과 판정 때의 크롭이 같은 모양이 되게 한다.
    /// </summary>
    public static class SlotCropper
    {
        /// <summary>
        /// 슬롯 수만큼 크롭을 만든다. 이미지 크기가 카메라 설정과 다르거나 크롭이 이미지 밖으로 나가면
        /// 슬롯별 판정을 할 수 없으므로 false이고 만든 크롭은 모두 지운다.
        /// </summary>
        public static bool TryCrop(Texture2D full, RecipeSO recipe, InspectionCameraSettingsSO settings, out SlotCropSet crops)
        {
            crops = null;
            TopDownCameraModel model = settings.CreateModel();
            if (full == null || full.width != model.ImageWidth || full.height != model.ImageHeight)
            {
                return false;
            }

            var items = new List<RecipeItem>(recipe.Items);
            items.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));

            var made = new List<SlotCrop>(items.Count);
            foreach (RecipeItem item in items)
            {
                var center = new Vector3(item.SlotLocalPosition.x, settings.CropPlaneLocalY, item.SlotLocalPosition.z);
                if (!model.TryGetCropRect(center, settings.CropSizeMeters, out RectInt rect))
                {
                    new SlotCropSet(made).Dispose();
                    return false;
                }

                var image = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
                image.SetPixels(full.GetPixels(rect.x, rect.y, rect.width, rect.height));
                image.Apply();
                made.Add(new SlotCrop(item.SlotNumber, rect, image));
            }

            crops = new SlotCropSet(made);
            return true;
        }
    }
}
