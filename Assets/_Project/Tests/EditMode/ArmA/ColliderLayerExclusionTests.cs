using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ColliderLayerExclusionTests
    {
        // 시험에 쓰는 레이어 번호. 프로젝트에 이름이 없어도 번호로 지정할 수 있다.
        private const int ArmLayer = 8;
        private const int ShelfLayer = 9;
        private const int DefaultLayer = 0;

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

        private Collider NewCollider(string name, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            _created.Add(go);
            return go.AddComponent<BoxCollider>();
        }

        [Test]
        public void LayersOf_CombinesTheLayersTheCollidersSitOn()
        {
            Collider fingerA = NewCollider("fingerA", ArmLayer);
            Collider fingerB = NewCollider("fingerB", ArmLayer);
            Collider pad = NewCollider("pad", ShelfLayer);

            Assert.That(ColliderLayerExclusion.LayersOf(new[] { fingerA, fingerB }), Is.EqualTo(1 << ArmLayer));
            Assert.That(ColliderLayerExclusion.LayersOf(new[] { fingerA, pad }), Is.EqualTo((1 << ArmLayer) | (1 << ShelfLayer)));
        }

        [Test]
        public void LayersOf_IgnoresMissingColliders_AndANullList()
        {
            Collider gone = NewCollider("gone", ArmLayer);
            Object.DestroyImmediate(gone.gameObject);

            Assert.That(ColliderLayerExclusion.LayersOf(new Collider[] { null, gone }), Is.EqualTo(ColliderLayerExclusion.NoLayers));
            Assert.That(ColliderLayerExclusion.LayersOf(null), Is.EqualTo(ColliderLayerExclusion.NoLayers));
        }

        [Test]
        public void Exclude_WritesTheMaskToEveryCollider_AndCountsThem()
        {
            Collider first = NewCollider("first", DefaultLayer);
            Collider second = NewCollider("second", DefaultLayer);

            int count = ColliderLayerExclusion.Exclude(new[] { first, second }, 1 << ArmLayer);

            Assert.That(count, Is.EqualTo(2));
            Assert.That((int)first.excludeLayers, Is.EqualTo(1 << ArmLayer));
            Assert.That((int)second.excludeLayers, Is.EqualTo(1 << ArmLayer));
        }

        [Test]
        public void Exclude_ReplacesTheEarlierMask_SoALaterPhaseStartsClean()
        {
            Collider item = NewCollider("item", ShelfLayer);
            ColliderLayerExclusion.Exclude(new[] { item }, ColliderLayerExclusion.AllLayers);

            ColliderLayerExclusion.Exclude(new[] { item }, 1 << ArmLayer);

            Assert.That((int)item.excludeLayers, Is.EqualTo(1 << ArmLayer));
        }

        [Test]
        public void Exclude_NoLayers_ClearsTheExclusion()
        {
            Collider item = NewCollider("item", ShelfLayer);
            ColliderLayerExclusion.Exclude(new[] { item }, ColliderLayerExclusion.AllLayers);

            ColliderLayerExclusion.Exclude(new[] { item }, ColliderLayerExclusion.NoLayers);

            Assert.That((int)item.excludeLayers, Is.EqualTo(0));
        }

        [Test]
        public void Exclude_SkipsMissingColliders_AndANullList()
        {
            Collider item = NewCollider("item", ShelfLayer);
            Collider gone = NewCollider("gone", ShelfLayer);
            Object.DestroyImmediate(gone.gameObject);

            Assert.That(ColliderLayerExclusion.Exclude(new Collider[] { item, null, gone }, 1 << ArmLayer), Is.EqualTo(1));
            Assert.That(ColliderLayerExclusion.Exclude(null, 1 << ArmLayer), Is.EqualTo(0));
        }

        [Test]
        public void ExcludeAllExcept_KeepsOnlyTheNamedLayers()
        {
            Collider item = NewCollider("item", ShelfLayer);

            ColliderLayerExclusion.ExcludeAllExcept(new[] { item }, 1 << ArmLayer);

            int excluded = item.excludeLayers;
            Assert.That((excluded & (1 << ArmLayer)) != 0, Is.False, "남긴 레이어는 제외하지 않는다");
            Assert.That((excluded & (1 << ShelfLayer)) != 0, Is.True, "선반 레이어는 제외한다");
            Assert.That((excluded & (1 << DefaultLayer)) != 0, Is.True, "기본 레이어(박스)는 제외한다");
        }

        [Test]
        public void ExcludeAllExcept_NothingKept_ExcludesEveryLayer()
        {
            Collider item = NewCollider("item", ShelfLayer);

            ColliderLayerExclusion.ExcludeAllExcept(new[] { item }, ColliderLayerExclusion.NoLayers);

            Assert.That((int)item.excludeLayers, Is.EqualTo(ColliderLayerExclusion.AllLayers));
        }
    }
}
