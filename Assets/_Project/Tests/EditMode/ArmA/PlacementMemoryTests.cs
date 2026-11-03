using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class PlacementMemoryTests
    {
        private const float RadiusM = 0.02f;
        private static readonly Vector3 SlotA = new Vector3(1f, 0.5f, 2f);
        private static readonly Vector3 SlotB = new Vector3(1f, 0.5f, 2.06f);

        private static PlacementRecall Recall(int heightIndex) => new PlacementRecall(heightIndex, true, 0f, 0f, 0f, 0f, 0f);

        [TestCase(0f)]
        [TestCase(-0.01f)]
        public void ARadiusThatIsNotPositive_IsRejected(float radius)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementMemory(radius));
        }

        [Test]
        public void WithNothingRemembered_NothingIsRecalled_AndThereIsNoHeightHint()
        {
            var memory = new PlacementMemory(RadiusM);

            Assert.That(memory.TryRecall(SlotA, "A-1", out _), Is.False);
            Assert.That(memory.TryGetLowestHeightIndex(SlotA, out int index), Is.False);
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void ARememberedPlacement_IsRecalledForTheSameSlotAndItem()
        {
            var memory = new PlacementMemory(RadiusM);
            var remembered = new PlacementRecall(2, false, 15f, -30f, 90f, 0.1f, -0.1f);

            memory.Remember(SlotA, "C-4", remembered);

            Assert.That(memory.TryRecall(SlotA, "C-4", out PlacementRecall recall), Is.True);
            Assert.That(recall.HeightIndex, Is.EqualTo(2));
            Assert.That(recall.UsesRestingOrientation, Is.False);
            Assert.That(recall.PitchDeg, Is.EqualTo(15f));
            Assert.That(recall.YawDeg, Is.EqualTo(-30f));
            Assert.That(recall.RollDeg, Is.EqualTo(90f));
            Assert.That(recall.StandoffExtraM, Is.EqualTo(0.1f));
            Assert.That(recall.LateralM, Is.EqualTo(-0.1f));
        }

        [Test]
        public void ASlotWithinTheRadius_IsTheSameSlot_ButOneBeyondItIsNot()
        {
            var memory = new PlacementMemory(RadiusM);
            memory.Remember(SlotA, "A-1", Recall(1));

            // 박스가 지그에 조금 다르게 멈춰도 같은 슬롯으로 알아본다.
            Assert.That(memory.TryRecall(SlotA + new Vector3(0.01f, 0f, 0f), "A-1", out _), Is.True);
            Assert.That(memory.TryRecall(SlotA + new Vector3(0.03f, 0f, 0f), "A-1", out _), Is.False);
            Assert.That(memory.TryRecall(SlotB, "A-1", out _), Is.False, "이웃 슬롯은 다른 슬롯이다");
        }

        [Test]
        public void ADifferentItemInTheSameSlot_IsNotRecalled()
        {
            var memory = new PlacementMemory(RadiusM);
            memory.Remember(SlotA, "A-4", Recall(3));

            Assert.That(memory.TryRecall(SlotA, "C-4", out _), Is.False);
        }

        [Test]
        public void RememberingTheSameSlotAndItemAgain_ReplacesTheOldPlacement()
        {
            var memory = new PlacementMemory(RadiusM);
            memory.Remember(SlotA, "A-1", Recall(1));

            memory.Remember(SlotA, "A-1", Recall(3));

            Assert.That(memory.Count, Is.EqualTo(1));
            memory.TryRecall(SlotA, "A-1", out PlacementRecall recall);
            Assert.That(recall.HeightIndex, Is.EqualTo(3));
        }

        [Test]
        public void Forgetting_RemovesOnlyThatSlotAndItem()
        {
            var memory = new PlacementMemory(RadiusM);
            memory.Remember(SlotA, "A-1", Recall(1));
            memory.Remember(SlotA, "B-1", Recall(2));
            memory.Remember(SlotB, "A-1", Recall(3));

            memory.Forget(SlotA, "A-1");

            Assert.That(memory.TryRecall(SlotA, "A-1", out _), Is.False);
            Assert.That(memory.TryRecall(SlotA, "B-1", out _), Is.True);
            Assert.That(memory.TryRecall(SlotB, "A-1", out _), Is.True);
        }

        [Test]
        public void ForgettingSomethingNotRemembered_ChangesNothing()
        {
            var memory = new PlacementMemory(RadiusM);
            memory.Remember(SlotA, "A-1", Recall(1));

            memory.Forget(SlotB, "A-1");

            Assert.That(memory.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheHeightHint_IsTheLowestHeightAnyItemSucceededAtInThatSlot()
        {
            var memory = new PlacementMemory(RadiusM);
            memory.Remember(SlotA, "A-1", Recall(3));
            memory.Remember(SlotA, "B-1", Recall(2));
            memory.Remember(SlotB, "A-2", Recall(0));

            Assert.That(memory.TryGetLowestHeightIndex(SlotA, out int index), Is.True);
            Assert.That(index, Is.EqualTo(2), "다른 슬롯(SlotB)의 더 낮은 높이는 섞이지 않는다");
        }

        [Test]
        public void AnItemKeyThatIsNull_CountsAsTheEmptyKey()
        {
            var memory = new PlacementMemory(RadiusM);
            memory.Remember(SlotA, null, Recall(2));

            Assert.That(memory.TryRecall(SlotA, string.Empty, out _), Is.True);
            Assert.That(memory.TryRecall(SlotA, null, out _), Is.True);
        }
    }
}
