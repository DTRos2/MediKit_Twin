using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class PickPoseBookTests
    {
        private static TaughtPickPose Pose(string slotId, float armBaseX = 1f, float yaw = 0f) => TaughtPickPose.Capture(
            slotId,
            new Vector3(armBaseX - 0.03f, 0f, 1.98f),
            yaw,
            new Vector3(armBaseX, 0f, 2f),
            new Pose(new Vector3(armBaseX, 0.7f, 3f), Quaternion.Euler(0f, yaw, 0f)),
            new[] { 0.1f, -0.2f, 0.3f });

        [Test]
        public void ANewBookIsEmpty()
        {
            var book = new PickPoseBook();

            Assert.That(book.Count, Is.EqualTo(0));
            Assert.That(book.SlotIds, Is.Empty);
            Assert.That(book.TryGet("A-1", out _), Is.False);
        }

        [Test]
        public void ASavedPoseCanBeFoundBySlot()
        {
            var book = new PickPoseBook();
            book.Set(Pose("A-1", armBaseX: 1.88f));

            Assert.That(book.TryGet("A-1", out TaughtPickPose found), Is.True);
            Assert.That(found.ArmBasePosition.x, Is.EqualTo(1.88f).Within(1e-5f));
        }

        [Test]
        public void TheSlotLookupIgnoresCaseAndSpaces()
        {
            var book = new PickPoseBook();
            book.Set(Pose("A-1"));

            Assert.That(book.TryGet(" a-1 ", out _), Is.True);
        }

        [Test]
        public void SavingTheSameSlotAgainReplacesIt()
        {
            var book = new PickPoseBook();
            book.Set(Pose("A-1", armBaseX: 1f));
            book.Set(Pose("a-1", armBaseX: 2f));

            Assert.That(book.Count, Is.EqualTo(1));
            book.TryGet("A-1", out TaughtPickPose found);
            Assert.That(found.ArmBasePosition.x, Is.EqualTo(2f));
        }

        [Test]
        public void DifferentSlotsAreKeptSeparately_AndListedInOrder()
        {
            var book = new PickPoseBook();
            book.Set(Pose("B-1"));
            book.Set(Pose("A-2"));
            book.Set(Pose("A-1"));

            Assert.That(book.SlotIds, Is.EqualTo(new[] { "A-1", "A-2", "B-1" }));
        }

        [Test]
        public void RemoveDeletesOnlyThatSlot()
        {
            var book = new PickPoseBook();
            book.Set(Pose("A-1"));
            book.Set(Pose("A-2"));

            Assert.That(book.Remove("a-1"), Is.True);
            Assert.That(book.TryGet("A-1", out _), Is.False);
            Assert.That(book.TryGet("A-2", out _), Is.True);
        }

        [Test]
        public void RemovingAMissingSlot_ReturnsFalse()
        {
            Assert.That(new PickPoseBook().Remove("A-1"), Is.False);
        }

        [Test]
        public void JsonKeepsEveryPoseAndItsValues()
        {
            var book = new PickPoseBook();
            book.Set(Pose("A-1", armBaseX: 1.88f, yaw: 30f));
            book.Set(Pose("C-4", armBaseX: 3.5f, yaw: -45f));

            PickPoseBook restored = PickPoseBook.FromJson(book.ToJson());

            Assert.That(restored.SlotIds, Is.EqualTo(new[] { "A-1", "C-4" }));
            restored.TryGet("A-1", out TaughtPickPose first);
            Assert.That(first.ArmBasePosition.x, Is.EqualTo(1.88f).Within(1e-5f));
            Assert.That(first.RootPosition.x, Is.EqualTo(1.85f).Within(1e-5f), "ArmA 루트 자리도 파일에 남는다");
            Assert.That(first.RootPosition.z, Is.EqualTo(1.98f).Within(1e-5f));
            Assert.That(first.RootYawDeg, Is.EqualTo(30f).Within(1e-4f));
            Assert.That(first.ToolPose.position.y, Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(Quaternion.Angle(first.ToolPose.rotation, Quaternion.Euler(0f, 30f, 0f)), Is.LessThan(1e-2f));
            Assert.That(first.JointsRad(), Is.EqualTo(new[] { 0.1f, -0.2f, 0.3f }).Within(1e-5f));
            restored.TryGet("C-4", out TaughtPickPose second);
            Assert.That(second.RootYawDeg, Is.EqualTo(-45f).Within(1e-4f));
        }

        [Test]
        public void EmptyOrBlankJson_GivesAnEmptyBook()
        {
            Assert.That(PickPoseBook.FromJson(string.Empty).Count, Is.EqualTo(0));
            Assert.That(PickPoseBook.FromJson("   ").Count, Is.EqualTo(0));
            Assert.That(PickPoseBook.FromJson(null).Count, Is.EqualTo(0));
        }

        [Test]
        public void BrokenJson_Throws_SoTheExistingFileIsNotOverwritten()
        {
            Assert.Throws<ArgumentException>(() => PickPoseBook.FromJson("{ this is not json"));
        }

        [Test]
        public void SettingANullPose_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PickPoseBook().Set(null));
        }
    }
}
