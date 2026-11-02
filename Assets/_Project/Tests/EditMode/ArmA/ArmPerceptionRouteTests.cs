using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmPerceptionRouteTests
    {
        private static readonly Vector3[] ThreeSpots = { new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 3f) };

        [Test]
        public void BeforeItIsAttached_EveryRouteCountsAsClear()
        {
            var perception = new ArmPerception();

            Assert.That(perception.IsRouteClear(new GraspTargets(Quaternion.identity, Vector3.zero, Vector3.one, Vector3.up, null)), Is.True);
            Assert.That(perception.IsClearAlong(Quaternion.identity, ThreeSpots), Is.True);
            Assert.That(perception.DescribeRouteCollision(new GraspTargets(Quaternion.identity, Vector3.zero, Vector3.one, Vector3.up, null)), Is.Empty);
        }

        [Test]
        public void ThreeSpots_AreAskedAsOneRoute_InTheOrderNearGraspRetreat_WithTheDirection()
        {
            GraspTargets asked = default;
            var perception = new ArmPerception();
            perception.Attach(null, pose => true, null, targets =>
            {
                asked = targets;
                return false;
            });
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);

            bool clear = perception.IsClearAlong(rotation, ThreeSpots);

            Assert.That(clear, Is.False);
            Assert.That(asked.Rotation, Is.EqualTo(rotation));
            Assert.That(asked.Near, Is.EqualTo(ThreeSpots[0]));
            Assert.That(asked.Grasp, Is.EqualTo(ThreeSpots[1]));
            Assert.That(asked.Retreat, Is.EqualTo(ThreeSpots[2]));
            Assert.That(asked.Route, Is.Null);
        }

        [Test]
        public void AnyOtherNumberOfSpots_AreAskedOneByOne()
        {
            var asked = new List<Vector3>();
            var perception = new ArmPerception();
            perception.Attach(null, pose =>
            {
                asked.Add(pose.position);
                return true;
            }, null, targets => false);

            bool clear = perception.IsClearAlong(Quaternion.identity, new[] { Vector3.one, Vector3.up });

            Assert.That(clear, Is.True);
            Assert.That(asked, Is.EqualTo(new[] { Vector3.one, Vector3.up }));
        }

        [Test]
        public void ThreeSpots_AreDescribedAsOneRoute_WithTheDirection()
        {
            GraspTargets asked = default;
            var perception = new ArmPerception();
            perception.Attach(null, null, null, null, targets =>
            {
                asked = targets;
                return "link_b ↔ shelf_board";
            });
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);

            string description = perception.DescribeClearAlong(rotation, ThreeSpots);

            Assert.That(description, Is.EqualTo("link_b ↔ shelf_board"));
            Assert.That(asked.Rotation, Is.EqualTo(rotation));
            Assert.That(asked.Near, Is.EqualTo(ThreeSpots[0]));
            Assert.That(asked.Retreat, Is.EqualTo(ThreeSpots[2]));
        }

        [Test]
        public void AnyOtherNumberOfSpots_CannotBeDescribed()
        {
            var perception = new ArmPerception();
            perception.Attach(null, null, null, null, targets => "link_b ↔ shelf_board");

            Assert.That(perception.DescribeClearAlong(Quaternion.identity, new[] { Vector3.one, Vector3.up }), Is.Empty);
        }

        [Test]
        public void BeforeItIsAttached_NothingIsDescribed()
        {
            Assert.That(new ArmPerception().DescribeClearAlong(Quaternion.identity, ThreeSpots), Is.Empty);
        }

        [Test]
        public void TheRouteDescriptionIsPassedThrough()
        {
            var perception = new ArmPerception();
            perception.Attach(null, null, null, null, targets => "link_b ↔ shelf_board");

            Assert.That(perception.DescribeRouteCollision(new GraspTargets(Quaternion.identity, Vector3.zero, Vector3.one, Vector3.up, null)), Does.Contain("shelf_board"));
        }
    }
}
