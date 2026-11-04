using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GraspAttachmentTests
    {
        private const float Tolerance = 1e-4f;

        private GameObject _anchorObject;
        private GameObject _itemObject;

        [TearDown]
        public void TearDown()
        {
            if (_itemObject != null)
            {
                Object.DestroyImmediate(_itemObject);
            }

            if (_anchorObject != null)
            {
                Object.DestroyImmediate(_anchorObject);
            }
        }

        [Test]
        public void CarryingWhileNothingIsHeld_DoesNothing()
        {
            var attachment = new GraspAttachment();

            Assert.DoesNotThrow(() => attachment.Carry(Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one)));
        }

        [Test]
        public void ACarriedItem_MovesByTheSameMotionAsTheArm()
        {
            var attachment = HoldAnItemAt(new Vector3(1f, 0f, 0.2f));
            Matrix4x4 motion = Matrix4x4.TRS(new Vector3(0.5f, 0f, -1f), Quaternion.identity, Vector3.one);

            attachment.Carry(motion);

            Assert.That(Vector3.Distance(_itemObject.transform.position, new Vector3(1.5f, 0f, -0.8f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(attachment.Held.Body.position, new Vector3(1.5f, 0f, -0.8f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void ACarriedItem_TurnsAroundTheHand_WhenTheArmTurns()
        {
            var hand = new Vector3(1f, 0f, 0f);
            var attachment = HoldAnItemAt(new Vector3(1f, 0f, 0.2f));
            Matrix4x4 motion = Common.RigidMotion.Between(hand, Quaternion.identity, hand, Quaternion.Euler(0f, 90f, 0f));

            attachment.Carry(motion);

            Assert.That(Vector3.Distance(_itemObject.transform.position, new Vector3(1.2f, 0f, 0f)), Is.LessThan(Tolerance));
            Assert.That(Quaternion.Angle(_itemObject.transform.rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(Tolerance));
        }

        private GraspAttachment HoldAnItemAt(Vector3 position)
        {
            _anchorObject = new GameObject("Anchor");
            ArticulationBody anchor = _anchorObject.AddComponent<ArticulationBody>();

            _itemObject = new GameObject("Item");
            _itemObject.transform.position = position;
            _itemObject.AddComponent<Rigidbody>();
            Graspable item = _itemObject.AddComponent<Graspable>();

            var attachment = new GraspAttachment();
            attachment.Attach(item, anchor);
            return attachment;
        }
    }
}
