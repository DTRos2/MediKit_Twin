using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    public sealed class RandomRecipeGeneratorTests
    {
        private const int MinItems = 2;
        private const int MaxItems = 4;

        // 실제 선반처럼 3행 4열이고 칸마다 구성품이 다르다.
        private static readonly ShelfCellContent[] Cells =
        {
            new ShelfCellContent("A-1", "gauze"), new ShelfCellContent("A-2", "bandage"),
            new ShelfCellContent("A-3", "antiseptic"), new ShelfCellContent("A-4", "gloves"),
            new ShelfCellContent("B-1", "blood_tube"), new ShelfCellContent("B-2", "needle"),
            new ShelfCellContent("B-3", "alcohol_pad"), new ShelfCellContent("B-4", "tourniquet"),
            new ShelfCellContent("C-1", "swab"), new ShelfCellContent("C-2", "buffer_vial"),
            new ShelfCellContent("C-3", "test_cassette"), new ShelfCellContent("C-4", "sample_bag"),
        };

        private static readonly Vector3[] SlotPositions =
        {
            new Vector3(0f, 0f, 0.082f), new Vector3(0f, 0f, 0.0273f),
            new Vector3(0f, 0f, -0.0273f), new Vector3(0f, 0f, -0.082f),
        };

        private static RandomRecipeGenerator Create(IEnumerable<ShelfCellContent> cells = null, int min = MinItems, int max = MaxItems)
        {
            return new RandomRecipeGenerator(cells ?? Cells, min, max);
        }

        private static RandomRecipe Make(int seed, RandomRecipeGenerator generator = null)
        {
            return (generator ?? Create()).Generate(new System.Random(seed), "EM", 0, SlotPositions);
        }

        [Test]
        public void TheSameSeedMakesTheSameRecipe()
        {
            Assert.That(Make(7).Describe(), Is.EqualTo(Make(7).Describe()));
        }

        [Test]
        public void DifferentSeedsMakeDifferentRecipes()
        {
            int distinct = Enumerable.Range(1, 30).Select(seed => Make(seed).Describe()).Distinct().Count();

            Assert.That(distinct, Is.GreaterThan(10));
        }

        [Test]
        public void TheItemCountStaysWithinTheLimitsAndTheBoxSlots()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                RandomRecipe recipe = Make(seed);

                Assert.That(recipe.Items.Count, Is.InRange(MinItems, MaxItems), $"시드 {seed}");
                Assert.That(recipe.Items.Count, Is.LessThanOrEqualTo(SlotPositions.Length), $"시드 {seed}");
            }
        }

        [Test]
        public void EveryItemIsACellAndComponentPairFromTheShelf()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                foreach (RandomRecipeItem item in Make(seed).Items)
                {
                    Assert.That(Cells.Any(cell => cell.ShelfSlotId == item.ShelfSlotId && cell.ComponentId == item.ComponentId), Is.True,
                        $"시드 {seed}: {item.ShelfSlotId} {item.ComponentId}는 선반에 없는 짝이다");
                }
            }
        }

        [Test]
        public void ComponentsCellsAndSlotNumbersNeverRepeatInOneRecipe()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                IReadOnlyList<RandomRecipeItem> items = Make(seed).Items;

                Assert.That(items.Select(item => item.ComponentId).Distinct().Count(), Is.EqualTo(items.Count), $"시드 {seed}: 구성품이 겹친다");
                Assert.That(items.Select(item => item.ShelfSlotId).Distinct().Count(), Is.EqualTo(items.Count), $"시드 {seed}: 칸이 겹친다");
                Assert.That(items.Select(item => item.SlotNumber).Distinct().Count(), Is.EqualTo(items.Count), $"시드 {seed}: 슬롯이 겹친다");
            }
        }

        [Test]
        public void SlotNumbersRunFromOneWithoutGapsAndEachMatchesItsOwnPosition()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                IReadOnlyList<RandomRecipeItem> items = Make(seed).Items;

                Assert.That(items.Select(item => item.SlotNumber).OrderBy(number => number), Is.EqualTo(Enumerable.Range(1, items.Count)), $"시드 {seed}");
                foreach (RandomRecipeItem item in items)
                {
                    Assert.That(item.SlotLocalPosition, Is.EqualTo(SlotPositions[item.SlotNumber - 1]), $"시드 {seed}: 슬롯 {item.SlotNumber}");
                }
            }
        }

        [Test]
        public void EveryCellAndEverySlotGetsUsedAcrossManySeeds()
        {
            var cells = new HashSet<string>();
            var slotPerComponent = new Dictionary<string, HashSet<int>>();

            for (int seed = 0; seed < 300; seed++)
            {
                foreach (RandomRecipeItem item in Make(seed).Items)
                {
                    cells.Add(item.ShelfSlotId);
                    if (!slotPerComponent.TryGetValue(item.ComponentId, out HashSet<int> slots))
                    {
                        slots = new HashSet<int>();
                        slotPerComponent[item.ComponentId] = slots;
                    }

                    slots.Add(item.SlotNumber);
                }
            }

            Assert.That(cells.Count, Is.EqualTo(Cells.Length), "한 번도 고르지 못한 칸이 있다");
            // 같은 구성품이 여러 슬롯에 가야 슬롯 배정을 섞는 의미가 있다.
            Assert.That(slotPerComponent.Values.All(slots => slots.Count > 1), Is.True, "슬롯이 하나로 고정된 구성품이 있다");
        }

        [Test]
        public void ThePickOrderIsNotAlwaysTheCellOrder()
        {
            int shuffled = Enumerable.Range(0, 100)
                .Select(seed => Make(seed).Items.Select(item => item.ShelfSlotId).ToArray())
                .Count(cellIds => !cellIds.SequenceEqual(cellIds.OrderBy(id => id, StringComparer.Ordinal)));

            Assert.That(shuffled, Is.GreaterThan(0));
        }

        [Test]
        public void FewerComponentKindsThanTheMinimumUsesWhatExists()
        {
            // 칸은 4개지만 구성품은 두 종류뿐이라 같은 구성품의 칸은 하나만 쓴다.
            var cells = new[]
            {
                new ShelfCellContent("A-1", "gauze"), new ShelfCellContent("A-2", "gauze"),
                new ShelfCellContent("B-1", "needle"), new ShelfCellContent("B-2", "needle"),
            };

            RandomRecipe recipe = Make(3, Create(cells, 3, 4));

            Assert.That(recipe.Items.Count, Is.EqualTo(2));
        }

        [Test]
        public void ARepeatedCellIdKeepsOnlyTheFirstOne()
        {
            var cells = new[] { new ShelfCellContent("A-1", "gauze"), new ShelfCellContent("A-1", "needle") };

            RandomRecipe recipe = Make(1, Create(cells, 1, 4));

            Assert.That(recipe.Items.Select(item => item.ComponentId), Is.EqualTo(new[] { "gauze" }));
        }

        [Test]
        public void FewerSlotsThanTheMaximumLimitsTheItemCount()
        {
            RandomRecipe recipe = Create(null, 4, 4).Generate(new System.Random(5), "EM", 0, SlotPositions.Take(2).ToArray());

            Assert.That(recipe.Items.Count, Is.EqualTo(2));
        }

        [Test]
        public void TheKitCodeAndBoxSizeAreCarriedOverUnchanged()
        {
            RandomRecipe recipe = Create().Generate(new System.Random(1), "BL", 1, SlotPositions);

            Assert.That(recipe.KitCode, Is.EqualTo("BL"));
            Assert.That(recipe.BoxSize, Is.EqualTo(1));
        }

        [Test]
        public void ARandomThatContinuesMakesTheNextRecipeFromWhereItLeftOff()
        {
            // 여러 키트의 레시피를 같은 난수로 이어서 만들므로, 이어 부르면 같은 레시피가 되풀이되지 않아야 한다.
            // 우연히 같을 수 있어 한 쌍이 아니라 여러 시드의 쌍 중 다른 것이 있는지 본다.
            RandomRecipeGenerator generator = Create();
            int different = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                var random = new System.Random(seed);
                string first = generator.Generate(random, "EM", 0, SlotPositions).Describe();
                string second = generator.Generate(random, "EM", 0, SlotPositions).Describe();
                if (first != second)
                {
                    different++;
                }
            }

            Assert.That(different, Is.GreaterThan(10));
        }

        [Test]
        public void BadArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RandomRecipeGenerator(null, 1, 4));
            Assert.Throws<ArgumentException>(() => new RandomRecipeGenerator(new ShelfCellContent[0], 1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRecipeGenerator(Cells, 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRecipeGenerator(Cells, 3, 2));
            Assert.Throws<ArgumentNullException>(() => Create().Generate(null, "EM", 0, SlotPositions));
            Assert.Throws<ArgumentException>(() => Create().Generate(new System.Random(1), "EM", 0, new Vector3[0]));
        }

        [Test]
        public void TheShuffleKeepsEveryElementExactlyOnce()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                int[] numbers = Enumerable.Range(1, 8).ToArray();
                RandomShuffle.InPlace(new System.Random(seed), numbers);

                Assert.That(numbers.OrderBy(n => n), Is.EqualTo(Enumerable.Range(1, 8)), $"시드 {seed}");
            }
        }
    }
}
