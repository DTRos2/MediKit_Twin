using System;
using System.Collections.Generic;

namespace MediKitTwin.ML
{
    /// <summary>
    /// 합성 박스 하나의 이산적인 설계: 어떤 키트에서 어떤 내용(정상, 누락, 오구성)으로 만들지.
    /// 위치 오차, 박스 회전, 조명, 색조 같은 연속 값은 Seed로 촬영할 때 정한다.
    /// </summary>
    public sealed class SyntheticBoxCase
    {
        private readonly string[] _expectedComponentIds;
        private readonly string[] _actualComponentIds;

        public SyntheticBoxCase(
            int index,
            DataSplit split,
            string kitCode,
            BoxVariant variant,
            int affectedSlotNumber,
            IReadOnlyList<string> expectedComponentIds,
            IReadOnlyList<string> actualComponentIds,
            int seed)
        {
            if (expectedComponentIds.Count != actualComponentIds.Count)
            {
                throw new ArgumentException("레시피와 실제 내용의 슬롯 수가 다르다.");
            }

            Index = index;
            Split = split;
            KitCode = kitCode;
            Variant = variant;
            AffectedSlotNumber = affectedSlotNumber;
            Seed = seed;
            _expectedComponentIds = Copy(expectedComponentIds);
            _actualComponentIds = Copy(actualComponentIds);
        }

        public int Index { get; }

        public DataSplit Split { get; }

        public string KitCode { get; }

        public BoxVariant Variant { get; }

        /// <summary>누락이나 오구성이 생긴 슬롯 번호(1부터). 정상이면 0이다.</summary>
        public int AffectedSlotNumber { get; }

        /// <summary>촬영할 때 연속 값(위치 오차, 회전, 조명, 색조)을 정하는 난수 시드.</summary>
        public int Seed { get; }

        public int SlotCount => _actualComponentIds.Length;

        public bool IsDefect => Variant != BoxVariant.Normal;

        /// <summary>레시피가 요구하는 구성품 ID. 인덱스는 슬롯 번호 - 1이다.</summary>
        public string GetExpectedComponentId(int slotNumber) => _expectedComponentIds[slotNumber - 1];

        /// <summary>슬롯에 실제로 놓을 구성품 ID. 빈 슬롯은 SlotCropRecord.EmptyLabel이다.</summary>
        public string GetActualComponentId(int slotNumber) => _actualComponentIds[slotNumber - 1];

        private static string[] Copy(IReadOnlyList<string> source)
        {
            var copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }
}
