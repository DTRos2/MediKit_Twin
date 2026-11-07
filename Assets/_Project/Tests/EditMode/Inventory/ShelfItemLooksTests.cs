using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Inventory.Tests
{
    public sealed class ShelfItemLooksTests
    {
        private static ShelfItemLooksSO Create(params ShelfItemLook[] looks)
        {
            var asset = ScriptableObject.CreateInstance<ShelfItemLooksSO>();
            typeof(ShelfItemLooksSO).GetField("_looks", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(asset, new List<ShelfItemLook>(looks));
            return asset;
        }

        private static ShelfItemLook Look(string id, ShelfItemShape shape, float size)
        {
            return new ShelfItemLook(id, shape, new Vector3(size, size, size), new Color(0.1f, 0.2f, 0.3f));
        }

        [Test]
        public void GetLook_KnownComponent_ReturnsItsLookWithoutFallback()
        {
            var asset = Create(Look("gauze", ShelfItemShape.Cube, 0.2f), Look("bandage", ShelfItemShape.Cylinder, 0.12f));

            ShelfItemLook look = asset.GetLook("bandage", out bool isFallback);

            Assert.That(isFallback, Is.False);
            Assert.That(look.Shape, Is.EqualTo(ShelfItemShape.Cylinder));
            Assert.That(look.Size.x, Is.EqualTo(0.12f));
        }

        [Test]
        public void GetLook_UnknownComponent_ReturnsFallbackKeepingTheRequestedId()
        {
            var asset = Create(Look("gauze", ShelfItemShape.Cube, 0.2f));

            ShelfItemLook look = asset.GetLook("new_item", out bool isFallback);

            Assert.That(isFallback, Is.True);
            Assert.That(look.ComponentId, Is.EqualTo("new_item"));
        }

        [Test]
        public void GetLook_DuplicateComponent_UsesTheFirstOne()
        {
            var asset = Create(Look("gauze", ShelfItemShape.Cube, 0.2f), Look("gauze", ShelfItemShape.Cylinder, 0.5f));

            ShelfItemLook look = asset.GetLook("gauze", out _);

            Assert.That(look.Shape, Is.EqualTo(ShelfItemShape.Cube));
        }

        [Test]
        public void GetLook_EmptyAsset_AlwaysFallsBack()
        {
            Assert.That(Create().GetLook("gauze", out bool isFallback).ComponentId, Is.EqualTo("gauze"));
            Assert.That(Create().GetLook("gauze", out isFallback).Size.x, Is.GreaterThan(0f));
        }
    }
}
