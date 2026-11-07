using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class LandingTraceTests
    {
        private static LandingTrace With(string contacts = "")
        {
            return new LandingTrace(3f, 0.5f, () => contacts);
        }

        [Test]
        public void ASlowFall_IsNotASpike()
        {
            LandingTrace trace = With();
            trace.Begin(Vector3.zero);
            trace.Sample(0.02f, Vector3.zero, 1.4f);

            Assert.That(trace.HasSpike, Is.False);
            Assert.That(trace.PeakSpeedMps, Is.EqualTo(1.4f));
        }

        [Test]
        public void ASpeedAtTheThreshold_IsASpike()
        {
            LandingTrace trace = With();
            trace.Begin(Vector3.zero);
            trace.Sample(0.02f, Vector3.zero, 3f);

            Assert.That(trace.HasSpike, Is.True);
        }

        [Test]
        public void KeepsTheFastestSpeedAndWhenItHappened()
        {
            LandingTrace trace = With();
            trace.Begin(Vector3.zero);
            trace.Sample(0.02f, Vector3.zero, 2f);
            trace.Sample(0.04f, Vector3.zero, 9f);
            trace.Sample(0.06f, Vector3.zero, 4f);

            Assert.That(trace.PeakSpeedMps, Is.EqualTo(9f));
            Assert.That(trace.PeakAtSeconds, Is.EqualTo(0.04f));
        }

        [Test]
        public void BeginForgetsThePreviousLanding()
        {
            LandingTrace trace = With();
            trace.Begin(Vector3.zero);
            trace.Sample(0.02f, Vector3.zero, 9f);

            trace.Begin(Vector3.zero);

            Assert.That(trace.HasSpike, Is.False);
            Assert.That(trace.PeakSpeedMps, Is.EqualTo(0f));
        }

        [Test]
        public void TheReport_NamesWhatTheItemOverlapped()
        {
            LandingTrace trace = With("KitBox/Wall_NegZ 0.4cm");
            trace.Begin(new Vector3(0f, 0.05f, -0.082f));

            Assert.That(trace.Describe(), Does.Contain("Wall_NegZ 0.4cm"));
        }

        [Test]
        public void TheReport_ShowsThePositionInCentimeters()
        {
            LandingTrace trace = With("x");
            trace.Begin(new Vector3(0f, 0.05f, -0.082f));

            Assert.That(trace.Describe(), Does.Contain("(0.0, 5.0, -8.2)cm"));
        }

        [Test]
        public void WithoutAnyOverlapOrSpeed_TheReportSaysNothingOverlapped()
        {
            LandingTrace trace = With();
            trace.Begin(Vector3.zero);
            trace.Sample(0.02f, Vector3.zero, 0.5f);

            Assert.That(trace.Describe(), Does.Contain("겹친 곳은 없었다"));
        }

        [Test]
        public void ASpikeIsReported_EvenWithoutAnOverlap()
        {
            LandingTrace trace = With();
            trace.Begin(Vector3.zero);
            trace.Sample(0.1f, Vector3.zero, 8f);

            Assert.That(trace.Describe(), Does.Contain("속도 8.0m/s"));
        }

        [Test]
        public void OverlapsAreOnlyAskedForWhileWatching()
        {
            int asked = 0;
            var trace = new LandingTrace(3f, 0.1f, () => { asked++; return string.Empty; });

            trace.Begin(Vector3.zero);
            trace.Sample(0.05f, Vector3.zero, 1f);
            trace.Sample(0.2f, Vector3.zero, 1f);
            trace.Sample(0.4f, Vector3.zero, 1f);

            Assert.That(asked, Is.EqualTo(2), "놓는 순간과 0.05초 한 번뿐이다");
        }

        [Test]
        public void TheReport_IsCappedSoTheLogStaysReadable()
        {
            LandingTrace trace = With("a");
            trace.Begin(Vector3.zero);
            for (int i = 1; i <= 20; i++)
            {
                trace.Sample(i * 0.02f, Vector3.zero, 1f);
            }

            Assert.That(trace.Describe(), Does.EndWith("…"));
        }

        [Test]
        public void WithoutAContactSource_NothingIsReportedAsOverlapped()
        {
            var trace = new LandingTrace(3f, 0.5f, null);
            trace.Begin(Vector3.zero);

            Assert.That(trace.Describe(), Does.Contain("겹친 곳은 없었다"));
        }
    }
}
