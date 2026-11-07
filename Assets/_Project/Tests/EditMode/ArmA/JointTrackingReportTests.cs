using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class JointTrackingReportTests
    {
        private static readonly string[] Names = { "j1", "j2", "j5" };
        private static readonly float[] Lower = { -175f * Mathf.Deg2Rad, -175f * Mathf.Deg2Rad, -175f * Mathf.Deg2Rad };
        private static readonly float[] Upper = { 175f * Mathf.Deg2Rad, 175f * Mathf.Deg2Rad, 175f * Mathf.Deg2Rad };

        private static float[] Deg(params float[] degrees)
        {
            var rad = new float[degrees.Length];
            for (int i = 0; i < rad.Length; i++)
            {
                rad[i] = degrees[i] * Mathf.Deg2Rad;
            }

            return rad;
        }

        private static string Describe(float[] targetDeg, float[] currentDeg) =>
            JointTrackingReport.Describe(Names, Deg(targetDeg), Deg(currentDeg), Lower, Upper, 2f, 3f);

        [Test]
        public void WhenEveryJointFollows_SaysSo()
        {
            string text = Describe(new[] { 10f, 20f, 30f }, new[] { 10.5f, 19.5f, 30f });

            Assert.That(text, Does.Contain("모든 관절"));
            Assert.That(text, Does.Not.Contain("j1"));
        }

        [Test]
        public void ALaggingJoint_ShowsItsTargetCurrentAndError()
        {
            string text = Describe(new[] { 0f, 0f, -85f }, new[] { 0f, 0f, -60f });

            Assert.That(text, Does.Contain("j5"));
            Assert.That(text, Does.Contain("목표 -85.0°"));
            Assert.That(text, Does.Contain("현재 -60.0°"));
            Assert.That(text, Does.Contain("오차 25.0°"));
            Assert.That(text, Does.Not.Contain("j1"));
        }

        [Test]
        public void TheLimitsAreShownInDegrees()
        {
            string text = Describe(new[] { 0f, 0f, 90f }, new[] { 0f, 0f, 50f });

            Assert.That(text, Does.Contain("한계 -175~175°"));
        }

        [Test]
        public void ALaggingJointStuckAtTheUpperLimit_SaysItIsAgainstTheLimit()
        {
            string text = Describe(new[] { 0f, 0f, 200f }, new[] { 0f, 0f, 174f });

            Assert.That(text, Does.Contain("상한에 붙음"));
        }

        [Test]
        public void ALaggingJointStuckAtTheLowerLimit_SaysItIsAgainstTheLimit()
        {
            string text = Describe(new[] { 0f, 0f, -200f }, new[] { 0f, 0f, -174f });

            Assert.That(text, Does.Contain("하한에 붙음"));
        }

        [Test]
        public void ALaggingJointFarFromBothLimits_IsNotMarkedAsAgainstALimit()
        {
            string text = Describe(new[] { 0f, 0f, 90f }, new[] { 0f, 0f, 40f });

            Assert.That(text, Does.Not.Contain("붙음"));
        }

        [Test]
        public void SeveralLaggingJoints_AreListedWithTheWorstFirst()
        {
            string text = Describe(new[] { 30f, 10f, 0f }, new[] { 0f, 0f, 0f });

            Assert.That(text.IndexOf("j1", StringComparison.Ordinal), Is.LessThan(text.IndexOf("j2", StringComparison.Ordinal)), "오차 30도인 j1이 10도인 j2보다 먼저");
        }

        [Test]
        public void HasLagging_IsTrueWhenAnyJointIsOffByTheThresholdOrMore()
        {
            Assert.That(JointTrackingReport.HasLagging(Deg(0f, 0f, 0f), Deg(0f, 0f, 5f), 5f), Is.True);
            Assert.That(JointTrackingReport.HasLagging(Deg(0f, 0f, 0f), Deg(0f, -8f, 0f), 5f), Is.True);
        }

        [Test]
        public void HasLagging_IsFalseWhenEveryJointIsCloserThanTheThreshold()
        {
            Assert.That(JointTrackingReport.HasLagging(Deg(10f, 20f, 30f), Deg(11f, 18f, 33f), 5f), Is.False);
        }

        [Test]
        public void HasLagging_IsFalseWhenTheCountsDiffer()
        {
            Assert.That(JointTrackingReport.HasLagging(Deg(0f, 0f), Deg(90f, 90f, 90f), 5f), Is.False);
        }

        [Test]
        public void MismatchedCounts_Throw()
        {
            Assert.Throws<ArgumentException>(() =>
                JointTrackingReport.Describe(Names, Deg(0f, 0f), Deg(0f, 0f, 0f), Lower, Upper, 2f, 3f));
        }
    }
}
