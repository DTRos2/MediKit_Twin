using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>선반 칸 하나의 시작 재고. 목록에 없는 칸은 ScenarioConfigSO.DefaultStock을 쓴다.</summary>
    [Serializable]
    public struct ShelfStockSetting
    {
        [SerializeField] private string _shelfSlotId;
        [SerializeField, Min(0)] private int _quantity;

        public string ShelfSlotId => _shelfSlotId;

        public int Quantity => _quantity;
    }

    /// <summary>
    /// 시나리오 하나의 실행 설정. 시나리오를 바꿔도 코드는 바뀌지 않고 이 에셋만 바꾼다.
    /// 예: S2는 누락 확률 10%와 고정 누락 3번, S4는 누락 0%와 C-3 재고 2개. (FR-04, FR-13)
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Scenario Config", fileName = "Scenario_")]
    public sealed class ScenarioConfigSO : ScriptableObject
    {
        [Header("누락 규칙 (FR-04)")]
        [Tooltip("키트마다 누락이 발생할 확률(%). 0이면 누락이 없다.")]
        [SerializeField, Range(0f, 100f)] private float _missingProbabilityPercent = 10f;

        [Tooltip("항상 누락시킬 키트 순번. 0이면 사용하지 않는다.")]
        [SerializeField, Min(0)] private int _fixedMissingKitSequence = 3;

        [Tooltip("고정 누락 키트에서 빠뜨릴 슬롯 번호. 0이면 무작위로 고른다. 시연에서 빠지는 슬롯을 미리 알리고 싶을 때 쓴다.")]
        [SerializeField, Min(0)] private int _fixedMissingSlotNumber;

        [Tooltip("난수 시드. 같은 시드와 같은 주문 파일은 같은 결과를 낸다. (NFR-06)")]
        [SerializeField] private int _randomSeed = 1234;

        [Header("재고 (FR-13)")]
        [Tooltip("목록에 없는 선반 칸의 시작 재고.")]
        [SerializeField, Min(0)] private int _defaultStock = 20;

        [Tooltip("시작 재고를 따로 정할 선반 칸. 예: C-3을 2개로 두면 DX 세 번째 키트에서 소진된다.")]
        [SerializeField] private List<ShelfStockSetting> _stockOverrides = new List<ShelfStockSetting>();

        [Tooltip("재고가 0이 된 뒤 보충까지 기다리는 시뮬레이션 시간(초).")]
        [SerializeField, Min(0f)] private float _refillDelaySeconds = 30f;

        [Tooltip("보충할 때 채우는 수량.")]
        [SerializeField, Min(1)] private int _refillQuantity = 20;

        public float MissingProbabilityPercent => _missingProbabilityPercent;

        public int FixedMissingKitSequence => _fixedMissingKitSequence;

        public int FixedMissingSlotNumber => _fixedMissingSlotNumber;

        public int RandomSeed => _randomSeed;

        public int DefaultStock => _defaultStock;

        public IReadOnlyList<ShelfStockSetting> StockOverrides => _stockOverrides;

        public float RefillDelaySeconds => _refillDelaySeconds;

        public int RefillQuantity => _refillQuantity;

        /// <summary>선반 칸의 시작 재고. 목록에 있으면 그 값, 없으면 기본 재고이다.</summary>
        public int GetInitialStock(string shelfSlotId)
        {
            for (int i = 0; i < _stockOverrides.Count; i++)
            {
                if (_stockOverrides[i].ShelfSlotId == shelfSlotId)
                {
                    return _stockOverrides[i].Quantity;
                }
            }

            return _defaultStock;
        }

        private void OnValidate()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _stockOverrides.Count; i++)
            {
                if (!seen.Add(_stockOverrides[i].ShelfSlotId ?? string.Empty))
                {
                    Debug.LogWarning($"{name}: 재고 설정의 선반 칸 '{_stockOverrides[i].ShelfSlotId}'가 겹친다. 먼저 나온 값만 쓴다.", this);
                }
            }
        }
    }
}
