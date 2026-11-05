using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TaughtPickPoseRepositoryTests
    {
        private static TaughtPickPose Pose(string slotId, float armBaseX = 1f) => TaughtPickPose.Capture(
            slotId,
            new Vector3(armBaseX - 0.03f, 0f, 1.98f),
            0f,
            new Vector3(armBaseX, 0f, 2f),
            new Pose(new Vector3(armBaseX, 0.7f, 3f), Quaternion.identity),
            new[] { 0f, 0f, 0f });

        [Test]
        public void ASavedPoseCanBeReadBack()
        {
            var repository = new TaughtPickPoseRepository(new InMemoryPickPoseStorage());

            repository.Save(Pose("A-1", armBaseX: 1.88f));

            Assert.That(repository.TryGet("A-1", out TaughtPickPose found), Is.True);
            Assert.That(found.ArmBasePosition.x, Is.EqualTo(1.88f).Within(1e-5f));
        }

        [Test]
        public void AnotherRepositoryOnTheSameStorageSeesTheSave()
        {
            // 패널이 저장한 것을 집으러 가는 쪽이 같은 파일을 통해 바로 본다.
            var storage = new InMemoryPickPoseStorage();
            var panelSide = new TaughtPickPoseRepository(storage);
            var transferSide = new TaughtPickPoseRepository(storage);

            Assert.That(transferSide.TryGet("A-1", out _), Is.False);
            panelSide.Save(Pose("A-1"));

            Assert.That(transferSide.TryGet("A-1", out _), Is.True, "읽을 때마다 저장소에서 다시 읽는다");
        }

        [Test]
        public void SavingASecondSlot_KeepsTheFirst()
        {
            var repository = new TaughtPickPoseRepository(new InMemoryPickPoseStorage());

            repository.Save(Pose("A-1"));
            repository.Save(Pose("A-2"));

            Assert.That(repository.SlotIds(), Is.EqualTo(new[] { "A-1", "A-2" }));
        }

        [Test]
        public void SavingTheSameSlotAgain_ReplacesIt()
        {
            var repository = new TaughtPickPoseRepository(new InMemoryPickPoseStorage());

            repository.Save(Pose("A-1", armBaseX: 1f));
            repository.Save(Pose("A-1", armBaseX: 2f));

            Assert.That(repository.SlotIds().Count, Is.EqualTo(1));
            repository.TryGet("A-1", out TaughtPickPose found);
            Assert.That(found.ArmBasePosition.x, Is.EqualTo(2f));
        }

        [Test]
        public void Remove_DeletesTheSlotAndWritesOnce()
        {
            var storage = new InMemoryPickPoseStorage();
            var repository = new TaughtPickPoseRepository(storage);
            repository.Save(Pose("A-1"));
            int writesBefore = storage.WriteCount;

            Assert.That(repository.Remove("A-1"), Is.True);

            Assert.That(repository.TryGet("A-1", out _), Is.False);
            Assert.That(storage.WriteCount, Is.EqualTo(writesBefore + 1));
        }

        [Test]
        public void RemovingAMissingSlot_DoesNotWriteAnything()
        {
            var storage = new InMemoryPickPoseStorage();
            var repository = new TaughtPickPoseRepository(storage);

            Assert.That(repository.Remove("A-1"), Is.False);
            Assert.That(storage.WriteCount, Is.EqualTo(0));
        }

        [Test]
        public void WhenTheStorageIsBroken_SavingThrowsAndKeepsTheOldText()
        {
            var storage = new InMemoryPickPoseStorage { Text = "{ broken" };
            var repository = new TaughtPickPoseRepository(storage);

            Assert.Throws<ArgumentException>(() => repository.Save(Pose("A-1")));

            Assert.That(storage.Text, Is.EqualTo("{ broken"), "읽지 못한 글자를 덮어써서 기존 자세를 잃지 않는다");
            Assert.That(storage.WriteCount, Is.EqualTo(0));
        }

        [Test]
        public void WhenTheStorageIsBroken_ReadingThrows()
        {
            var repository = new TaughtPickPoseRepository(new InMemoryPickPoseStorage { Text = "{ broken" });

            Assert.Throws<ArgumentException>(() => repository.TryGet("A-1", out _));
        }

        [Test]
        public void Constructor_NullStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TaughtPickPoseRepository(null));
        }
    }
}
