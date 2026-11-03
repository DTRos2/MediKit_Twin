using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class SlotTensorizerTests
    {
        [Test]
        public void SolidColor_FillsEachChannelPlane()
        {
            var image = new Texture2D(155, 87, TextureFormat.RGBA32, false);
            var pixels = new Color32[155 * 87];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 0, 51, 255);
            }

            image.SetPixels32(pixels);
            var tensor = new float[SlotTensorizer.FloatsPerImage];

            SlotTensorizer.Write(image, tensor, 0);

            int plane = SlotTensorizer.Width * SlotTensorizer.Height;
            Assert.AreEqual(1f, tensor[0], 1e-4f);
            Assert.AreEqual(1f, tensor[plane - 1], 1e-4f);
            Assert.AreEqual(0f, tensor[plane], 1e-4f);
            Assert.AreEqual(0.2f, tensor[2 * plane + 100], 1e-4f);
            Object.DestroyImmediate(image);
        }

        [Test]
        public void TopRowOfTexture_GoesToFirstTensorRow()
        {
            // Unity 텍스처는 왼쪽 아래 원점이고 모델 입력은 위가 0번 행이다. 위쪽 절반만 빨강으로 칠한다.
            var image = new Texture2D(155, 87, TextureFormat.RGBA32, false);
            var pixels = new Color32[155 * 87];
            for (int y = 0; y < 87; y++)
            {
                for (int x = 0; x < 155; x++)
                {
                    pixels[y * 155 + x] = y >= 44 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 0, 255);
                }
            }

            image.SetPixels32(pixels);
            var tensor = new float[SlotTensorizer.FloatsPerImage];

            SlotTensorizer.Write(image, tensor, 0);

            Assert.AreEqual(1f, tensor[0], 1e-3f, "첫 행의 빨강");
            Assert.AreEqual(0f, tensor[(SlotTensorizer.Height - 1) * SlotTensorizer.Width], 1e-3f, "마지막 행의 빨강");
            Object.DestroyImmediate(image);
        }

        [Test]
        public void Offset_WritesAfterPreviousImage()
        {
            var image = new Texture2D(155, 87, TextureFormat.RGBA32, false);
            var tensor = new float[SlotTensorizer.FloatsPerImage * 2];

            SlotTensorizer.Write(image, tensor, SlotTensorizer.FloatsPerImage);

            Assert.AreEqual(0f, tensor[0]);
            Object.DestroyImmediate(image);
        }
    }
}
