using System;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class SynchronizedJointStepTests
    {
        private static float[] Run(float[] command, float[] goal, float maxStep)
        {
            var steps = new float[command.Length];
            SynchronizedJointStep.Compute(command, goal, maxStep, steps);
            return steps;
        }

        [Test]
        public void TheFarthestJointMovesAtFullSpeed()
        {
            float[] steps = Run(new float[] { 0f, 0f }, new float[] { 1f, 0.2f }, 0.1f);

            Assert.That(steps[0], Is.EqualTo(0.1f).Within(1e-6f));
        }

        [Test]
        public void ANearerJointMovesInProportion_SoAllArriveTogether()
        {
            float[] steps = Run(new float[] { 0f, 0f }, new float[] { 1f, 0.2f }, 0.1f);

            // 0.2 / 1.0 만큼의 비율: 최대 속도의 20%.
            Assert.That(steps[1], Is.EqualTo(0.02f).Within(1e-6f));
        }

        [Test]
        public void TheDirectionDoesNotMatter_OnlyTheDistance()
        {
            float[] steps = Run(new float[] { 0.5f, 0.5f }, new float[] { -0.5f, 0.4f }, 0.1f);

            Assert.That(steps[0], Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(steps[1], Is.EqualTo(0.01f).Within(1e-6f));
        }

        [Test]
        public void WhenTheFarthestJointCanArriveThisStep_EveryJointArrives()
        {
            float[] steps = Run(new float[] { 0f, 0f, 0f }, new float[] { 0.05f, 0.02f, -0.01f }, 0.1f);

            Assert.That(steps[0], Is.EqualTo(0.05f).Within(1e-6f));
            Assert.That(steps[1], Is.EqualTo(0.02f).Within(1e-6f));
            Assert.That(steps[2], Is.EqualTo(0.01f).Within(1e-6f));
        }

        [Test]
        public void WhenEverythingIsAlreadyThere_NothingMoves()
        {
            float[] steps = Run(new float[] { 0.3f, -0.2f }, new float[] { 0.3f, -0.2f }, 0.1f);

            Assert.That(steps, Is.EqualTo(new[] { 0f, 0f }));
        }

        [Test]
        public void ARepeatedStep_MakesAllJointsReachTheGoalOnTheSameStep()
        {
            var command = new[] { 0f, 0f, 0f };
            var goal = new[] { 0.9f, 0.3f, -0.6f };
            var steps = new float[3];

            int count = 0;
            while (count < 1000)
            {
                SynchronizedJointStep.Compute(command, goal, 0.1f, steps);
                bool allArrived = true;
                for (int i = 0; i < command.Length; i++)
                {
                    float direction = Math.Sign(goal[i] - command[i]);
                    command[i] += direction * steps[i];
                    allArrived &= Math.Abs(goal[i] - command[i]) < 1e-5f;
                }

                count++;
                if (allArrived)
                {
                    break;
                }
            }

            Assert.That(count, Is.EqualTo(9), "가장 먼 관절(0.9)이 0.1씩 가므로 9스텝에 모두 도착한다");
        }

        [Test]
        public void MismatchedLengths_Throw()
        {
            Assert.Throws<ArgumentException>(() => SynchronizedJointStep.Compute(new float[2], new float[3], 0.1f, new float[2]));
            Assert.Throws<ArgumentException>(() => SynchronizedJointStep.Compute(new float[2], new float[2], 0.1f, new float[3]));
        }

        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => SynchronizedJointStep.Compute(null, new float[1], 0.1f, new float[1]));
        }
    }
}
