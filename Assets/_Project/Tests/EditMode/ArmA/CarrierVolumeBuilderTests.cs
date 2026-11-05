using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class CarrierVolumeBuilderTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private GameObject _carrier;
        private GameObject _arm;
        private Transform _baseLink;

        [SetUp]
        public void SetUp()
        {
            // 차체 루트(ArmA) 아래에 팔(fr5_robot)과 차체(MobileBase, Base)가 나란히 있다. 팔 베이스 링크는 팔 아래, 바닥에서 0.75m 위에 있다.
            _carrier = Make("ArmA", null, new Vector3(3f, 0f, 2f));
            _arm = Make("fr5_robot", _carrier.transform, new Vector3(0f, 0.75f, 0f));
            _baseLink = Make("base_link", _arm.transform, Vector3.zero).transform;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private GameObject Make(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            if (parent == null)
            {
                _created.Add(go);
            }

            return go;
        }

        private BoxCollider AddBody(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var box = Make(name, parent, Vector3.zero).AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            return box;
        }

        private CarrierBody Build() => CarrierVolumeBuilder.Build(_carrier.transform, _baseLink, _arm.transform);

        [Test]
        public void TheBodiesBesideTheArmBecomeVolumes_ButNotTheOnesUnderTheArm()
        {
            AddBody("MobileBase", _carrier.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
            AddBody("Base", _carrier.transform, new Vector3(0f, 0.459f, 0f), new Vector3(0.4f, 0.4f, 0.4f));
            AddBody("shoulder_Link", _arm.transform, Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f));

            CarrierBody body = Build();

            Assert.That(body.Volumes.Count, Is.EqualTo(2));
        }

        [Test]
        public void TheBoxIsExpressedInTheArmBaseLinkFrame_SoItFollowsTheArmBaseWherever()
        {
            _carrier.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            AddBody("MobileBase", _carrier.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f));

            LinkVolume volume = Build().Volumes[0];

            // 차체 상자 중심은 바닥에서 0.25m, 팔 베이스 링크는 0.75m이므로 링크 기준으로 0.5m 아래다. 차체가 어느 쪽을 보든 같아야 한다.
            Assert.That(Vector3.Distance(volume.LocalCenter, new Vector3(0f, -0.5f, 0f)), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(volume.HalfExtents, new Vector3(0.25f, 0.25f, 0.25f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void ATriggerBoxOnTheCarrierIsItsBody()
        {
            AddBody("MobileBase", _carrier.transform, Vector3.zero, Vector3.one).isTrigger = true;

            Assert.That(Build().Volumes.Count, Is.EqualTo(1));
        }

        [Test]
        public void AHeldItemIsNotPartOfTheBody()
        {
            BoxCollider held = AddBody("held", _carrier.transform, Vector3.zero, Vector3.one * 0.05f);
            held.gameObject.AddComponent<Graspable>();

            Assert.That(Build().Volumes.Count, Is.EqualTo(0));
        }

        [Test]
        public void TheBodiesCollidersAreRemembered_SoTheyAreNotTakenForObstacles()
        {
            BoxCollider body = AddBody("MobileBase", _carrier.transform, Vector3.zero, Vector3.one);
            BoxCollider armPart = AddBody("shoulder_Link", _arm.transform, Vector3.zero, Vector3.one);

            CarrierBody carrier = Build();

            Assert.That(carrier.Contains(body), Is.True);
            Assert.That(carrier.Contains(armPart), Is.False);
        }

        [Test]
        public void TheUndersideIsTheLowestWorldHeightOfAnyBody()
        {
            // 차체는 바닥(0)에서 0.5까지, 받침은 0.2에서 0.7까지. 가장 낮은 면은 0이다.
            AddBody("MobileBase", _carrier.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
            AddBody("Base", _carrier.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.5f, 0.4f));

            Assert.That(Build().UndersideY, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void TheUndersideFollowsTheCarrierHeight()
        {
            _carrier.transform.position = new Vector3(3f, 0.1f, 2f);
            AddBody("MobileBase", _carrier.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f));

            Assert.That(Build().UndersideY, Is.EqualTo(0.1f).Within(1e-5f));
        }

        [Test]
        public void WithoutACarrierRoot_ThereIsNoBody_AndNothingCountsAsGround()
        {
            CarrierBody body = CarrierVolumeBuilder.Build(null, _baseLink, _arm.transform);

            Assert.That(body.Volumes.Count, Is.EqualTo(0));
            Assert.That(GroundSurface.IsGround(new Bounds(new Vector3(0f, -5f, 0f), Vector3.one), body.UndersideY, 0.03f), Is.False);
        }
    }
}
