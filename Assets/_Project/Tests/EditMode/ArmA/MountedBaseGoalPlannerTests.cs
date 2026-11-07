using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class MountedBaseGoalPlannerTests
    {
        private const float MinHeadingTravel = 0.05f;

        private static MountedBaseGoalPlanner Create() => new MountedBaseGoalPlanner(MinHeadingTravel);

        // 루트가 goal 자리에서 goal 방향으로 섰을 때 팔 베이스의 월드 위치.
        private static Vector3 ArmBaseAt(BaseGoal goal, Vector3 offsetInRoot)
        {
            return goal.Position + Quaternion.Euler(0f, goal.YawDeg, 0f) * offsetInRoot;
        }

        [Test]
        public void ArmBaseLandsExactlyOnTheTarget_ForAnyRootYawAndDirection()
        {
            var offsetInRoot = new Vector3(0.1f, 0f, -0.6f);
            var rootPosition = new Vector3(1f, 0f, 1f);

            foreach (float rootYaw in new[] { 0f, 90f, 200f, -45f })
            {
                Vector3 armBase = rootPosition + Quaternion.Euler(0f, rootYaw, 0f) * offsetInRoot;

                foreach (Vector3 target in new[] { new Vector3(4f, 0f, 1f), new Vector3(-2f, 0f, 3f), new Vector3(1f, 0f, -4f) })
                {
                    BaseGoal goal = Create().Plan(rootPosition, rootYaw, armBase, target);

                    Vector3 landed = ArmBaseAt(goal, offsetInRoot);
                    Assert.That(Vector3.Distance(landed, target), Is.LessThan(1e-3f), $"yaw {rootYaw}, 목표 {target}");
                }
            }
        }

        [Test]
        public void WithNoOffset_GoesStraightToTheTargetFacingIt()
        {
            BaseGoal goal = Create().Plan(Vector3.zero, 0f, Vector3.zero, new Vector3(3f, 0f, 0f));

            Assert.That(goal.Position.x, Is.EqualTo(3f).Within(1e-4f));
            Assert.That(goal.Position.z, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(goal.YawDeg, Is.EqualTo(90f).Within(1e-3f));
        }

        [Test]
        public void WithAnExplicitFacing_ArmBaseStillLandsOnTheTarget_AndTheBaseFacesThatWay()
        {
            var offsetInRoot = new Vector3(0.1f, 0f, -0.6f);
            var rootPosition = new Vector3(1f, 0f, 1f);
            Vector3 armBase = rootPosition + offsetInRoot;
            var target = new Vector3(4f, 0f, 1f);

            foreach (float facing in new[] { 0f, 90f, 180f, -60f })
            {
                BaseGoal goal = Create().Plan(rootPosition, 0f, armBase, target, facing);

                Assert.That(goal.YawDeg, Is.EqualTo(facing));
                Assert.That(Vector3.Distance(ArmBaseAt(goal, offsetInRoot), target), Is.LessThan(1e-3f), $"방향 {facing}");
            }
        }

        [Test]
        public void ForAVeryShortMove_KeepsTheCurrentYaw()
        {
            var offsetInRoot = new Vector3(0f, 0f, -0.6f);
            Vector3 armBase = Quaternion.Euler(0f, 30f, 0f) * offsetInRoot;

            BaseGoal goal = Create().Plan(Vector3.zero, 30f, armBase, armBase + new Vector3(0.01f, 0f, 0f));

            Assert.That(goal.YawDeg, Is.EqualTo(30f));
        }

        [Test]
        public void KeepsTheRootHeight_AndIgnoresTheTargetHeight()
        {
            BaseGoal goal = Create().Plan(new Vector3(0f, 0.2f, 0f), 0f, new Vector3(0f, 0.9f, 0f), new Vector3(2f, 99f, 0f));

            Assert.That(goal.Position.y, Is.EqualTo(0.2f));
        }

        [Test]
        public void Constructor_NegativeDistance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MountedBaseGoalPlanner(-0.1f));
        }
    }
}
