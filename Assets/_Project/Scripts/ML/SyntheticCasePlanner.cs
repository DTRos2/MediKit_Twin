using System;
using System.Collections.Generic;

namespace MediKitTwin.ML
{
    /// <summary>
    /// 합성 데이터셋의 박스 목록을 정한다. 같은 입력과 시드는 같은 목록을 낸다. (NFR-06)
    /// 정상과 불량(누락, 오구성)의 수를 맞추고, 키트 종류를 고르게 섞는다.
    /// 테스트 세트는 정상과 불량의 수가 정확히 같아야 하므로 개수가 짝수여야 한다.
    /// </summary>
    public static class SyntheticCasePlanner
    {
        public static List<SyntheticBoxCase> Plan(
            IReadOnlyList<SyntheticKitSpec> kits,
            DataSplit split,
            int count,
            int seed,
            float defectRatio = 0.5f)
        {
            if (kits == null || kits.Count == 0)
            {
                throw new ArgumentException("키트 레시피가 하나도 없다.", nameof(kits));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (split == DataSplit.Test)
            {
                if (count % 2 != 0)
                {
                    throw new ArgumentException("테스트 세트의 개수는 정상과 불량을 같게 하려고 짝수여야 한다.", nameof(count));
                }

                defectRatio = 0.5f;
            }
            else if (defectRatio < 0f || defectRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(defectRatio));
            }

            var random = new Random(seed);
            List<string> componentPool = CollectComponentPool(kits);

            int defectCount = (int)Math.Round(count * (double)defectRatio);
            int missingCount = defectCount / 2;
            int wrongItemCount = defectCount - missingCount;
            int normalCount = count - defectCount;

            // 종류별로 키트를 번갈아 배정한 뒤 섞는다. 종류 안에서 키트가 고르게 나온다.
            var variants = new List<(BoxVariant variant, int kitIndex)>(count);
            AddVariant(variants, BoxVariant.Normal, normalCount, kits.Count);
            AddVariant(variants, BoxVariant.Missing, missingCount, kits.Count);
            AddVariant(variants, BoxVariant.WrongItem, wrongItemCount, kits.Count);
            Shuffle(variants, random);

            var cases = new List<SyntheticBoxCase>(count);
            for (int i = 0; i < variants.Count; i++)
            {
                cases.Add(CreateCase(i, split, kits[variants[i].kitIndex], variants[i].variant, componentPool, random));
            }

            return cases;
        }

        private static void AddVariant(List<(BoxVariant variant, int kitIndex)> list, BoxVariant variant, int count, int kitCount)
        {
            for (int i = 0; i < count; i++)
            {
                list.Add((variant, i % kitCount));
            }
        }

        private static SyntheticBoxCase CreateCase(
            int index,
            DataSplit split,
            SyntheticKitSpec kit,
            BoxVariant variant,
            IReadOnlyList<string> componentPool,
            Random random)
        {
            string[] expected = kit.CopyComponentIds();
            string[] actual = kit.CopyComponentIds();
            int affectedSlotNumber = 0;

            if (variant != BoxVariant.Normal)
            {
                int slotIndex = random.Next(kit.SlotCount);
                affectedSlotNumber = slotIndex + 1;

                actual[slotIndex] = variant == BoxVariant.Missing
                    ? SlotCropRecord.EmptyLabel
                    : PickWrongComponent(expected, componentPool, random);
            }

            return new SyntheticBoxCase(index, split, kit.KitCode, variant, affectedSlotNumber, expected, actual, random.Next());
        }

        /// <summary>
        /// 레시피에 없는 구성품 중에서 고른다. 같은 박스에 이미 있는 구성품과 겹치면
        /// "다른 슬롯의 구성품이 옮겨진" 경우와 섞이므로 고르지 않는다.
        /// </summary>
        private static string PickWrongComponent(string[] expected, IReadOnlyList<string> componentPool, Random random)
        {
            var candidates = new List<string>();
            for (int i = 0; i < componentPool.Count; i++)
            {
                if (Array.IndexOf(expected, componentPool[i]) < 0)
                {
                    candidates.Add(componentPool[i]);
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("오구성으로 바꿀 다른 구성품이 없다. 구성품이 서로 다른 키트 레시피가 2종 이상 필요하다.");
            }

            return candidates[random.Next(candidates.Count)];
        }

        private static List<string> CollectComponentPool(IReadOnlyList<SyntheticKitSpec> kits)
        {
            var pool = new List<string>();
            var seen = new HashSet<string>();
            for (int k = 0; k < kits.Count; k++)
            {
                foreach (string id in kits[k].CopyComponentIds())
                {
                    if (seen.Add(id))
                    {
                        pool.Add(id);
                    }
                }
            }

            return pool;
        }

        private static void Shuffle<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
