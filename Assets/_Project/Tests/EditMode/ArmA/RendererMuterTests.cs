using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class RendererMuterTests
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

        private GameObject Cube(string name, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            _created.Add(go);
            return go;
        }

        [Test]
        public void Mute_StopsDrawing_AndRestoreDrawsAgain()
        {
            Renderer renderer = Cube("a").GetComponent<Renderer>();
            var muter = new RendererMuter(new[] { renderer });

            muter.Mute();
            Assert.That(renderer.forceRenderingOff, Is.True);
            Assert.That(muter.IsMuted, Is.True);

            muter.Restore();
            Assert.That(renderer.forceRenderingOff, Is.False);
            Assert.That(muter.IsMuted, Is.False);
        }

        [Test]
        public void ARendererAlreadyOff_StaysOff_AfterRestore()
        {
            Renderer renderer = Cube("a").GetComponent<Renderer>();
            renderer.forceRenderingOff = true;
            var muter = new RendererMuter(new[] { renderer });

            muter.Mute();
            muter.Restore();

            Assert.That(renderer.forceRenderingOff, Is.True);
        }

        [Test]
        public void MuteTwice_ThenRestoreOnce_StillRestoresTheOriginalState()
        {
            Renderer renderer = Cube("a").GetComponent<Renderer>();
            var muter = new RendererMuter(new[] { renderer });

            muter.Mute();
            muter.Mute();
            muter.Restore();

            Assert.That(renderer.forceRenderingOff, Is.False);
        }

        [Test]
        public void RestoreWithoutMute_ChangesNothing()
        {
            Renderer renderer = Cube("a").GetComponent<Renderer>();
            renderer.forceRenderingOff = true;

            new RendererMuter(new[] { renderer }).Restore();

            Assert.That(renderer.forceRenderingOff, Is.True);
        }

        [Test]
        public void ADestroyedRenderer_IsSkipped()
        {
            GameObject gone = Cube("gone");
            Renderer alive = Cube("alive").GetComponent<Renderer>();
            var muter = new RendererMuter(new[] { gone.GetComponent<Renderer>(), alive });
            Object.DestroyImmediate(gone);

            Assert.DoesNotThrow(() => muter.Mute());
            Assert.That(alive.forceRenderingOff, Is.True);
            Assert.DoesNotThrow(() => muter.Restore());
            Assert.That(alive.forceRenderingOff, Is.False);
        }

        [Test]
        public void NullAndEmptyInput_MakeAnEmptyMuter()
        {
            Assert.That(new RendererMuter(null).Count, Is.EqualTo(0));
            Assert.That(new RendererMuter(new Renderer[] { null }).Count, Is.EqualTo(0));
        }

        [Test]
        public void OwnBodyRenderers_TakesEveryRendererUnderTheRoot_ExceptTheCarriedItem()
        {
            GameObject root = new GameObject("robot");
            _created.Add(root);
            Cube("link", root.transform);
            GameObject nested = new GameObject("arm");
            nested.transform.SetParent(root.transform, false);
            Cube("hand", nested.transform);
            GameObject carried = Cube("carried", root.transform);
            carried.AddComponent<Graspable>();
            Cube("outside");

            List<Renderer> found = OwnBodyRenderers.Collect(root.transform);

            Assert.That(found.Count, Is.EqualTo(2));
            Assert.That(found.TrueForAll(r => r.name != "carried" && r.name != "outside"), Is.True);
        }

        [Test]
        public void OwnBodyRenderers_WithoutARoot_IsEmpty()
        {
            Assert.That(OwnBodyRenderers.Collect(null), Is.Empty);
        }
    }
}
