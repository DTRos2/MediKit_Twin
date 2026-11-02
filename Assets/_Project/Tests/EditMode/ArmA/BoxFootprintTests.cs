using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BoxFootprintTests
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

        private Transform NewBox()
        {
            var box = new GameObject("KitBox");
            _created.Add(box);
            return box.transform;
        }

        private static BoxCollider AddPart(Transform box, string name, Vector3 center, Vector3 size, bool isTrigger = false)
        {
            var part = new GameObject(name);
            part.transform.SetParent(box, false);
            var collider = part.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            collider.isTrigger = isTrigger;
            return collider;
        }

        [Test]
        public void ThePointsInsideTheFloorAndWallsAreInsideTheBox()
        {
            Transform box = NewBox();
            AddPart(box, "Floor", new Vector3(0f, -0.045f, 0f), new Vector3(0.44f, 0.01f, 0.34f));
            AddPart(box, "Wall", new Vector3(0.215f, 0f, 0f), new Vector3(0.01f, 0.1f, 0.34f));

            Assert.That(BoxFootprint.Contains(box, new Vector3(0f, 0f, 0f)), Is.True);
        }

        [Test]
        public void APointThatFellBelowTheFloorIsOutside()
        {
            Transform box = NewBox();
            AddPart(box, "Floor", new Vector3(0f, -0.045f, 0f), new Vector3(0.44f, 0.01f, 0.34f));
            AddPart(box, "Wall", new Vector3(0.215f, 0f, 0f), new Vector3(0.01f, 0.1f, 0.34f));

            Assert.That(BoxFootprint.Contains(box, new Vector3(0f, -0.3f, 0f)), Is.False);
        }

        [Test]
        public void ThePointsAboveTheWallsAreStillInsideBecauseTheBoxIsOpenAtTheTop()
        {
            Transform box = NewBox();
            AddPart(box, "Floor", new Vector3(0f, -0.045f, 0f), new Vector3(0.44f, 0.01f, 0.34f));
            AddPart(box, "Wall", new Vector3(0.215f, -0.025f, 0f), new Vector3(0.01f, 0.05f, 0.34f));

            Assert.That(BoxFootprint.Contains(box, new Vector3(0f, 0.5f, 0f)), Is.True);
        }

        [Test]
        public void APointBesideTheBoxIsOutside()
        {
            Transform box = NewBox();
            AddPart(box, "Floor", new Vector3(0f, -0.045f, 0f), new Vector3(0.44f, 0.01f, 0.34f));

            Assert.That(BoxFootprint.Contains(box, new Vector3(0.6f, -0.04f, 0f)), Is.False);
        }

        [Test]
        public void SensorTriggersDoNotCountAsTheBoxShape()
        {
            Transform box = NewBox();
            AddPart(box, "Floor", new Vector3(0f, -0.045f, 0f), new Vector3(0.44f, 0.01f, 0.34f));
            AddPart(box, "Sensor", new Vector3(5f, 0f, 0f), new Vector3(0.06f, 0.06f, 0.06f), isTrigger: true);

            Assert.That(BoxFootprint.TryGetBounds(box, out Bounds bounds), Is.True);
            Assert.That(bounds.max.x, Is.LessThan(1f), "멀리 있는 트리거가 영역을 넓히면 안 된다");
        }

        [Test]
        public void ItemsAlreadyPlacedDoNotCountAsTheBoxShape()
        {
            Transform box = NewBox();
            AddPart(box, "Floor", new Vector3(0f, -0.045f, 0f), new Vector3(0.44f, 0.01f, 0.34f));

            var item = new GameObject("Item");
            item.transform.SetParent(box, false);
            item.transform.localPosition = new Vector3(5f, 0f, 0f);
            item.AddComponent<BoxCollider>();
            item.AddComponent<Graspable>();

            Assert.That(BoxFootprint.TryGetBounds(box, out Bounds bounds), Is.True);
            Assert.That(bounds.max.x, Is.LessThan(1f), "놓인 물건이 박스 영역을 넓히면 안 된다");
        }

        [Test]
        public void WithoutAnyCollider_NothingIsInside()
        {
            Transform box = NewBox();

            Assert.That(BoxFootprint.TryGetBounds(box, out _), Is.False);
            Assert.That(BoxFootprint.Contains(box, Vector3.zero), Is.False);
        }
    }
}
