using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class FallbackPickPoseStorageTests
    {
        private InMemoryPickPoseStorage _primary;
        private InMemoryPickPoseStorage _fallback;
        private FallbackPickPoseStorage _storage;

        [SetUp]
        public void CreateStorages()
        {
            _primary = new InMemoryPickPoseStorage();
            _fallback = new InMemoryPickPoseStorage { Text = "기본값" };
            _storage = new FallbackPickPoseStorage(_primary, _fallback);
        }

        private static TaughtPickPose Pose(string slotId) => TaughtPickPose.Capture(
            slotId,
            new Vector3(0.97f, 0f, 1.98f),
            0f,
            new Vector3(1f, 0f, 2f),
            new Pose(new Vector3(1f, 0.7f, 3f), Quaternion.identity),
            new[] { 0f, 0f, 0f });

        [Test]
        public void WhenThePrimaryIsEmpty_ReadingGivesTheFallbackText()
        {
            Assert.That(_storage.Read(), Is.EqualTo("기본값"));
        }

        [Test]
        public void WhenThePrimaryIsOnlyWhitespace_ReadingGivesTheFallbackText()
        {
            _primary.Text = "  \n";

            Assert.That(_storage.Read(), Is.EqualTo("기본값"));
        }

        [Test]
        public void WhenThePrimaryHasText_ReadingGivesThePrimaryText()
        {
            _primary.Text = "내 자세";

            Assert.That(_storage.Read(), Is.EqualTo("내 자세"));
        }

        [Test]
        public void Writing_GoesToThePrimaryOnly()
        {
            _storage.Write("새 글자");

            Assert.That(_primary.Text, Is.EqualTo("새 글자"));
            Assert.That(_fallback.Text, Is.EqualTo("기본값"));
            Assert.That(_fallback.WriteCount, Is.EqualTo(0));
        }

        [Test]
        public void AfterWriting_ReadingGivesWhatWasWritten_NotTheFallback()
        {
            _storage.Write("새 글자");

            Assert.That(_storage.Read(), Is.EqualTo("새 글자"));
        }

        [Test]
        public void Constructor_NullStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FallbackPickPoseStorage(null, _fallback));
            Assert.Throws<ArgumentNullException>(() => new FallbackPickPoseStorage(_primary, null));
        }

        [Test]
        public void SavingAPose_WhenOnlyTheFallbackHasPoses_CopiesTheFallbackPosesToThePrimary()
        {
            // 빌드 첫 실행: 사용자 데이터 폴더(primary)는 비었고 빌드에 실린 기본 자세(fallback)에 두 칸이 있다.
            var seed = new TaughtPickPoseRepository(_fallback);
            _fallback.Text = string.Empty;
            seed.Save(Pose("A-1"));
            seed.Save(Pose("A-2"));
            int fallbackWrites = _fallback.WriteCount;
            var repository = new TaughtPickPoseRepository(_storage);

            Assert.That(repository.TryGet("A-1", out _), Is.True, "기본 자세를 찾는다");
            repository.Save(Pose("A-3"));

            Assert.That(new TaughtPickPoseRepository(_primary).SlotIds(), Is.EqualTo(new[] { "A-1", "A-2", "A-3" }), "기본 자세도 함께 옮겨 적는다");
            Assert.That(_fallback.WriteCount, Is.EqualTo(fallbackWrites), "기본 자세 저장소는 바꾸지 않는다");
        }
    }
}
