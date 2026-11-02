using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class CollisionIsolationTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                Object.DestroyImmediate(go);
            }

            _created.Clear();
        }

        private Collider NewCollider(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<BoxCollider>();
        }

        [Test]
        public void SeparatesTheItemFromEverythingExceptTheKeptColliders()
        {
            Collider item = NewCollider("item");
            Collider finger = NewCollider("finger");
            Collider shelf = NewCollider("shelf");
            Collider box = NewCollider("box");

            int pairs = CollisionIsolation.Isolate(
                new[] { item }, new[] { item, finger, shelf, box }, new HashSet<Collider> { finger });

            Assert.That(pairs, Is.EqualTo(2));
            Assert.That(Physics.GetIgnoreCollision(item, shelf), Is.True);
            Assert.That(Physics.GetIgnoreCollision(item, box), Is.True);
            Assert.That(Physics.GetIgnoreCollision(item, finger), Is.False);
        }

        [Test]
        public void NeverPairsAColliderWithItself()
        {
            Collider item = NewCollider("item");

            Assert.That(CollisionIsolation.Isolate(new[] { item }, new[] { item }, null), Is.EqualTo(0));
        }

        [Test]
        public void SkipsDestroyedColliders()
        {
            Collider item = NewCollider("item");
            Collider gone = NewCollider("gone");
            Object.DestroyImmediate(gone.gameObject);

            Assert.That(CollisionIsolation.Isolate(new[] { item }, new[] { item, gone }, null), Is.EqualTo(0));
        }

        [Test]
        public void WithoutKeptColliders_SeparatesFromAll()
        {
            Collider item = NewCollider("item");
            Collider other = NewCollider("other");

            int pairs = CollisionIsolation.Isolate(new[] { item }, new[] { other }, null);

            Assert.That(pairs, Is.EqualTo(1));
            Assert.That(Physics.GetIgnoreCollision(item, other), Is.True);
        }

        [Test]
        public void Restore_MakesAnIsolatedPairCollideAgain()
        {
            Collider item = NewCollider("item");
            Collider boxFloor = NewCollider("floor");
            Collider shelf = NewCollider("shelf");
            CollisionIsolation.Isolate(new[] { item }, new[] { boxFloor, shelf }, null);

            int pairs = CollisionIsolation.Restore(new[] { item }, new[] { boxFloor });

            Assert.That(pairs, Is.EqualTo(1));
            Assert.That(Physics.GetIgnoreCollision(item, boxFloor), Is.False);
            Assert.That(Physics.GetIgnoreCollision(item, shelf), Is.True, "고르지 않은 충돌체는 계속 떨어져 있어야 한다");
        }

        [Test]
        public void Restore_NeverPairsAColliderWithItself_AndSkipsDestroyedOnes()
        {
            Collider item = NewCollider("item");
            Collider gone = NewCollider("gone");
            Object.DestroyImmediate(gone.gameObject);

            Assert.That(CollisionIsolation.Restore(new[] { item }, new[] { item, gone }), Is.EqualTo(0));
        }
    }
}
