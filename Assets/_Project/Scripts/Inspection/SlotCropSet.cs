using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.Inspection
{
    /// <summary>슬롯 하나를 잘라 낸 이미지. 슬롯 번호는 레시피의 번호와 같고, Rect는 전체 이미지 안의 픽셀 영역(왼쪽 아래 원점)이다.</summary>
    public readonly struct SlotCrop
    {
        public SlotCrop(int slotNumber, RectInt rect, Texture2D image)
        {
            SlotNumber = slotNumber;
            Rect = rect;
            Image = image;
        }

        public int SlotNumber { get; }

        public RectInt Rect { get; }

        public Texture2D Image { get; }
    }

    /// <summary>
    /// 박스 하나의 슬롯 크롭 모음. 이미지 텍스처를 소유하므로 다 쓰면 Dispose로 지운다.
    /// 목록은 슬롯 번호 순서이다.
    /// </summary>
    public sealed class SlotCropSet : IDisposable
    {
        private readonly List<SlotCrop> _crops;

        public SlotCropSet(List<SlotCrop> crops)
        {
            _crops = crops;
        }

        public IReadOnlyList<SlotCrop> Crops => _crops;

        public void Dispose()
        {
            foreach (SlotCrop crop in _crops)
            {
                if (crop.Image == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(crop.Image);
                }
                else
                {
                    Object.DestroyImmediate(crop.Image);
                }
            }

            _crops.Clear();
        }
    }
}
