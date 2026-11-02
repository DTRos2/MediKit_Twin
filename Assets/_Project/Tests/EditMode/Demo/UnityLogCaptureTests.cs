using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class UnityLogCaptureTests
    {
        private LogBuffer _buffer;
        private UnityLogCapture _capture;
        private double _now;

        [SetUp]
        public void SetUp()
        {
            _buffer = new LogBuffer(50);
            _now = 12.34;
            _capture = new UnityLogCapture(_buffer, () => _now);
        }

        [TearDown]
        public void TearDown()
        {
            _capture.Stop();
        }

        [Test]
        public void SeverityMap_TreatsWarningsAsWarnings_AndExceptionsAndAssertsAsErrors()
        {
            Assert.That(LogSeverityMap.From(LogType.Log), Is.EqualTo(LogSeverity.Info));
            Assert.That(LogSeverityMap.From(LogType.Warning), Is.EqualTo(LogSeverity.Warning));
            Assert.That(LogSeverityMap.From(LogType.Error), Is.EqualTo(LogSeverity.Error));
            Assert.That(LogSeverityMap.From(LogType.Assert), Is.EqualTo(LogSeverity.Error));
            Assert.That(LogSeverityMap.From(LogType.Exception), Is.EqualTo(LogSeverity.Error));
        }

        [Test]
        public void ALogAfterStart_IsCaptured_WithTheCurrentTime()
        {
            _capture.Start();

            Debug.Log("캡처 시험 글");

            Assert.That(_buffer.ToText(), Does.Contain("[12.3s] 캡처 시험 글"));
        }

        [Test]
        public void AWarningAfterStart_IsCapturedAsAWarning()
        {
            _capture.Start();

            Debug.LogWarning("캡처 시험 경고");

            Assert.That(_buffer.ToText(LogSeverity.Warning), Does.Contain("경고: 캡처 시험 경고"));
        }

        [Test]
        public void ALogBeforeStart_AndAfterStop_IsNotCaptured()
        {
            Debug.Log("시작 전");
            _capture.Start();
            _capture.Stop();
            Debug.Log("멈춘 뒤");

            Assert.That(_buffer.ToText(), Does.Not.Contain("시작 전"));
            Assert.That(_buffer.ToText(), Does.Not.Contain("멈춘 뒤"));
            Assert.That(_capture.IsCapturing, Is.False);
        }

        [Test]
        public void StartingTwice_DoesNotCaptureEachLogTwice()
        {
            _capture.Start();
            _capture.Start();

            Debug.Log("한 번만");

            Assert.That(_buffer.CountAtLeast(LogSeverity.Info), Is.EqualTo(CountOf("한 번만")));
            Assert.That(CountOf("한 번만"), Is.EqualTo(1));
        }

        [Test]
        public void TheBufferAndTheClock_AreRequired()
        {
            Assert.Throws<ArgumentNullException>(() => new UnityLogCapture(null, () => 0d));
            Assert.Throws<ArgumentNullException>(() => new UnityLogCapture(_buffer, null));
        }

        private int CountOf(string text)
        {
            int count = 0;
            foreach (LogLine line in _buffer.Lines)
            {
                if (line.Text.Contains(text))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
