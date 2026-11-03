using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class LogRowListTests
    {
        private int _measureCount;

        private LogRowList NewList()
        {
            _measureCount = 0;
            return new LogRowList(LogTagClassifier.Default, (text, width) =>
            {
                _measureCount++;
                return 30f + text.Length;
            });
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
        public void TheFirstRefresh_BuildsTheRows_OldestFirst_WithTheirTagsAndHeights()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("카메라 켬", "충돌 상자");

            bool isChanged = list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            Assert.That(isChanged, Is.True);
            Assert.That(list.Rows.Count, Is.EqualTo(2));
            Assert.That(list.Rows[0].Line.Text, Is.EqualTo("카메라 켬"));
            Assert.That(list.Rows[1].Tag.Name, Is.EqualTo("COLLIDER"));
            Assert.That(list.Rows[0].Height, Is.EqualTo(30f + "카메라 켬".Length));
        }

        [Test]
        public void WhenNothingChanged_ItDoesNotRebuildOrMeasureAgain()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a", "b");
            list.Refresh(buffer, LogSeverity.Info, 10, 300f);
            int measuredAfterFirst = _measureCount;

            bool isChanged = list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            Assert.That(isChanged, Is.False);
            Assert.That(_measureCount, Is.EqualTo(measuredAfterFirst), "글 높이는 비싸니 다시 재지 않는다");
        }

        [Test]
        public void ANewLog_RebuildsTheRows()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a");
            list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            buffer.Add(5, LogSeverity.Info, "b");
            bool isChanged = list.Refresh(buffer, LogSeverity.Info, 10, 300f);

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
            list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            bool isChanged = list.Refresh(buffer, LogSeverity.Warning, 10, 300f);

            Assert.That(isChanged, Is.True);
            Assert.That(list.Rows.Count, Is.EqualTo(1));
            Assert.That(list.Rows[0].Tag.Name, Is.EqualTo("WARN"));
        }

        [Test]
        public void ADifferentWidth_RebuildsTheRows_BecauseTheWrappedHeightChanges()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a");
            list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            Assert.That(list.Refresh(buffer, LogSeverity.Info, 10, 200f), Is.True);
        }

        [Test]
        public void ATinyWidthDifference_IsIgnored()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a");
            list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            Assert.That(list.Refresh(buffer, LogSeverity.Info, 10, 300.2f), Is.False);
        }

        [Test]
        public void OnlyTheMostRecentLines_UpToTheLimit_AreKept()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("1", "2", "3", "4", "5");

            list.Refresh(buffer, LogSeverity.Info, 2, 300f);

            Assert.That(list.Rows.Count, Is.EqualTo(2));
            Assert.That(list.Rows[0].Line.Text, Is.EqualTo("4"));
            Assert.That(list.Rows[1].Line.Text, Is.EqualTo("5"));
        }

        [Test]
        public void ClearingTheBuffer_EmptiesTheRows()
        {
            LogRowList list = NewList();
            LogBuffer buffer = BufferWith("a", "b");
            list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            buffer.Clear();
            list.Refresh(buffer, LogSeverity.Info, 10, 300f);

            Assert.That(list.Rows, Is.Empty);
        }

        [Test]
        public void ADifferentBuffer_RebuildsEvenIfItsVersionMatches()
        {
            LogRowList list = NewList();
            list.Refresh(BufferWith("a"), LogSeverity.Info, 10, 300f);

            bool isChanged = list.Refresh(BufferWith("z"), LogSeverity.Info, 10, 300f);

            Assert.That(isChanged, Is.True);
            Assert.That(list.Rows[0].Line.Text, Is.EqualTo("z"));
        }

        [Test]
        public void TheDependencies_CannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LogRowList(null, (t, w) => 0f));
            Assert.Throws<ArgumentNullException>(() => new LogRowList(LogTagClassifier.Default, null));
        }
    }
}
