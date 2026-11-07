using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GpuDrivenRenderingOptOutTests
    {
        private const string ComponentTypeName = "UnityEngine.Rendering.DisallowGPUDrivenRendering, Unity.RenderPipelines.GPUDriven.Runtime";

        private readonly List<GameObject> _created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            // 빼는 방법이 되는 패키지 컴포넌트가 없는 환경이면 이 시험은 뜻이 없다.
            if (!GpuDrivenRenderingOptOut.IsSupported)
            {
                Assert.Ignore("GPU Resident Drawer의 Disallow 컴포넌트를 찾을 수 없다.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
            {
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        private static Type ComponentType => Type.GetType(ComponentTypeName);

        private MeshRenderer NewMesh()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _created.Add(cube);
            return cube.GetComponent<MeshRenderer>();
        }

        private static bool HasOptOut(Renderer renderer) => renderer.GetComponent(ComponentType) != null;

        [Test]
        public void Apply_AddsTheDisallowComponent_ToEachMeshRendererObject()
        {
            MeshRenderer first = NewMesh();
            MeshRenderer second = NewMesh();
            var optOut = new GpuDrivenRenderingOptOut(new Renderer[] { first, second });

            optOut.Apply();

            Assert.That(HasOptOut(first), Is.True);
            Assert.That(HasOptOut(second), Is.True);
            Assert.That(optOut.AppliedCount, Is.EqualTo(2));
            Assert.That(optOut.IsApplied, Is.True);
        }

        [Test]
        public void Restore_RemovesOnlyWhatApplyAdded()
        {
            MeshRenderer mesh = NewMesh();
            var optOut = new GpuDrivenRenderingOptOut(new Renderer[] { mesh });
            optOut.Apply();

            optOut.Restore();

            Assert.That(HasOptOut(mesh), Is.False);
            Assert.That(optOut.IsApplied, Is.False);
            Assert.That(optOut.AppliedCount, Is.EqualTo(0));
        }

        [Test]
        public void AComponentThatWasAlreadyThere_IsNotCounted_AndIsLeftAloneOnRestore()
        {
            MeshRenderer mesh = NewMesh();
            mesh.gameObject.AddComponent(ComponentType);
            var optOut = new GpuDrivenRenderingOptOut(new Renderer[] { mesh });

            optOut.Apply();
            optOut.Restore();

            Assert.That(optOut.Count, Is.EqualTo(1));
            Assert.That(HasOptOut(mesh), Is.True, "사용자가 붙여 둔 것은 떼지 않는다");
        }

        [Test]
        public void ApplyingTwice_AddsOnlyOneComponent()
        {
            MeshRenderer mesh = NewMesh();
            var optOut = new GpuDrivenRenderingOptOut(new Renderer[] { mesh });

            optOut.Apply();
            optOut.Apply();

            Assert.That(mesh.GetComponents(ComponentType).Length, Is.EqualTo(1));
            Assert.That(optOut.AppliedCount, Is.EqualTo(1));
        }

        [Test]
        public void RestoreWithoutApply_ChangesNothing()
        {
            MeshRenderer mesh = NewMesh();
            mesh.gameObject.AddComponent(ComponentType);

            new GpuDrivenRenderingOptOut(new Renderer[] { mesh }).Restore();

            Assert.That(HasOptOut(mesh), Is.True);
        }

        [Test]
        public void OnlyMeshRenderers_AreCounted_AndNullsAndOtherRenderersAreSkipped()
        {
            GameObject line = new GameObject("line");
            _created.Add(line);
            LineRenderer lineRenderer = line.AddComponent<LineRenderer>();

            var optOut = new GpuDrivenRenderingOptOut(new Renderer[] { NewMesh(), null, lineRenderer });

            Assert.That(optOut.Count, Is.EqualTo(1));
            Assert.DoesNotThrow(() => optOut.Apply());
            Assert.That(line.GetComponent(ComponentType), Is.Null);
        }

        [Test]
        public void ADestroyedRenderer_IsSkippedWithoutError()
        {
            MeshRenderer mesh = NewMesh();
            var optOut = new GpuDrivenRenderingOptOut(new Renderer[] { mesh });
            UnityEngine.Object.DestroyImmediate(mesh.gameObject);

            Assert.DoesNotThrow(() => optOut.Apply());
            Assert.DoesNotThrow(() => optOut.Restore());
        }

        [Test]
        public void ANullList_MeansNothingToDo()
        {
            var optOut = new GpuDrivenRenderingOptOut(null);

            Assert.That(optOut.Count, Is.EqualTo(0));
            Assert.DoesNotThrow(() => optOut.Apply());
        }
    }
}
