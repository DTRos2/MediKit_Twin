using System.Collections.Generic;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ShelfRowsTests
    {
        private static readonly string[] ThreeByFour =
        {
            "A-1", "A-2", "A-3", "A-4",
            "B-1", "B-2", "B-3", "B-4",
            "C-1", "C-2", "C-3", "C-4",
        };

        [Test]
        public void TheSlots_AreGroupedByRowLetter_InOrder()
        {
            IReadOnlyList<ShelfRow> rows = ShelfRows.Group(ThreeByFour);

            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows[0].Letter, Is.EqualTo('A'));
            Assert.That(rows[1].Letter, Is.EqualTo('B'));
            Assert.That(rows[2].Letter, Is.EqualTo('C'));
            Assert.That(rows[1].SlotIds, Is.EqualTo(new[] { "B-1", "B-2", "B-3", "B-4" }));
        }

        [Test]
        public void ARowWithFewerSlots_IsKeptAsIs()
        {
            IReadOnlyList<ShelfRow> rows = ShelfRows.Group(new[] { "A-1", "A-2", "B-1" });

            Assert.That(rows[0].SlotIds.Count, Is.EqualTo(2));
            Assert.That(rows[1].SlotIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void ANewRow_StartsEvenWhenTheLetterReturnsLater()
        {
            // 목록이 A, B, A 순서면 선반의 모양을 그대로 따라 세 줄이 된다(같은 글자를 합치지 않는다).
            IReadOnlyList<ShelfRow> rows = ShelfRows.Group(new[] { "A-1", "B-1", "A-2" });

            Assert.That(rows.Count, Is.EqualTo(3));
        }

        [Test]
        public void EmptyOrNullIds_AreSkipped()
        {
            IReadOnlyList<ShelfRow> rows = ShelfRows.Group(new[] { "A-1", string.Empty, null, "A-2" });

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].SlotIds, Is.EqualTo(new[] { "A-1", "A-2" }));
        }

        [Test]
        public void NoSlots_GiveNoRows()
        {
            Assert.That(ShelfRows.Group(new string[0]), Is.Empty);
            Assert.That(ShelfRows.Group(null), Is.Empty);
        }
    }
}
