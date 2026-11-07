using System.IO;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// uGUI 위젯이 쓰는 그림 중 Unity 기본 그림에 없는 것(가운데가 빈 링)을 코드로 그려 에셋으로 저장한다. 이미 있으면 그대로 쓴다.
    /// 링의 굵기 비율은 IMGUI 조준 원과 같은 치수(DashboardMetrics.CameraCrosshair*)에서 구하므로 두 화면의 조준 원이 닮았다.
    /// </summary>
    public static class UiSprites
    {
        // 그림 한 변의 픽셀 수. 작은 링을 확대해도 가장자리가 거칠지 않게 넉넉하게 잡는다.
        private const int RingTextureSize = 128;

        /// <summary>가운데가 빈 흰색 링 그림. 색은 이미지의 색으로 입힌다.</summary>
        public static Sprite Ring()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectAssetPaths.RingSprite);
            if (existing != null)
            {
                return existing;
            }

            WriteRingTexture(ProjectAssetPaths.RingSprite);
            ImportAsSprite(ProjectAssetPaths.RingSprite);
            return AssetDatabase.LoadAssetAtPath<Sprite>(ProjectAssetPaths.RingSprite);
        }

        private static void WriteRingTexture(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            float outer = RingTextureSize * 0.5f;
            float inner = outer * (1f - DashboardMetrics.CameraCrosshairWidth / DashboardMetrics.CameraCrosshairRadius);
            var texture = new Texture2D(RingTextureSize, RingTextureSize, TextureFormat.RGBA32, false);
            var pixels = new Color[RingTextureSize * RingTextureSize];
            for (int y = 0; y < RingTextureSize; y++)
            {
                for (int x = 0; x < RingTextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(outer, outer));

                    // 바깥과 안쪽 가장자리를 한 픽셀 폭으로 부드럽게 해서 계단이 보이지 않게 한다.
                    float alpha = Mathf.Clamp01(outer - distance) * Mathf.Clamp01(distance - inner);
                    pixels[y * RingTextureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath);
        }

        private static void ImportAsSprite(string assetPath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
