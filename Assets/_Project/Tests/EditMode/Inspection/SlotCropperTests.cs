using MediKitTwin.Recipes;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class SlotCropperTests
    {
        // 기본 카메라 설정(높이 0.6m, 시야각 30도, 256x512)과 EM 레시피의 슬롯 좌표
        private static readonly Vector3[] Slots =
        {
            new Vector3(0f, 0f, 0.082f), new Vector3(0f, 0f, 0.0273f),
            new Vector3(0f, 0f, -0.0273f), new Vector3(0f, 0f, -0.082f),
        };

        private InspectionCameraSettingsSO _settings;
        private RecipeSO _recipe;
        private Texture2D _full;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<InspectionCameraSettingsSO>();
            _recipe = InspectionTestFactory.Recipe("EM", Slots, "gauze", "bandage", "antiseptic", "gloves");
            _full = new Texture2D(_settings.ImageWidth, _settings.ImageHeight, TextureFormat.RGBA32, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_full);
        }

        private RectInt ExpectedRect(int slotNumber)
        {
            Vector3 position = Slots[slotNumber - 1];
            var center = new Vector3(position.x, _settings.CropPlaneLocalY, position.z);
            Assert.IsTrue(_settings.CreateModel().TryGetCropRect(center, _settings.CropSizeMeters, out RectInt rect));
            return rect;
        }

        [Test]
        public void Crops_AreInSlotOrder_WithRectsFromTheCameraModel()
        {
            Assert.IsTrue(SlotCropper.TryCrop(_full, _recipe, _settings, out SlotCropSet crops));

            using (crops)
            {
                Assert.AreEqual(4, crops.Crops.Count);
                for (int i = 0; i < 4; i++)
                {
                    Assert.AreEqual(i + 1, crops.Crops[i].SlotNumber);
                    Assert.AreEqual(ExpectedRect(i + 1), crops.Crops[i].Rect);
                    Assert.AreEqual(crops.Crops[i].Rect.width, crops.Crops[i].Image.width);
                    Assert.AreEqual(crops.Crops[i].Rect.height, crops.Crops[i].Image.height);
                }
            }
        }

        [Test]
        public void CropPixels_ComeFromTheirOwnSlotRegion()
        {
            Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow };
            for (int slot = 1; slot <= 4; slot++)
            {
                RectInt rect = ExpectedRect(slot);
                var fill = new Color[rect.width * rect.height];
                for (int i = 0; i < fill.Length; i++)
                {
                    fill[i] = colors[slot - 1];
                }

                _full.SetPixels(rect.x, rect.y, rect.width, rect.height, fill);
            }

            _full.Apply();

            Assert.IsTrue(SlotCropper.TryCrop(_full, _recipe, _settings, out SlotCropSet crops));

            using (crops)
            {
                for (int i = 0; i < 4; i++)
                {
                    Texture2D image = crops.Crops[i].Image;
                    Assert.AreEqual(colors[i], image.GetPixel(image.width / 2, image.height / 2));
                }
            }
        }

        [Test]
        public void ImageSizeDifferentFromSettings_Fails()
        {
            var small = new Texture2D(64, 64, TextureFormat.RGBA32, false);

            Assert.IsFalse(SlotCropper.TryCrop(small, _recipe, _settings, out SlotCropSet crops));
            Assert.IsNull(crops);

            Object.DestroyImmediate(small);
        }

        [Test]
        public void SlotOutsideTheImage_Fails()
        {
            RecipeSO far = InspectionTestFactory.Recipe("EM", new[] { new Vector3(0f, 0f, 0.5f) }, "gauze");

            Assert.IsFalse(SlotCropper.TryCrop(_full, far, _settings, out SlotCropSet crops));
            Assert.IsNull(crops);

            Object.DestroyImmediate(far);
        }
    }
}
