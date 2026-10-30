using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 구성품 종류별 자리표시 모양을 담은 설정 에셋. 모양과 크기와 색을 코드에 쓰지 않고 이 에셋에서 고친다.
    /// 구성품을 추가할 때는 레시피(RecipeSO) 하나와 이 목록에 한 줄을 더하면 되고 에디터 코드는 고치지 않는다. (NFR-07)
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Shelf Item Looks", fileName = "ShelfItemLooks")]
    public sealed class ShelfItemLooksSO : ScriptableObject
    {
        [SerializeField] private List<ShelfItemLook> _looks = new List<ShelfItemLook>();

        [Header("목록에 없는 구성품")]
        [Tooltip("목록에 없는 구성품에 쓰는 모양.")]
        [SerializeField] private ShelfItemShape _fallbackShape = ShelfItemShape.Cube;
        [SerializeField] private Vector3 _fallbackSize = new Vector3(0.10f, 0.10f, 0.10f);
        [SerializeField] private Color _fallbackColor = new Color(0.70f, 0.72f, 0.76f);

        public IReadOnlyList<ShelfItemLook> Looks => _looks;

        /// <summary>구성품의 모양을 돌려준다. 목록에 없으면 기본 모양을 돌려주고 isFallback을 true로 둔다.</summary>
        public ShelfItemLook GetLook(string componentId, out bool isFallback)
        {
            for (int i = 0; i < _looks.Count; i++)
            {
                if (_looks[i].ComponentId == componentId)
                {
                    isFallback = false;
                    return _looks[i];
                }
            }

            isFallback = true;
            return new ShelfItemLook(componentId, _fallbackShape, _fallbackSize, _fallbackColor);
        }

        private void OnValidate()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _looks.Count; i++)
            {
                if (!seen.Add(_looks[i].ComponentId ?? string.Empty))
                {
                    Debug.LogWarning($"{name}: 구성품 '{_looks[i].ComponentId}'의 모양이 겹친다. 먼저 나온 값만 쓴다.", this);
                }
            }
        }
    }
}
