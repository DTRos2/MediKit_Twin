using System;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class LogBufferTests
    {
        [Test]
        public void ToText_JoinsTheLines_OldestFirst()
        {
            var buffer = new LogBuffer(10);
            buffer.Add(1.0, LogSeverity.Info, "첫째");
            buffer.Add(2.5, LogSeverity.Info, "둘째");

            Assert.That(buffer.ToText(), Is.EqualTo("[1.0s] 첫째\n[2.5s] 둘째"));
        }

        [Test]
        public void WarningsAndErrors_AreMarkedInTheText()
        {
            var buffer = new LogBuffer(10);
            buffer.Add(1.0, LogSeverity.Warning, "조심");
            buffer.Add(2.0, LogSeverity.Error, "망함");

            Assert.That(buffer.ToText(), Is.EqualTo("[1.0s] 경고: 조심\n[2.0s] 오류: 망함"));
        }

        [Test]
        public void WhenFull_TheOldestLinesAreDropped()
        {
            var buffer = new LogBuffer(2);
            buffer.Add(1.0, LogSeverity.Info, "a");
            buffer.Add(2.0, LogSeverity.Info, "b");
            buffer.Add(3.0, LogSeverity.Info, "c");

            Assert.That(buffer.Lines.Count, Is.EqualTo(2));
            Assert.That(buffer.ToText(), Is.EqualTo("[2.0s] b\n[3.0s] c"));
        }

        [Test]
        public void ToText_WithAMinimum_KeepsOnlyThoseAtLeastAsSevere()
        {
            var buffer = new LogBuffer(10);
            buffer.Add(1.0, LogSeverity.Info, "a");
            buffer.Add(2.0, LogSeverity.Warning, "b");
            buffer.Add(3.0, LogSeverity.Error, "c");

            Assert.That(buffer.ToText(LogSeverity.Warning), Is.EqualTo("[2.0s] 경고: b\n[3.0s] 오류: c"));
            Assert.That(buffer.CountAtLeast(LogSeverity.Warning), Is.EqualTo(2));
            Assert.That(buffer.CountAtLeast(LogSeverity.Error), Is.EqualTo(1));
        }

        [Test]
        public void TailText_KeepsTheMostRecentLines_InTheirOriginalOrder()
        {
            var buffer = new LogBuffer(10);
            for (int i = 1; i <= 5; i++)
            {
                buffer.Add(i, LogSeverity.Info, $"줄{i}");
            }

            Assert.That(buffer.TailText(LogSeverity.Info, 2), Is.EqualTo("[4.0s] 줄4\n[5.0s] 줄5"));
        }

        [Test]
        public void TailText_CountsOnlyTheLinesThatPassTheFilter()
        {
            var buffer = new LogBuffer(10);
            buffer.Add(1.0, LogSeverity.Warning, "w1");
            buffer.Add(2.0, LogSeverity.Info, "i");
            buffer.Add(3.0, LogSeverity.Warning, "w2");

            Assert.That(buffer.TailText(LogSeverity.Warning, 2), Is.EqualTo("[1.0s] 경고: w1\n[3.0s] 경고: w2"));
        }

        [Test]
        public void TailLines_KeepsTheMostRecentLines_OldestFirst()
        {
            var buffer = new LogBuffer(10);
            for (int i = 1; i <= 5; i++)
            {
                buffer.Add(i, LogSeverity.Info, $"줄{i}");
            }

            var lines = buffer.TailLines(LogSeverity.Info, 2);

            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[0].Text, Is.EqualTo("줄4"));
            Assert.That(lines[1].Text, Is.EqualTo("줄5"));
        }

        [Test]
        public void TailLines_CountsOnlyTheLinesThatPassTheFilter()
        {
            var buffer = new LogBuffer(10);
            buffer.Add(1.0, LogSeverity.Warning, "w1");
            buffer.Add(2.0, LogSeverity.Info, "i");
            buffer.Add(3.0, LogSeverity.Error, "e1");

            var lines = buffer.TailLines(LogSeverity.Warning, 5);

            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[0].Text, Is.EqualTo("w1"));
            Assert.That(lines[1].Text, Is.EqualTo("e1"));
        }

        [Test]
        public void TailLines_OfAnEmptyBuffer_IsEmpty()
        {
            Assert.That(new LogBuffer(3).TailLines(LogSeverity.Info, 5), Is.Empty);
        }

        [Test]
        public void TailLines_IsACopy_SoLaterLogsDoNotChangeAnEarlierResult()
        {
            var buffer = new LogBuffer(10);
            buffer.Add(1.0, LogSeverity.Info, "a");
            var lines = buffer.TailLines(LogSeverity.Info, 5);

            buffer.Add(2.0, LogSeverity.Info, "b");

            Assert.That(lines.Count, Is.EqualTo(1));
        }

        [Test]
        public void AnEmptyBuffer_GivesAnEmptyText()
        {
            Assert.That(new LogBuffer(3).ToText(), Is.Empty);
        }

        [Test]
        public void Version_ChangesWhenLinesAreAddedOrCleared_ButNotWhenClearingAnEmptyBuffer()
        {
            var buffer = new LogBuffer(3);
            int start = buffer.Version;

            buffer.Clear();
            Assert.That(buffer.Version, Is.EqualTo(start), "비어 있으면 바뀐 게 없다");

            buffer.Add(1.0, LogSeverity.Info, "a");
            int afterAdd = buffer.Version;
            Assert.That(afterAdd, Is.Not.EqualTo(start));

            buffer.Clear();
            Assert.That(buffer.Version, Is.Not.EqualTo(afterAdd));
            Assert.That(buffer.Lines, Is.Empty);
        }

        [Test]
        public void TheCapacity_MustBeAtLeastOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogBuffer(0));
        }
    }
}
