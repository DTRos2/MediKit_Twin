using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>물건 오브젝트의 크기를 재는 도구. 렌더러 경계를 합쳐서 잰다(충돌체는 없을 수도 있어서).</summary>
    public static class ItemExtent
    {
        /// <summary>물건 높이(세계 Y 방향)의 절반(m). 오브젝트가 없거나 렌더러가 없어 잴 수 없으면 0이다.</summary>
        public static float HalfHeightOf(GameObject item)
        {
            if (item == null)
            {
                return 0f;
            }

            // 숨겨 둔 원본(비활성)도 재야 하므로 비활성 자식까지 포함한다.
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return 0f;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.extents.y;
        }
    }
}
