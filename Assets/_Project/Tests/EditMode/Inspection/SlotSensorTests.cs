using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class SlotSensorTests
    {
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

        private GameObject Make(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            _created.Add(go);
            return go;
        }

        // 박스 루트 아래에 슬롯 z 좌표마다 센서를 만든다. 슬롯 번호는 1부터.
        private SlotSensorArray MakeBox(Vector3 boxPosition, params float[] slotZ)
        {
            GameObject box = Make("Box");
            box.transform.position = boxPosition;
            var array = box.AddComponent<SlotSensorArray>();

            for (int i = 0; i < slotZ.Length; i++)
            {
                GameObject sensorObject = Make($"Slot{i + 1}", box.transform);
                sensorObject.transform.localPosition = new Vector3(0f, 0f, slotZ[i]);
                var volume = sensorObject.AddComponent<BoxCollider>();
                volume.isTrigger = true;
                volume.size = new Vector3(0.1f, 0.1f, 0.05f);
                sensorObject.AddComponent<SlotSensor>().Configure(i + 1);
            }

            return array;
        }

        private void PutItem(string componentId, Vector3 worldPosition, Transform parent = null)
        {
            GameObject item = Make($"Item_{componentId}", parent);
            item.transform.position = worldPosition;
            item.AddComponent<KitComponentTag>().Configure(componentId);
        }

        [Test]
        public void ItemInsideVolume_IsReadAsItsComponentId()
        {
            SlotSensorArray box = MakeBox(Vector3.zero, 0.1f, -0.1f);
            PutItem("gauze", new Vector3(0f, 0f, 0.1f));

            Assert.IsTrue(box.TryRead(2, out string[] ids));

            CollectionAssert.AreEqual(new[] { "gauze", null }, ids);
        }

        [Test]
        public void ItemFollowsBox_WhenBoxMoves()
        {
            SlotSensorArray box = MakeBox(new Vector3(5f, 0f, 0f), 0.1f);
            PutItem("gauze", new Vector3(5f, 0f, 0.1f), box.transform);

            box.transform.position = new Vector3(5f, 0f, 3f);

            Assert.IsTrue(box.TryRead(1, out string[] ids));
            CollectionAssert.AreEqual(new[] { "gauze" }, ids);
        }

        [Test]
        public void ItemOutsideVolume_IsNotRead()
        {
            SlotSensorArray box = MakeBox(Vector3.zero, 0.1f);
            PutItem("gauze", new Vector3(0f, 0f, 0.3f));

            box.TryRead(1, out string[] ids);

            CollectionAssert.AreEqual(new string[] { null }, ids);
        }

        [Test]
        public void TwoItemsInOneSlot_ReadsTheNearestToCenter()
        {
            SlotSensorArray box = MakeBox(Vector3.zero, 0.1f);
            PutItem("far", new Vector3(0f, 0f, 0.12f));
            PutItem("near", new Vector3(0f, 0f, 0.101f));

            box.TryRead(1, out string[] ids);

            Assert.AreEqual("near", ids[0]);
        }

        [Test]
        public void UnresponsiveSensor_FailsTheWholeRead()
        {
            SlotSensorArray box = MakeBox(Vector3.zero, 0.1f, -0.1f);
            box.GetComponentsInChildren<SlotSensor>()[1].IsResponsive = false;

            Assert.IsFalse(box.TryRead(2, out string[] ids));
            Assert.IsNull(ids);
        }

        [Test]
        public void MissingSensorForASlot_FailsTheRead()
        {
            SlotSensorArray box = MakeBox(Vector3.zero, 0.1f);

            Assert.IsFalse(box.TryRead(2, out _));
        }
    }
}
