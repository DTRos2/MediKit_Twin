using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BaseVolumeBuilderTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private GameObject Make(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            if (parent == null)
            {
                _created.Add(go);
            }

            return go;
        }

        private static BoxCollider AddBox(GameObject go, Vector3 center, Vector3 size)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            return box;
        }

        // 베이스 링크 아래에 베이스 몸통(충돌체)과 첫 관절 링크(충돌체)가 있는 로봇.
        private (Transform baseLink, ArticulationBody joint) MakeRobot()
        {
            GameObject baseLink = Make("base_link", null, new Vector3(1f, 0.75f, 2f));
            AddBox(Make("base_body", baseLink.transform, Vector3.zero), new Vector3(0f, 0.05f, 0f), new Vector3(0.15f, 0.1f, 0.15f));

            GameObject shoulder = Make("shoulder_Link", baseLink.transform, new Vector3(0f, 0.1f, 0f));
            AddBox(shoulder, Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f));
            ArticulationBody joint = shoulder.AddComponent<ArticulationBody>();

            return (baseLink.transform, joint);
        }

        [Test]
        public void OnlyTheCollidersOfTheBaseItselfBecomeVolumes_NotTheOnesUnderAJointLink()
        {
            (Transform baseLink, ArticulationBody joint) = MakeRobot();

            List<LinkVolume> volumes = BaseVolumeBuilder.Build(baseLink, new[] { joint });

            Assert.That(volumes.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheBoxIsExpressedInTheBaseLinkFrame()
        {
            (Transform baseLink, ArticulationBody joint) = MakeRobot();
            baseLink.rotation = Quaternion.Euler(0f, 90f, 0f);

            LinkVolume volume = BaseVolumeBuilder.Build(baseLink, new[] { joint })[0];

            // 베이스 링크가 어디에 어느 쪽을 보든, 상자는 링크 기준 (0, 0.05, 0)에 반폭 (0.075, 0.05, 0.075)다.
            Assert.That(Vector3.Distance(volume.LocalCenter, new Vector3(0f, 0.05f, 0f)), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(volume.HalfExtents, new Vector3(0.075f, 0.05f, 0.075f)), Is.LessThan(1e-4f));
            Assert.That(volume.LinkIndex, Is.EqualTo(0));
        }

        [Test]
        public void ATriggerColliderIsNotAnObstacleShape()
        {
            (Transform baseLink, ArticulationBody joint) = MakeRobot();
            AddBox(Make("sensor", baseLink, Vector3.zero), Vector3.zero, Vector3.one).isTrigger = true;

            Assert.That(BaseVolumeBuilder.Build(baseLink, new[] { joint }).Count, Is.EqualTo(1));
        }

        [Test]
        public void ABaseWithoutColliders_HasNoVolumes()
        {
            GameObject baseLink = Make("base_link", null, Vector3.zero);

            Assert.That(BaseVolumeBuilder.Build(baseLink.transform, new ArticulationBody[0]).Count, Is.EqualTo(0));
        }
    }
}
