using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class TopDownCameraModelTests
    {
        // 레시피의 슬롯 좌표(박스 로컬): 긴 변(Z)을 따라 4칸
        private static readonly Vector3[] Slots =
        {
            new Vector3(0f, 0f, 0.082f), new Vector3(0f, 0f, 0.0273f),
            new Vector3(0f, 0f, -0.0273f), new Vector3(0f, 0f, -0.082f),
        };

        private static TopDownCameraModel Model() => new TopDownCameraModel(0.6f, 30f, 256, 512);

        [Test]
        public void BoxOrigin_ProjectsToImageCenter()
        {
            Assert.IsTrue(Model().TryProject(Vector3.zero, out Vector2 pixel));

            Assert.AreEqual(128f, pixel.x, 0.001f);
            Assert.AreEqual(256f, pixel.y, 0.001f);
        }

        [Test]
        public void PlusX_GoesRight_PlusZ_GoesUp()
        {
            TopDownCameraModel model = Model();
            model.TryProject(new Vector3(0.05f, 0f, 0f), out Vector2 right);
            model.TryProject(new Vector3(0f, 0f, 0.05f), out Vector2 up);

            Assert.Greater(right.x, 128f);
            Assert.AreEqual(256f, right.y, 0.001f);
            Assert.Greater(up.y, 256f);
            Assert.AreEqual(128f, up.x, 0.001f);
        }

        [Test]
        public void HigherPoint_AppearsLargerBecauseItIsCloser()
        {
            TopDownCameraModel model = Model();

            Assert.Greater(model.PixelsPerMeterAt(0.1f), model.PixelsPerMeterAt(0f));
        }

        [Test]
        public void PointAtOrAboveCamera_CannotBeProjected()
        {
            Assert.IsFalse(Model().TryProject(new Vector3(0f, 0.6f, 0f), out _));
            Assert.IsFalse(Model().TryProject(new Vector3(0f, 0.7f, 0f), out _));
        }

        [Test]
        public void SlotCrops_AreInsideImage_AndSameSize()
        {
            TopDownCameraModel model = Model();
            var size = new Vector2(0.0976f, 0.0546f);

            foreach (Vector3 slot in Slots)
            {
                Assert.IsTrue(model.TryGetCropRect(slot, size, out RectInt rect), slot.ToString());
                Assert.GreaterOrEqual(rect.xMin, 0);
                Assert.GreaterOrEqual(rect.yMin, 0);
                Assert.LessOrEqual(rect.xMax, 256);
                Assert.LessOrEqual(rect.yMax, 512);
                Assert.AreEqual(Mathf.RoundToInt(size.x * model.PixelsPerMeterAt(0f)), rect.width);
                Assert.AreEqual(Mathf.RoundToInt(size.y * model.PixelsPerMeterAt(0f)), rect.height);
            }
        }

        [Test]
        public void SlotCrops_FollowSlotOrder_AndDoNotOverlap()
        {
            TopDownCameraModel model = Model();
            var size = new Vector2(0.0976f, 0.0546f);
            var rects = new RectInt[Slots.Length];
            for (int i = 0; i < Slots.Length; i++)
            {
                Assert.IsTrue(model.TryGetCropRect(Slots[i], size, out rects[i]));
            }

            // 슬롯 번호가 커질수록 Z가 작아지므로 이미지에서는 아래로 내려간다.
            for (int i = 0; i < rects.Length - 1; i++)
            {
                Assert.Greater(rects[i].yMin, rects[i + 1].yMin);
                Assert.LessOrEqual(rects[i + 1].yMax, rects[i].yMin + 1, "이웃 슬롯 크롭이 겹친다.");
            }
        }

        [Test]
        public void SymmetricSlots_HaveMirroredCrops()
        {
            TopDownCameraModel model = Model();
            var size = new Vector2(0.0976f, 0.0546f);
            model.TryGetCropRect(Slots[0], size, out RectInt top);
            model.TryGetCropRect(Slots[3], size, out RectInt bottom);

            Assert.AreEqual(512, top.yMax + bottom.yMin, 1);
        }

        [Test]
        public void CropOutsideImage_ReturnsFalse()
        {
            Assert.IsFalse(Model().TryGetCropRect(new Vector3(0f, 0f, 0.3f), new Vector2(0.0976f, 0.0546f), out _));
            Assert.IsFalse(Model().TryGetCropRect(new Vector3(0.2f, 0f, 0f), new Vector2(0.0976f, 0.0546f), out _));
        }

        [Test]
        public void InvalidSettings_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TopDownCameraModel(0f, 30f, 256, 512));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TopDownCameraModel(0.6f, 0f, 256, 512));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TopDownCameraModel(0.6f, 30f, 0, 512));
        }
    }
}
