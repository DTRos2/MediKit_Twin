using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    /// <summary>
    /// 박스에 놓여 고정된 물건이 박스를 따라 움직일 수 있는 상태인지 본다.
    /// 보간(Interpolate)이 켜진 몸체는 화면 위치를 물리 위치에서 덮어써서, 부모 박스가 컨베이어로 움직여도 물건이 제자리에 남는다.
    /// 실제로 따라가는지는 컨베이어를 타는 PlayMode 시험(KitBoxRideTests)이 보고, 여기서는 그 조건이 되는 설정만 본다.
    /// </summary>
    public sealed class GraspableFreezeTests
    {
        private GameObject _host;
        private Graspable _item;

        [SetUp]
        public void CreateItem()
        {
            _host = new GameObject("Item");
            var body = _host.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            _item = _host.AddComponent<Graspable>();
        }

        [TearDown]
        public void DestroyItem()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Freeze_MakesTheItemKinematic_WithoutInterpolation_SoItFollowsItsParent()
        {
            _item.EnablePhysics(useGravity: true);

            _item.Freeze();

            Assert.That(_item.Body.isKinematic, Is.True);
            Assert.That(_item.Body.interpolation, Is.EqualTo(RigidbodyInterpolation.None), "보간이 켜져 있으면 박스를 따라가지 못하고 제자리에 남는다");
        }

        [Test]
        public void Freeze_DropsContinuousDetection_ThatDoesNotSuitAFixedBody()
        {
            _item.EnablePhysics(useGravity: true);
            Assert.That(_item.Body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));

            _item.Freeze();

            Assert.That(_item.Body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.Discrete));
        }

        [Test]
        public void Freeze_StopsTheItemBeforeFixingIt()
        {
            _item.EnablePhysics(useGravity: true);
            _item.Body.linearVelocity = Vector3.one;
            _item.Body.angularVelocity = Vector3.one;

            _item.Freeze();

            Assert.That(_item.Body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(_item.Body.angularVelocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void EnablePhysics_AfterFreeze_GivesTheItemItsLiveSettingsBack()
        {
            _item.Freeze();

            _item.EnablePhysics(useGravity: true);

            Assert.That(_item.Body.isKinematic, Is.False);
            Assert.That(_item.Body.useGravity, Is.True);
            Assert.That(_item.Body.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
            Assert.That(_item.Body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
        }
    }
}
