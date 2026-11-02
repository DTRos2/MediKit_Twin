using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.PlayMode.Tests
{
    public sealed class RandomOrderGeneratorTests
    {
        private static readonly string[] Codes = { "EM", "DX", "BL" };
        private static readonly string[] Hospitals = { "가병원", "나병원" };
        private const int MaxPerLine = 2;

        private static RandomOrderGenerator Create(IEnumerable<string> codes = null, int maxPerLine = MaxPerLine)
        {
            return new RandomOrderGenerator(codes ?? Codes, Hospitals, maxPerLine);
        }

        // 주문 글을 (순번, 병원, 코드, 수량) 줄로 쪼갠다.
        private static List<string[]> Rows(GeneratedOrder order)
        {
            return order.Csv.Split('\n').Select(line => line.Split(',')).ToList();
        }

        [Test]
        public void TheSameSeedMakesTheSameOrder()
        {
            Assert.That(Create().Generate(7, 6).Csv, Is.EqualTo(Create().Generate(7, 6).Csv));
        }

        [Test]
        public void DifferentSeedsMakeDifferentOrders()
        {
            var distinct = Enumerable.Range(1, 12).Select(seed => Create().Generate(seed, 6).Csv).Distinct().Count();

            Assert.That(distinct, Is.GreaterThan(1));
        }

        [Test]
        public void TheQuantitiesAddUpToExactlyTheRequestedKitCount()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (int kits in new[] { 1, 2, 5, 9 })
                {
                    GeneratedOrder order = Create().Generate(seed, kits);

                    Assert.That(Rows(order).Sum(row => int.Parse(row[3])), Is.EqualTo(kits), $"시드 {seed}, 키트 {kits}");
                    Assert.That(order.KitCount, Is.EqualTo(kits));
                }
            }
        }

        [Test]
        public void EveryLineHasFourColumnsAValidCodeAndAQuantityWithinTheLimit()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (string[] row in Rows(Create().Generate(seed, 8)))
                {
                    Assert.That(row.Length, Is.EqualTo(4));
                    Assert.That(Hospitals, Does.Contain(row[1]));
                    Assert.That(Codes, Does.Contain(row[2]));
                    Assert.That(int.Parse(row[3]), Is.InRange(1, MaxPerLine));
                }
            }
        }

        [Test]
        public void TheSequenceNumbersCountUpFromOneWithoutGaps()
        {
            List<string[]> rows = Rows(Create().Generate(3, 9));

            Assert.That(rows.Select(row => int.Parse(row[0])), Is.EqualTo(Enumerable.Range(1, rows.Count)));
        }

        [Test]
        public void TheOrderOfTheGivenCodesDoesNotChangeTheResult()
        {
            string forward = Create(new[] { "EM", "DX", "BL" }).Generate(11, 7).Csv;
            string backward = Create(new[] { "BL", "DX", "EM" }).Generate(11, 7).Csv;

            Assert.That(backward, Is.EqualTo(forward));
        }

        [Test]
        public void CodesAreTrimmedUppercasedAndDeduplicated()
        {
            GeneratedOrder order = Create(new[] { " em ", "EM", "em" }).Generate(5, 6);

            Assert.That(Rows(order).Select(row => row[2]).Distinct(), Is.EqualTo(new[] { "EM" }));
        }

        [Test]
        public void WithOneKitTheOrderIsASingleLineOfOne()
        {
            GeneratedOrder order = Create().Generate(1, 1);

            Assert.That(Rows(order).Count, Is.EqualTo(1));
            Assert.That(Rows(order)[0][3], Is.EqualTo("1"));
        }

        [Test]
        public void WhenEachLineIsLimitedToOne_EveryLineIsOneKit()
        {
            GeneratedOrder order = Create(maxPerLine: 1).Generate(2, 6);

            Assert.That(Rows(order).Count, Is.EqualTo(6));
            Assert.That(Rows(order).All(row => row[3] == "1"), Is.True);
        }

        [Test]
        public void ManySeedsMixTheKitTypes()
        {
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 30; seed++)
            {
                foreach (string[] row in Rows(Create().Generate(seed, 6)))
                {
                    seen.Add(row[2]);
                }
            }

            Assert.That(seen, Is.EquivalentTo(Codes));
        }

        [Test]
        public void TheSummaryNamesEachLineSoTheKitOrderIsVisible()
        {
            GeneratedOrder order = Create(maxPerLine: 1).Generate(2, 3);

            Assert.That(order.Summary.Split(',').Length, Is.EqualTo(3));
            Assert.That(order.Summary, Does.Contain("×1"));
            Assert.That(order.Describe(), Does.Contain("시드 2").And.Contain("키트 3개"));
        }

        [Test]
        public void InvalidArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RandomOrderGenerator(null, Hospitals, 2));
            Assert.Throws<ArgumentException>(() => new RandomOrderGenerator(new string[0], Hospitals, 2));
            Assert.Throws<ArgumentException>(() => new RandomOrderGenerator(new[] { " " }, Hospitals, 2));
            Assert.Throws<ArgumentException>(() => new RandomOrderGenerator(Codes, new string[0], 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomOrderGenerator(Codes, Hospitals, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create().Generate(1, 0));
        }
    }
}
