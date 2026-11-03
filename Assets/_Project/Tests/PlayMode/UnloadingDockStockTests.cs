#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Unloader;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Warcool.WarehouseRobot;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 하역장 구역(UnloadingDockStock)이 가장자리에 놓인 박스를 안쪽으로 밀어 가장자리 칸을 비우는지 본다.
    /// 씬 없이 칸 3개를 가진 ShelfStorage와 구역 관리 컴포넌트만 만들고, 로봇이 내려놓는 것은 ShelfStorage.PutBox로 대신한다.
    /// 시험이 빠르도록 미는 속도는 크게, 기다림은 짧게 둔다.
    /// </summary>
    public sealed class UnloadingDockStockTests
    {
        private const int ZoneCount = 3;
        private const float EdgeX = 0.18f;
        private const float ZonePitch = 0.6f;
        private const float BoxPitch = 0.14f;
        private const int Capacity = 2;
        private const float WaitSeconds = 0.6f;
        private const float PositionTolerance = 1e-3f;

        private readonly List<GameObject> _created = new List<GameObject>();
        private GameObject _host;
        private ShelfStorage _storage;
        private UnloadingDockStock _stock;
        private Transform _stockRoot;
        private Transform[] _slots;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("TestDockStock");
            _host.SetActive(false);

            _stockRoot = new GameObject("TestStockRoot").transform;
            _created.Add(_stockRoot.gameObject);

            _slots = new Transform[ZoneCount];
            _storage = _host.AddComponent<ShelfStorage>();
            var columns = new ShelfStorage.StorageColumn[ZoneCount];
            for (int i = 0; i < ZoneCount; i++)
            {
                _slots[i] = new GameObject("TestSlot" + i).transform;
                _slots[i].position = new Vector3(EdgeX, 0.268f, (i - 1) * ZonePitch);
                _created.Add(_slots[i].gameObject);
                columns[i] = new ShelfStorage.StorageColumn { levels = new[] { _slots[i] } };
            }

            _storage.columns = columns;

            _stock = _host.AddComponent<UnloadingDockStock>();
            var data = new SerializedObject(_stock);
            data.FindProperty("_storage").objectReferenceValue = _storage;
            data.FindProperty("_stockRoot").objectReferenceValue = _stockRoot;
            data.FindProperty("_pitchM").floatValue = BoxPitch;
            data.FindProperty("_capacityPerZone").intValue = Capacity;
            data.FindProperty("_shiftDelaySeconds").floatValue = 0.05f;
            data.FindProperty("_shiftSpeedMps").floatValue = 10f;
            data.ApplyModifiedProperties();
            _host.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
            Object.Destroy(_host);
        }

        [Test]
        public void ColumnFor_FindsTheZoneOfAKitCode_IgnoringCase()
        {
            Assert.That(_stock.ColumnFor("EM"), Is.EqualTo(0));
            Assert.That(_stock.ColumnFor("bl"), Is.EqualTo(1));
            Assert.That(_stock.ColumnFor("Dx"), Is.EqualTo(2));
        }

        [Test]
        public void ColumnFor_ReturnsMinusOne_ForUnknownOrEmptyCodes()
        {
            Assert.That(_stock.ColumnFor("ZZ"), Is.EqualTo(-1));
            Assert.That(_stock.ColumnFor(""), Is.EqualTo(-1));
            Assert.That(_stock.ColumnFor(null), Is.EqualTo(-1));
        }

        [UnityTest]
        public IEnumerator PlacedBox_MovesInwardByOnePitch_AndFreesTheEdgeSlot()
        {
            CargoBox box = Place(1);

            yield return new WaitForSeconds(WaitSeconds);

            Assert.That(_storage.GetBox(1, 0), Is.Null, "가장자리 칸이 비워지지 않았다");
            Assert.That(box.transform.position.x, Is.EqualTo(EdgeX + BoxPitch).Within(PositionTolerance), "박스가 안쪽으로 한 칸 밀리지 않았다");
            Assert.That(box.transform.parent, Is.SameAs(_stockRoot), "박스가 모아 두는 곳으로 옮겨지지 않았다");
        }

        [UnityTest]
        public IEnumerator NextBox_PushesTheEarlierBoxFurtherIn()
        {
            CargoBox first = Place(1);
            yield return new WaitForSeconds(WaitSeconds);

            CargoBox second = Place(1);
            yield return new WaitForSeconds(WaitSeconds);

            Assert.That(first.transform.position.x, Is.EqualTo(EdgeX + BoxPitch * 2f).Within(PositionTolerance));
            Assert.That(second.transform.position.x, Is.EqualTo(EdgeX + BoxPitch).Within(PositionTolerance));
            Assert.That(_storage.GetBox(1, 0), Is.Null);
        }

        [UnityTest]
        public IEnumerator Zones_AreIndependent()
        {
            CargoBox inZone0 = Place(0);
            yield return new WaitForSeconds(WaitSeconds);

            CargoBox inZone2 = Place(2);
            yield return new WaitForSeconds(WaitSeconds);

            Assert.That(inZone0.transform.position.x, Is.EqualTo(EdgeX + BoxPitch).Within(PositionTolerance));
            Assert.That(inZone2.transform.position.x, Is.EqualTo(EdgeX + BoxPitch).Within(PositionTolerance));
        }

        [UnityTest]
        public IEnumerator WhenAZoneIsFull_TheOldestBoxIsRemoved_AndItWarns()
        {
            string warningCode = null;
            void OnWarning(string code, string message) => warningCode = code;
            CellEvents.WarningRaised += OnWarning;
            try
            {
                CargoBox oldest = Place(1);
                yield return new WaitForSeconds(WaitSeconds);
                Place(1);
                yield return new WaitForSeconds(WaitSeconds);
                Place(1);
                yield return new WaitForSeconds(WaitSeconds);
                yield return null;

                Assert.That(oldest == null, Is.True, "가장 오래된 박스가 치워지지 않았다");
                Assert.That(warningCode, Is.EqualTo(UnloadingDockStock.FullWarningCode));
            }
            finally
            {
                CellEvents.WarningRaised -= OnWarning;
            }
        }

        // 로봇이 칸에 박스를 내려놓는 것을 ShelfStorage.PutBox로 대신한다.
        private CargoBox Place(int column)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(0.0976f, 0.1f, 0.2387f);
            _created.Add(go);
            CargoBox box = go.AddComponent<CargoBox>();
            _storage.PutBox(column, 0, box);
            return box;
        }
    }
}
#endif
