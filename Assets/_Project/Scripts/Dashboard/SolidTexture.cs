using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 한 가지 색으로 채운 1×1 그림을 색마다 한 번만 만들어 돌려준다. IMGUI의 스크롤 막대처럼 그림 배경이 필요한 스타일에 쓴다.
    /// 만든 그림은 씬에 저장되지 않고 도메인이 다시 로드될 때 함께 사라진다.
    /// </summary>
    public static class SolidTexture
    {
        private static readonly Dictionary<Color32, Texture2D> Cache = new Dictionary<Color32, Texture2D>();

        public static Texture2D Of(Color color)
        {
            Color32 key = color;
            if (Cache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            Cache[key] = texture;
            return texture;
        }
    }
}
