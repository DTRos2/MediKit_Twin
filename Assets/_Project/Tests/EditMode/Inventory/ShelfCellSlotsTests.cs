using System;
using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.Inventory.Tests
{
    public sealed class ShelfCellSlotsTests
    {
        // 용량 5, 한 줄 3개: 앞줄은 슬롯 0~2, 뒷줄은 슬롯 3~4.
        private static ShelfCellSlots Cell() => new ShelfCellSlots(5, 3);

        [Test]
        public void New_StartsFull()
        {
            var cell = Cell();

            Assert.That(cell.Quantity, Is.EqualTo(5));
            Assert.That(Enumerable.Range(0, 5).All(cell.IsOccupied), Is.True);
        }

        [Test]
        public void SetQuantity_TakeOne_RemovesFrontSlotFirstAndNothingMoves()
        {
            var cell = Cell();

            var steps = cell.SetQuantity(4);

            Assert.That(steps.Count, Is.EqualTo(1));
            Assert.That(steps[0].Kind, Is.EqualTo(ShelfCellStepKind.Vacate));
            Assert.That(steps[0].FromSlot, Is.EqualTo(0));
            Assert.That(cell.IsOccupied(3) && cell.IsOccupied(4), Is.True);
        }

        [Test]
        public void SetQuantity_FrontRowNotEmpty_BackRowStaysPut()
        {
            var cell = Cell();

            var steps = cell.SetQuantity(3);

            Assert.That(steps.Select(s => s.Kind), Is.All.EqualTo(ShelfCellStepKind.Vacate));
            Assert.That(cell.IsOccupied(2), Is.True);
            Assert.That(cell.IsOccupied(3), Is.True);
        }

        [Test]
        public void SetQuantity_FrontRowEmptied_BackItemsSlideForwardOneRow()
        {
            var cell = Cell();

            var steps = cell.SetQuantity(2);

            Assert.That(steps.Take(3).Select(s => s.Kind), Is.All.EqualTo(ShelfCellStepKind.Vacate));
            Assert.That(steps.Skip(3).Select(s => (s.Kind, s.FromSlot, s.ToSlot)),
                Is.EqualTo(new[] { (ShelfCellStepKind.Move, 3, 0), (ShelfCellStepKind.Move, 4, 1) }));
            Assert.That(cell.IsOccupied(0) && cell.IsOccupied(1), Is.True);
            Assert.That(cell.IsOccupied(2) || cell.IsOccupied(3) || cell.IsOccupied(4), Is.False);
        }

        [Test]
        public void SetQuantity_AfterSlide_NextTakeComesFromTheNewFrontRow()
        {
            var cell = Cell();
            cell.SetQuantity(2);

            var steps = cell.SetQuantity(1);

            Assert.That(steps.Single().FromSlot, Is.EqualTo(0));
            Assert.That(cell.IsOccupied(1), Is.True);
        }

        [Test]
        public void SetQuantity_ToZero_VacatesEverythingEvenThroughSlides()
        {
            var cell = Cell();

            cell.SetQuantity(0);

            Assert.That(cell.Quantity, Is.EqualTo(0));
            Assert.That(Enumerable.Range(0, 5).Any(cell.IsOccupied), Is.False);
        }

        [Test]
        public void SetQuantity_SingleCallToZero_ReportsStepsInApplicableOrder()
        {
            // 한 번에 줄이면 슬라이드로 옮겨 온 물건이 다시 집힐 수 있으므로 단계 순서대로 처리해야 상태가 맞는다.
            var cell = Cell();
            var occupied = Enumerable.Repeat(true, 5).ToArray();

            foreach (var step in cell.SetQuantity(0))
            {
                switch (step.Kind)
                {
                    case ShelfCellStepKind.Vacate:
                        Assert.That(occupied[step.FromSlot], Is.True);
                        occupied[step.FromSlot] = false;
                        break;
                    case ShelfCellStepKind.Move:
                        Assert.That(occupied[step.FromSlot], Is.True);
                        Assert.That(occupied[step.ToSlot], Is.False);
                        occupied[step.FromSlot] = false;
                        occupied[step.ToSlot] = true;
                        break;
                }
            }

            Assert.That(occupied.Any(o => o), Is.False);
        }

        [Test]
        public void SetQuantity_Refill_FillsFrontSlotsFirst()
        {
            var cell = Cell();
            cell.SetQuantity(0);

            var steps = cell.SetQuantity(5);

            Assert.That(steps.Select(s => s.Kind), Is.All.EqualTo(ShelfCellStepKind.Fill));
            Assert.That(steps.Select(s => s.ToSlot), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(cell.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void SetQuantity_Return_FillsFirstEmptySlot()
        {
            var cell = Cell();
            cell.SetQuantity(4);

            var steps = cell.SetQuantity(5);

            Assert.That(steps.Single().Kind, Is.EqualTo(ShelfCellStepKind.Fill));
            Assert.That(steps.Single().ToSlot, Is.EqualTo(0));
        }

        [Test]
        public void SetQuantity_AboveCapacity_IsClampedToCapacity()
        {
            var cell = Cell();
            cell.SetQuantity(2);

            cell.SetQuantity(20);

            Assert.That(cell.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void SetQuantity_Negative_IsClampedToZero()
        {
            var cell = Cell();

            cell.SetQuantity(-3);

            Assert.That(cell.Quantity, Is.EqualTo(0));
        }

        [Test]
        public void SetQuantity_SameQuantity_ReturnsNoSteps()
        {
            Assert.That(Cell().SetQuantity(5), Is.Empty);
        }

        [Test]
        public void SetQuantity_SingleRowCell_NeverSlides()
        {
            var cell = new ShelfCellSlots(3, 3);

            var steps = cell.SetQuantity(0);

            Assert.That(steps.Select(s => s.Kind), Is.All.EqualTo(ShelfCellStepKind.Vacate));
        }

        [Test]
        public void Constructor_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfCellSlots(0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShelfCellSlots(5, 0));
        }
    }
}
