using System;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 확률과 고정 키트 순번으로 누락을 정한다. (FR-04) 난수는 시드를 받는 System.Random으로 만들고,
    /// 키트 순번마다 따로 만들어 같은 시드면 몇 번을 물어도, 어떤 순서로 물어도 같은 키트에서 같은 누락이 나온다. (NFR-06)
    /// 재고 소진으로 다시 만드는 키트는 새 순번을 받아 새로 추첨한다.
    /// </summary>
    public sealed class RandomMissingRule : IMissingRule
    {
        private readonly MissingRuleSettings _settings;

        public RandomMissingRule(MissingRuleSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public int? SelectMissingSlot(KitData kit, RecipeSO recipe)
        {
            if (recipe.Items.Count == 0)
            {
                return null;
            }

            var random = new Random(MixSeed(_settings.Seed, kit.KitSequence));

            // 고정 키트여도 확률 추첨을 한 번 뽑아, 설정을 바꿔도 뒤따르는 난수열이 같게 한다.
            double percent = Math.Max(0f, Math.Min(100f, _settings.ProbabilityPercent));
            bool isChosenByChance = random.NextDouble() * 100.0 < percent;
            bool isFixedKit = _settings.FixedKitSequence > 0 && kit.KitSequence == _settings.FixedKitSequence;

            if (!isFixedKit && !isChosenByChance)
            {
                return null;
            }

            if (isFixedKit && _settings.FixedSlotNumber > 0 && recipe.TryGetItemBySlot(_settings.FixedSlotNumber, out _))
            {
                return _settings.FixedSlotNumber;
            }

            return recipe.Items[random.Next(recipe.Items.Count)].SlotNumber;
        }

        // string.GetHashCode나 HashCode.Combine은 실행마다 값이 달라지므로 정수 연산으로 직접 섞는다.
        private static int MixSeed(int seed, int kitSequence)
        {
            unchecked
            {
                return (seed * 486187739) ^ (kitSequence * 1664525 + 1013904223);
            }
        }
    }
}
