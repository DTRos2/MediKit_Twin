using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Common.Tests
{
    public sealed class PositionSeriesTests
    {
        private const float Mm = 0.001f;

        [Test]
        public void Span_OfNothing_IsZero()
        {
            Assert.That(new PositionSeries().Span(), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Span_IsTheRangeOnEachAxis()
        {
            var series = new PositionSeries();
            series.Add(new Vector3(1f, 5f, -2f));
            series.Add(new Vector3(4f, 5f, 3f));
            series.Add(new Vector3(2f, 6f, 0f));

            Vector3 span = series.Span();

            Assert.That(span.x, Is.EqualTo(3f));
            Assert.That(span.y, Is.EqualTo(1f));
            Assert.That(span.z, Is.EqualTo(5f));
        }

        [Test]
        public void Jitter_WithFewerThanThreeSamples_HasNoValue()
        {
            var series = new PositionSeries();
            series.Add(Vector3.zero);
            series.Add(Vector3.one);

            Assert.That(series.Jitter().HasValue, Is.False);
        }

        [Test]
        public void Jitter_OfSteadyMotion_IsZero()
        {
            var series = new PositionSeries();
            for (int i = 0; i < 6; i++)
            {
                series.Add(new Vector3(i * 24f * Mm, 0f, 0f));
            }

            JitterStats jitter = series.Jitter();

            Assert.That(jitter.HasValue, Is.True);
            Assert.That(jitter.RmsM, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(jitter.MaxM, Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void Jitter_OfAnAlternatingPosition_IsTwiceTheAmplitude()
        {
            // 0, 1mm, 0, 1mm, 0: 모든 2차 차분의 크기가 2mm다.
            var series = new PositionSeries();
            for (int i = 0; i < 5; i++)
            {
                series.Add(new Vector3(i % 2 * Mm, 0f, 0f));
            }

            JitterStats jitter = series.Jitter();

            Assert.That(jitter.RmsM, Is.EqualTo(2f * Mm).Within(1e-6f));
            Assert.That(jitter.MaxM, Is.EqualTo(2f * Mm).Within(1e-6f));
        }

        [Test]
        public void Jitter_MaxIsTheWorstSingleStep()
        {
            // 일정하게 가다가 한 번 3mm 튀는 경우: 튄 곳 앞뒤로 2차 차분이 +3mm, -3mm 나온다.
            var series = new PositionSeries();
            series.Add(new Vector3(0f, 0f, 0f));
            series.Add(new Vector3(0f, 0f, 0f));
            series.Add(new Vector3(3f * Mm, 0f, 0f));
            series.Add(new Vector3(3f * Mm, 0f, 0f));

            JitterStats jitter = series.Jitter();

            Assert.That(jitter.MaxM, Is.EqualTo(3f * Mm).Within(1e-6f));
            Assert.That(jitter.RmsM, Is.EqualTo(3f * Mm).Within(1e-6f));
        }

        [Test]
        public void Clear_EmptiesTheSeries()
        {
            var series = new PositionSeries();
            series.Add(Vector3.one);
            series.Add(Vector3.zero);

            series.Clear();

            Assert.That(series.Count, Is.EqualTo(0));
            Assert.That(series.Span(), Is.EqualTo(Vector3.zero));
        }
    }
}
