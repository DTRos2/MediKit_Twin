using System;
using System.IO;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class FilePickPoseStorageTests
    {
        private string _directory;
        private string _path;

        [SetUp]
        public void CreateTempDirectory()
        {
            _directory = Path.Combine(Path.GetTempPath(), "MediKitTwin_" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, "nested", FilePickPoseStorage.FileName);
        }

        [TearDown]
        public void DeleteTempDirectory()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void ReadingAMissingFile_GivesAnEmptyText()
        {
            Assert.That(new FilePickPoseStorage(_path).Read(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void WrittenTextCanBeReadBack_IncludingKorean()
        {
            var storage = new FilePickPoseStorage(_path);

            storage.Write("{\"메모\": \"칸 A-1\"}");

            Assert.That(storage.Read(), Is.EqualTo("{\"메모\": \"칸 A-1\"}"));
        }

        [Test]
        public void WritingCreatesTheMissingFolders()
        {
            new FilePickPoseStorage(_path).Write("x");

            Assert.That(File.Exists(_path), Is.True);
        }

        [Test]
        public void WritingAgainReplacesTheOldText_AndLeavesNoTempFile()
        {
            var storage = new FilePickPoseStorage(_path);

            storage.Write("first");
            storage.Write("second");

            Assert.That(storage.Read(), Is.EqualTo("second"));
            Assert.That(File.Exists(_path + ".tmp"), Is.False);
        }

        [Test]
        public void EmptyPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => new FilePickPoseStorage("  "));
        }

        [Test]
        public void ExposesItsFilePath()
        {
            Assert.That(new FilePickPoseStorage(_path).FilePath, Is.EqualTo(_path));
        }
    }
}
