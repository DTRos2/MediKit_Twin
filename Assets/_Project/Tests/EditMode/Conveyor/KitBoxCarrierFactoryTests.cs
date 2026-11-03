using ConveyorSystem;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class KitBoxCarrierFactoryTests
    {
        private GameObject _box;
        private KitBoxCarrier _carrier;

        [SetUp]
        public void SetUp()
        {
            // 높이 0.1m 상자를 높이 2m에 둔다. 상자의 아랫면은 1.95m이다.
            _box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _box.name = CellObjectNames.KitBoxKey;
            _box.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 30f, 0f));
            _box.transform.localScale = new Vector3(0.1f, 0.1f, 0.24f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_carrier != null)
            {
                Object.DestroyImmediate(_carrier.gameObject);
            }

            Object.DestroyImmediate(_box);
        }

        [Test]
        public void Create_PutsTheCarrierAtTheBoxBottomCenter_WithTheGivenRotation()
        {
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

            _carrier = new KitBoxCarrierFactory().Create(_box.transform, NewKit(), rotation);

            Assert.That(_carrier.transform.position.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(_carrier.transform.position.y, Is.EqualTo(1.95f).Within(1e-4f));
            Assert.That(_carrier.transform.position.z, Is.EqualTo(3f).Within(1e-4f));
            Assert.That(Quaternion.Angle(_carrier.transform.rotation, rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void Create_KeepsTheBoxWhereItWas_SoItDoesNotJumpWhenLeavingTheJig()
        {
            Vector3 positionBefore = _box.transform.position;
            Quaternion rotationBefore = _box.transform.rotation;

            _carrier = new KitBoxCarrierFactory().Create(_box.transform, NewKit(), Quaternion.Euler(0f, 90f, 0f));

            Assert.That(_box.transform.parent, Is.SameAs(_carrier.transform));
            Assert.That(Vector3.Distance(_box.transform.position, positionBefore), Is.LessThan(1e-4f));
            Assert.That(Quaternion.Angle(_box.transform.rotation, rotationBefore), Is.LessThan(0.01f));
        }

        [Test]
        public void Create_AttachesAParcelAndRemembersTheKit()
        {
            KitData kit = NewKit();

            _carrier = new KitBoxCarrierFactory().Create(_box.transform, kit, Quaternion.identity);

            Assert.That(_carrier.GetComponent<Parcel>(), Is.Not.Null);
            Assert.That(_carrier.Parcel, Is.SameAs(_carrier.GetComponent<Parcel>()));
            Assert.That(_carrier.Kit, Is.SameAs(kit));
            Assert.That(_carrier.Box, Is.SameAs(_box.transform));
        }

        [Test]
        public void Create_NamesTheCarrierWithoutKitBox_SoArmBDoesNotMistakeItForTheBox()
        {
            _carrier = new KitBoxCarrierFactory().Create(_box.transform, NewKit(), Quaternion.identity);

            Assert.That(_carrier.name, Does.Not.Contain(CellObjectNames.KitBoxKey).IgnoreCase);
        }

        private static KitData NewKit()
        {
            return new KitData(1, 1, "LOT-1", "병원", "EM");
        }
    }
}
