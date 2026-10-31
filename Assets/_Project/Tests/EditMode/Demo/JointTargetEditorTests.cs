using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class JointTargetEditorTests
    {
        private const float LowerDeg = -90f;
        private const float UpperDeg = 120f;

        private static JointTargetEditor Create(int count = 2)
        {
            var lower = new float[count];
            var upper = new float[count];
            for (int i = 0; i < count; i++)
            {
                lower[i] = LowerDeg * Mathf.Deg2Rad;
                upper[i] = UpperDeg * Mathf.Deg2Rad;
            }

            return new JointTargetEditor(lower, upper);
        }

        [Test]
        public void LimitsAreReportedInDegrees()
        {
            JointTargetEditor editor = Create();

            Assert.That(editor.LowerDeg(0), Is.EqualTo(LowerDeg).Within(1e-3f));
            Assert.That(editor.UpperDeg(1), Is.EqualTo(UpperDeg).Within(1e-3f));
        }

        [Test]
        public void SetTargetDeg_InsideTheLimits_KeepsTheValue()
        {
            JointTargetEditor editor = Create();

            float applied = editor.SetTargetDeg(0, 45f);

            Assert.That(applied, Is.EqualTo(45f).Within(1e-3f));
            Assert.That(editor.TargetDeg(0), Is.EqualTo(45f).Within(1e-3f));
        }

        [Test]
        public void SetTargetDeg_OutsideTheLimits_IsClamped()
        {
            JointTargetEditor editor = Create();

            Assert.That(editor.SetTargetDeg(0, 500f), Is.EqualTo(UpperDeg).Within(1e-3f));
            Assert.That(editor.SetTargetDeg(0, -500f), Is.EqualTo(LowerDeg).Within(1e-3f));
        }

        [Test]
        public void NudgeDeg_MovesFromTheCurrentTarget_AndStopsAtTheLimit()
        {
            JointTargetEditor editor = Create();
            editor.SetTargetDeg(0, 100f);

            Assert.That(editor.NudgeDeg(0, 5f), Is.EqualTo(105f).Within(1e-3f));
            Assert.That(editor.NudgeDeg(0, 50f), Is.EqualTo(UpperDeg).Within(1e-3f));
        }

        [Test]
        public void Reset_SetsEveryTarget_AndClampsOutOfRangeAngles()
        {
            JointTargetEditor editor = Create();

            editor.Reset(new[] { 30f * Mathf.Deg2Rad, 300f * Mathf.Deg2Rad });

            Assert.That(editor.TargetDeg(0), Is.EqualTo(30f).Within(1e-3f));
            Assert.That(editor.TargetDeg(1), Is.EqualTo(UpperDeg).Within(1e-3f));
        }

        [Test]
        public void CopyTargetsTo_GivesRadians()
        {
            JointTargetEditor editor = Create();
            editor.SetTargetDeg(1, 60f);
            var buffer = new float[2];

            editor.CopyTargetsTo(buffer);

            Assert.That(buffer[1], Is.EqualTo(60f * Mathf.Deg2Rad).Within(1e-5f));
        }

        [Test]
        public void WrongLengths_Throw()
        {
            JointTargetEditor editor = Create();

            Assert.Throws<ArgumentException>(() => editor.Reset(new float[1]));
            Assert.Throws<ArgumentException>(() => editor.CopyTargetsTo(new float[3]));
            Assert.Throws<ArgumentException>(() => new JointTargetEditor(new float[2], new float[3]));
            Assert.Throws<ArgumentNullException>(() => new JointTargetEditor(null, new float[1]));
        }
    }
}
