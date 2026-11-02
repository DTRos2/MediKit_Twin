using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ColliderLabelTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private Collider ColliderAt(params string[] names)
        {
            Transform parent = null;
            GameObject current = null;
            foreach (string name in names)
            {
                current = new GameObject(name);
                if (parent != null)
                {
                    current.transform.SetParent(parent);
                }
                else
                {
                    _root = current;
                }

                parent = current.transform;
            }

            return current.AddComponent<BoxCollider>();
        }

        [Test]
        public void ARootCollider_IsJustItsName()
        {
            Assert.That(ColliderLabel.Of(ColliderAt("Floor")), Is.EqualTo("Floor"));
        }

        [Test]
        public void AChild_GetsItsParentsNameInFront()
        {
            Assert.That(ColliderLabel.Of(ColliderAt("KitBox", "Wall_NegX")), Is.EqualTo("KitBox/Wall_NegX"));
        }

        [Test]
        public void OnlyTwoAncestorsAreShown()
        {
            Assert.That(ColliderLabel.Of(ColliderAt("Cell", "Conv_Kitting_02", "Model", "Cube")), Is.EqualTo("Conv_Kitting_02/Model/Cube"));
        }
    }
}
