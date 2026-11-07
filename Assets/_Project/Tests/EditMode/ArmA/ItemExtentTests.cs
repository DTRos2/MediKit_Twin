using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ItemExtentTests
    {
        private GameObject _item;

        [TearDown]
        public void TearDown()
        {
            if (_item != null)
            {
                Object.DestroyImmediate(_item);
            }
        }

        [Test]
        public void ANullItem_HasNoHeight()
        {
            Assert.That(ItemExtent.HalfHeightOf(null), Is.EqualTo(0f));
        }

        [Test]
        public void AnItemWithoutRenderers_HasNoHeight()
        {
            _item = new GameObject("empty");

            Assert.That(ItemExtent.HalfHeightOf(_item), Is.EqualTo(0f));
        }

        [Test]
        public void ThinFlatBox_ReportsHalfOfItsHeight()
        {
            _item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _item.transform.localScale = new Vector3(0.06f, 0.018f, 0.06f);

            Assert.That(ItemExtent.HalfHeightOf(_item), Is.EqualTo(0.009f).Within(1e-5f));
        }

        [Test]
        public void SeveralParts_AreMeasuredTogether()
        {
            _item = new GameObject("kit");
            GameObject low = GameObject.CreatePrimitive(PrimitiveType.Cube);
            low.transform.SetParent(_item.transform);
            low.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
            low.transform.localPosition = Vector3.zero;
            GameObject high = GameObject.CreatePrimitive(PrimitiveType.Cube);
            high.transform.SetParent(_item.transform);
            high.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
            high.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            // 아래 부품 바닥 -0.01부터 위 부품 윗면 0.05까지 0.06m이므로 절반은 0.03.
            Assert.That(ItemExtent.HalfHeightOf(_item), Is.EqualTo(0.03f).Within(1e-5f));
        }

        [Test]
        public void AHiddenItem_IsStillMeasured()
        {
            _item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _item.transform.localScale = new Vector3(0.06f, 0.018f, 0.06f);
            _item.SetActive(false);

            Assert.That(ItemExtent.HalfHeightOf(_item), Is.EqualTo(0.009f).Within(1e-5f));
        }
    }
}
