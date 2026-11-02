using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 키트 레시피의 내용(어떤 구성품을 어떤 순서로 집어 어느 슬롯에 놓을지)을 난수로 정한다.
    /// 랜덤 주문은 "무슨 키트를 몇 개 주문하는가"를 흔들고, 이것은 "키트 안에 무엇이 들어가는가"를 흔든다.
    /// 구성품 구성이 바뀌면 같은 선반 칸에서 집어도 앞뒤 순서와 놓는 슬롯이 달라져 팔의 자세와 이웃 물건과의 간섭이 달라진다.
    /// 강화학습이 아니라 입력을 흔들어 실패하는 조합을 찾는 시험이므로, 같은 난수면 항상 같은 레시피가 나와야 한다.
    /// 칸 안의 물건은 씬에 놓여 있어 바꿀 수 없으므로, 주입받은 (칸, 구성품) 짝 안에서만 고른다.
    /// 씬과 물리를 몰라서 씬 없이 시험한다.
    /// </summary>
    public sealed class RandomRecipeGenerator
    {
        private readonly ShelfCellContent[] _cells;
        private readonly int _minItems;
        private readonly int _maxItems;

        /// <param name="cells">고를 수 있는 (칸, 구성품) 짝. 같은 칸이 겹치면 처음 것만 쓰고, 칸 ID 순으로 늘어놓는다.</param>
        /// <param name="minItems">레시피 하나의 최소 구성품 수. 쓸 수 있는 구성품 종류가 이보다 적으면 있는 만큼만 쓴다.</param>
        /// <param name="maxItems">레시피 하나의 최대 구성품 수. 박스 슬롯 수를 넘기지 않는다.</param>
        public RandomRecipeGenerator(IEnumerable<ShelfCellContent> cells, int minItems, int maxItems)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (minItems < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minItems), "구성품은 한 개 이상이어야 한다.");
            }

            if (maxItems < minItems)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "최대 구성품 수는 최소보다 작을 수 없다.");
            }

            // 집합에서 꺼내는 순서는 실행마다 다를 수 있으므로, 같은 난수가 같은 레시피를 내도록 순서를 고정한다.
            _cells = cells
                .Where(cell => !string.IsNullOrEmpty(cell.ShelfSlotId) && !string.IsNullOrEmpty(cell.ComponentId))
                .GroupBy(cell => cell.ShelfSlotId, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(cell => cell.ShelfSlotId, StringComparer.Ordinal)
                .ToArray();

            if (_cells.Length == 0)
            {
                throw new ArgumentException("고를 수 있는 선반 칸이 하나도 없다.", nameof(cells));
            }

            _minItems = minItems;
            _maxItems = maxItems;
        }

        /// <param name="random">같은 상태의 난수면 같은 레시피가 나온다. 여러 레시피를 이어서 만들 때 같은 난수를 계속 넘긴다.</param>
        /// <param name="kitCode">레시피의 키트 코드. 그대로 담는다.</param>
        /// <param name="boxSize">상자 크기 값. 구성품과 무관하므로 그대로 담는다.</param>
        /// <param name="slotPositions">박스 슬롯의 로컬 좌표. 슬롯 번호 순서(1번부터)다. 구성품 수가 이보다 적으면 앞의 것만 쓴다.</param>
        public RandomRecipe Generate(System.Random random, string kitCode, int boxSize, IReadOnlyList<Vector3> slotPositions)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (slotPositions == null || slotPositions.Count == 0)
            {
                throw new ArgumentException("박스 슬롯 좌표가 하나도 없다.", nameof(slotPositions));
            }

            ShelfCellContent[] picked = PickCells(random, slotPositions.Count);

            // 레시피는 슬롯 번호를 1부터 구성품 수까지 빈틈없이 쓰고(RecipeSO.OnValidate), 번호 k는 좌표 목록의 k번째와 짝이다.
            // 그래서 구성품이 슬롯보다 적으면 앞의 슬롯만 쓰고, 어느 구성품이 어느 슬롯에 가는지만 섞는다.
            int[] slotNumbers = Enumerable.Range(1, picked.Length).ToArray();
            RandomShuffle.InPlace(random, slotNumbers);

            var items = new RandomRecipeItem[picked.Length];
            for (int i = 0; i < picked.Length; i++)
            {
                items[i] = new RandomRecipeItem(picked[i].ComponentId, picked[i].ShelfSlotId, slotNumbers[i], slotPositions[slotNumbers[i] - 1]);
            }

            return new RandomRecipe(kitCode, boxSize, items);
        }

        // 구성품 수를 정하고, 섞은 칸에서 구성품 종류가 겹치지 않게 앞에서부터 고른다. 고른 순서가 피킹 순서다.
        private ShelfCellContent[] PickCells(System.Random random, int slotCount)
        {
            int wanted = Math.Min(random.Next(_minItems, _maxItems + 1), slotCount);

            ShelfCellContent[] shuffled = (ShelfCellContent[])_cells.Clone();
            RandomShuffle.InPlace(random, shuffled);

            var picked = new List<ShelfCellContent>(wanted);
            var usedComponents = new HashSet<string>(StringComparer.Ordinal);
            foreach (ShelfCellContent cell in shuffled)
            {
                if (picked.Count == wanted)
                {
                    break;
                }

                // 키트 하나는 구성품을 종류마다 1개씩만 쓴다. (RecipeItem)
                if (usedComponents.Add(cell.ComponentId))
                {
                    picked.Add(cell);
                }
            }

            return picked.ToArray();
        }
    }
}
