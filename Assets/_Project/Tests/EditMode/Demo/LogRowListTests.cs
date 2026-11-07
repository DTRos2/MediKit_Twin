using System;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class LogRowListTests
    {
        private static LogRowList NewList()
        {
            return new LogRowList(LogTagClassifier.Default);
        }

        private static LogBuffer BufferWith(params string[] texts)
        {
            var buffer = new LogBuffer(100);
            for (int i = 0; i < texts.Length; i++)
            {
                buffer.Add(i, LogSeverity.Info, texts[i]);
            }

            return buffer;
        }

        [Test]
        public void TheFirstRefresh_BuildsTheRows_OldestFirst_WithTheirTags()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("카메라 켬", "충돌 상자");

            bool isChanged = list.Refresh(buffer, LogSeverity.Info, 10);

            Assert.That(isChanged, Is.True);
            Assert.That(list.Rows.Count, Is.EqualTo(2));
            Assert.That(list.Rows[0].Line.Text, Is.EqualTo("카메라 켬"));
            Assert.That(list.Rows[1].Tag.Name, Is.EqualTo("COLLIDER"));
        }

        [Test]
        public void WhenNothingChanged_ItDoesNotRebuild()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a", "b");
            list.Refresh(buffer, LogSeverity.Info, 10);

            bool isChanged = list.Refresh(buffer, LogSeverity.Info, 10);

            Assert.That(isChanged, Is.False);
        }

        [Test]
        public void ANewLog_RebuildsTheRows()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a");
            list.Refresh(buffer, LogSeverity.Info, 10);

            buffer.Add(5, LogSeverity.Info, "b");
            bool isChanged = list.Refresh(buffer, LogSeverity.Info, 10);

            Assert.That(isChanged, Is.True);
            Assert.That(list.Rows.Count, Is.EqualTo(2));
        }

        [Test]
        public void ADifferentFilter_RebuildsTheRows()
        {
            LogRowList list = NewList();
            var buffer = new LogBuffer(10);
            buffer.Add(1, LogSeverity.Info, "i");
            buffer.Add(2, LogSeverity.Warning, "w");
            list.Refresh(buffer, LogSeverity.Info, 10);

            bool isChanged = list.Refresh(buffer, LogSeverity.Warning, 10);

            Assert.That(isChanged, Is.True);
            Assert.That(list.Rows.Count, Is.EqualTo(1));
            Assert.That(list.Rows[0].Tag.Name, Is.EqualTo("WARN"));
        }

        [Test]
        public void ADifferentLineLimit_RebuildsTheRows()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("1", "2", "3");
            list.Refresh(buffer, LogSeverity.Info, 10);

            Assert.That(list.Refresh(buffer, LogSeverity.Info, 2), Is.True);
            Assert.That(list.Rows.Count, Is.EqualTo(2));
        }

        [Test]
        public void OnlyTheMostRecentLines_UpToTheLimit_AreKept()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("1", "2", "3", "4", "5");

            list.Refresh(buffer, LogSeverity.Info, 2);

            Assert.That(list.Rows.Count, Is.EqualTo(2));
            Assert.That(list.Rows[0].Line.Text, Is.EqualTo("4"));
            Assert.That(list.Rows[1].Line.Text, Is.EqualTo("5"));
        }

        [Test]
        public void ClearingTheBuffer_EmptiesTheRows()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a", "b");
            list.Refresh(buffer, LogSeverity.Info, 10);

            buffer.Clear();
            list.Refresh(buffer, LogSeverity.Info, 10);

            Assert.That(list.Rows, Is.Empty);
        }

        [Test]
        public void ADifferentBuffer_RebuildsEvenIfItsVersionMatches()
        {
            LogRowList list = NewList();
            list.Refresh(BufferWith("a"), LogSeverity.Info, 10);

            bool isChanged = list.Refresh(BufferWith("z"), LogSeverity.Info, 10);

            Assert.That(isChanged, Is.True);
            Assert.That(list.Rows[0].Line.Text, Is.EqualTo("z"));
        }

        [Test]
        public void TheClassifier_CannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LogRowList(null));
        }
    }
}
