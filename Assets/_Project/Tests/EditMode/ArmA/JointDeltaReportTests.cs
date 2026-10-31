using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class JointDeltaReportTests
    {
        [Test]
        public void Format_ShowsEachJointsChangeInDegreesWithItsSign()
        {
            string text = JointDeltaReport.Format(
                new[] { "j1", "j2", "j3" },
                new[] { 0f, 0f, 0f },
                new[] { 10f * Mathf.Deg2Rad, -20f * Mathf.Deg2Rad, 0f });

            Assert.That(text, Is.EqualTo("j1 +10.0° / j2 -20.0° / j3 0.0°"));
        }
    }
}
