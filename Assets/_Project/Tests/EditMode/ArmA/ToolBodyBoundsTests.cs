using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ToolBodyBoundsTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        private static readonly Pose Identity = new Pose(Vector3.zero, Quaternion.identity);

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private BoxCollider Box(Vector3 position, Vector3 size, Quaternion? rotation = null, bool trigger = false)
        {
            var go = new GameObject("box");
            _created.Add(go);
            go.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = trigger;
            return box;
        }

        [Test]
        public void ASingleBox_IsMeasuredAsItself()
        {
            BoxCollider box = Box(new Vector3(1f, 2f, 3f), new Vector3(0.2f, 0.4f, 0.6f));

            bool measured = ToolBodyBounds.TryMeasure(new[] { box }, Identity, out Bounds bounds);

            Assert.That(measured, Is.True);
            Assert.That(Vector3.Distance(bounds.center, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(bounds.size, new Vector3(0.2f, 0.4f, 0.6f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void SeveralBoxes_AreEnclosedTogether()
        {
            BoxCollider a = Box(new Vector3(-1f, 0f, 0f), Vector3.one * 0.2f);
            BoxCollider b = Box(new Vector3(1f, 0f, 0f), Vector3.one * 0.2f);

            ToolBodyBounds.TryMeasure(new[] { a, b }, Identity, out Bounds bounds);

            Assert.That(bounds.min.x, Is.EqualTo(-1.1f).Within(1e-4f));
            Assert.That(bounds.max.x, Is.EqualTo(1.1f).Within(1e-4f));
        }

        [Test]
        public void TheResultIsInTheFramesAxes_NotTheWorldsAxes()
        {
            // 기준 자세가 +Y축으로 90도 돌아 있으면, 월드 +X로 긴 상자는 기준 좌표에서 Z로 길다.
            BoxCollider box = Box(Vector3.zero, new Vector3(1f, 0.2f, 0.2f));
            var frame = new Pose(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));

            ToolBodyBounds.TryMeasure(new[] { box }, frame, out Bounds bounds);

            Assert.That(bounds.size.z, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(bounds.size.x, Is.EqualTo(0.2f).Within(1e-4f));
        }

        [Test]
        public void ARotatedBox_IsEnclosedByItsRotatedCorners()
        {
            BoxCollider box = Box(Vector3.zero, new Vector3(1f, 1f, 1f), Quaternion.Euler(0f, 45f, 0f));

            ToolBodyBounds.TryMeasure(new[] { box }, Identity, out Bounds bounds);

            Assert.That(bounds.size.x, Is.EqualTo(Mathf.Sqrt(2f)).Within(1e-4f));
            Assert.That(bounds.size.y, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void TriggersAndMissingColliders_AreSkipped()
        {
            BoxCollider real = Box(Vector3.zero, Vector3.one * 0.2f);
            BoxCollider trigger = Box(new Vector3(5f, 0f, 0f), Vector3.one, trigger: true);

            bool measured = ToolBodyBounds.TryMeasure(new Collider[] { real, trigger, null }, Identity, out Bounds bounds);

            Assert.That(measured, Is.True);
            Assert.That(bounds.max.x, Is.EqualTo(0.1f).Within(1e-4f));
        }

        [Test]
        public void WithNothingToMeasure_ItSaysSo()
        {
            Assert.That(ToolBodyBounds.TryMeasure(new Collider[0], Identity, out _), Is.False);
        }
    }
}
