using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Recipes
{
    /// <summary>
    /// 키트 종류 하나의 레시피. 로봇암 A(선반 위치, 슬롯 좌표), 검사(정답 목록), 데이터 생성기, 박스 공급부가 같은 레시피를 읽는다.
    /// 키트 종류를 추가할 때는 이 에셋을 하나 더 만들고 스크립트는 고치지 않는다. (NFR-07)
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Recipe", fileName = "Recipe_")]
    public sealed class RecipeSO : ScriptableObject
    {
        // 선반 한 행이 열 4개이고 열 번호가 피킹 순서이므로 구성품은 최대 4개다.
        private const int ShelfColumnCount = 4;

        [Tooltip("키트 종류 코드(EM, BL, DX). 대문자로 쓰고 레시피끼리 겹치면 안 된다.")]
        [SerializeField] private string _kitCode;
        [SerializeField] private BoxSize _boxSize;

        [Tooltip("피킹 순서대로 나열한다. 항목 1개가 슬롯 1칸이다.")]
        [SerializeField] private List<RecipeItem> _items = new List<RecipeItem>();

        public string KitCode => _kitCode;

        public BoxSize BoxSize => _boxSize;

        public IReadOnlyList<RecipeItem> Items => _items;

        /// <summary>슬롯 번호로 항목을 찾는다. 슬롯 센서가 보고한 번호로 구성품과 좌표를 얻을 때 쓴다.</summary>
        public bool TryGetItemBySlot(int slotNumber, out RecipeItem item)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].SlotNumber == slotNumber)
                {
                    item = _items[i];
                    return true;
                }
            }

            item = default;
            return false;
        }

        /// <summary>구성품 ID로 항목을 찾는다. 선반 칸과 슬롯 번호를 구성품 기준으로 얻을 때 쓴다.</summary>
        public bool TryGetItemByComponent(string componentId, out RecipeItem item)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ComponentId == componentId)
                {
                    item = _items[i];
                    return true;
                }
            }

            item = default;
            return false;
        }

        // 레시피는 손으로 편집하므로, 규칙이 어긋나면 편집 즉시 알려 런타임 오류로 번지지 않게 한다.
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_kitCode) || _kitCode != _kitCode.Trim().ToUpperInvariant())
            {
                Debug.LogWarning($"{name}: 키트 코드는 비어 있지 않은 대문자여야 한다. (현재 '{_kitCode}')", this);
            }

            if (_items.Count == 0 || _items.Count > ShelfColumnCount)
            {
                Debug.LogWarning($"{name}: 구성품은 1~{ShelfColumnCount}개여야 한다. (현재 {_items.Count}개)", this);
            }

            var componentIds = new HashSet<string>();
            var shelfSlotIds = new HashSet<string>();
            var slotNumbers = new HashSet<int>();
            char shelfRow = '\0';

            for (int i = 0; i < _items.Count; i++)
            {
                RecipeItem item = _items[i];
                int order = i + 1;

                if (string.IsNullOrEmpty(item.ComponentId) || !componentIds.Add(item.ComponentId))
                {
                    Debug.LogWarning($"{name}: {order}번째 구성품 ID가 비었거나 겹친다. 구성품은 종류마다 1개씩만 쓴다.", this);
                }

                if (!shelfSlotIds.Add(item.ShelfSlotId ?? string.Empty))
                {
                    Debug.LogWarning($"{name}: {order}번째 선반 칸 ID '{item.ShelfSlotId}'가 겹친다.", this);
                }

                // 선반 칸 ID는 '<행>-<열>'이고, 열 번호는 피킹 순서와 같으며 행은 레시피 안에서 같다.
                string shelfSlotId = item.ShelfSlotId;
                bool isFormatValid = !string.IsNullOrEmpty(shelfSlotId)
                    && shelfSlotId.Length == 3
                    && char.IsUpper(shelfSlotId[0])
                    && shelfSlotId[1] == '-'
                    && shelfSlotId[2] == (char)('0' + order);
                if (!isFormatValid)
                {
                    Debug.LogWarning($"{name}: {order}번째 선반 칸 ID '{shelfSlotId}'는 '<행>-{order}' 형식이어야 한다.", this);
                }
                else if (shelfRow == '\0')
                {
                    shelfRow = shelfSlotId[0];
                }
                else if (shelfSlotId[0] != shelfRow)
                {
                    Debug.LogWarning($"{name}: {order}번째 선반 칸 '{shelfSlotId}'의 행이 다른 항목('{shelfRow}')과 다르다.", this);
                }

                if (item.SlotNumber < 1 || item.SlotNumber > _items.Count || !slotNumbers.Add(item.SlotNumber))
                {
                    Debug.LogWarning($"{name}: {order}번째 슬롯 번호 {item.SlotNumber}가 1~{_items.Count} 범위를 벗어나거나 겹친다.", this);
                }
            }
        }
    }
}
