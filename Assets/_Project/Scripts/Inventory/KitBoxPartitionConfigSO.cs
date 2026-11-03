using System.Collections.Generic;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 키트 박스 칸막이의 두께, 높이, 물건과의 틈을 담은 설정 에셋. 칸막이 자리는 레시피의 슬롯 좌표에서 읽으므로
    /// 슬롯이 바뀌면 칸막이도 따라 바뀐다. 에디터 도구(칸막이 만들기, 선반 물건 줄이기)와 물건 크기 상한이 모두 이 값을 읽는다.
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Kit Box Partition Config", fileName = "KitBoxPartitionConfig")]
    public sealed class KitBoxPartitionConfigSO : ScriptableObject
    {
        [Header("칸막이")]
        [Tooltip("칸막이 두께(m). 얇을수록 칸이 넓지만 물리 충돌에서 물건이 뚫고 지나갈 수 있어 3mm 아래로는 두지 않는다.")]
        [SerializeField, Min(0.001f)] private float _thicknessM = 0.003f;
        [Tooltip("칸막이 높이(m, 박스 바닥 윗면 기준). 박스 벽 높이를 넘으면 벽 높이에 맞춘다. 뚜껑 덮기가 박스 윗면을 벽 높이로 재므로 벽보다 낮아야 한다.")]
        [SerializeField, Min(0.005f)] private float _heightM = 0.04f;
        [Tooltip("물건과 칸막이 사이에 한쪽마다 남길 틈(m). 이 틈이 있어야 물건이 칸막이에 끼지 않고 내려앉는다.")]
        [SerializeField, Min(0f)] private float _itemGapM = 0.002f;

        [Header("슬롯 좌표를 읽을 레시피")]
        [Tooltip("각 레시피의 슬롯 좌표(SlotLocalPosition)를 합쳐서 칸막이 자리를 정한다. 세 레시피가 같은 슬롯 자리를 쓴다.")]
        [SerializeField] private RecipeSO[] _recipes = new RecipeSO[0];

        public float ThicknessM => _thicknessM;

        public float HeightM => _heightM;

        public float ItemGapM => _itemGapM;

        public IReadOnlyList<RecipeSO> Recipes => _recipes;

        /// <summary>에디터 도구가 레시피 목록을 채울 때 쓴다.</summary>
        public void SetRecipes(RecipeSO[] recipes)
        {
            _recipes = recipes ?? new RecipeSO[0];
        }

        /// <summary>레시피의 슬롯 좌표(박스 긴 변인 z 방향)로 칸막이 배치를 계산한다.</summary>
        public KitBoxPartitionLayout CreateLayout()
        {
            var slotCenters = new List<float>();
            foreach (RecipeSO recipe in _recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                foreach (RecipeItem item in recipe.Items)
                {
                    slotCenters.Add(item.SlotLocalPosition.z);
                }
            }

            return new KitBoxPartitionLayout(slotCenters, _thicknessM, _itemGapM);
        }
    }
}
