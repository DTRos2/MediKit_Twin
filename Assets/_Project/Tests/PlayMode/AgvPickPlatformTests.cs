#if UNITY_EDITOR
using System.Collections;
using System.Text.RegularExpressions;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using MediKitTwin.Unloader;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Warcool.WarehouseRobot;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 컨베이어 끝에 닿은 박스를 받침대가 칸에 등록하는지 본다. 씬 없이 받침대(콜라이더 + ShelfStorage + AgvPickPlatform)와 박스만 만든다.
    /// 박스는 컨베이어를 타고 온 것처럼 받침(KitBoxCarrier)에 얹어 받침대 칸 위치에 놓고, AtConveyorEnd를 보내 등록을 시작한다.
    /// </summary>
    public sealed class AgvPickPlatformTests
    {
        private const int LevelIndex = 2;
        private const float PlatformTopY = 0.85f;
        private const float BoxHeightM = 0.10f;
        private const float WaitSeconds = 1f;

        private GameObject _platform;
        private Transform _slot;
        private ShelfStorage _storage;
        private KitBoxCarrier _carrier;

        [SetUp]
        public void SetUp()
        {
            _platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _platform.name = "TestPlatform";
            _platform.transform.localScale = new Vector3(0.36f, PlatformTopY, 0.26f);
            _platform.transform.position = new Vector3(0f, PlatformTopY * 0.5f, 0f);

            _slot = new GameObject("TestSlot").transform;
            _slot.position = new Vector3(0f, PlatformTopY + BoxHeightM * 0.5f, 0f);

            // 컴포넌트를 모두 붙이고 값을 정한 뒤에 켜야 ShelfStorage가 칸 배열을 알고 시작한다.
            _platform.SetActive(false);
            _storage = _platform.AddComponent<ShelfStorage>();
            var levels = new Transform[LevelIndex + 1];
            levels[LevelIndex] = _slot;
            _storage.columns = new[] { new ShelfStorage.StorageColumn { levels = levels } };

            var pick = _platform.AddComponent<AgvPickPlatform>();
            var data = new SerializedObject(pick);
            data.FindProperty("_storage").objectReferenceValue = _storage;
            data.FindProperty("_level").intValue = LevelIndex;
            data.ApplyModifiedProperties();
            _platform.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_carrier != null)
            {
                Object.Destroy(_carrier.gameObject);
            }

            Object.Destroy(_platform);
            Object.Destroy(_slot.gameObject);
        }

        [UnityTest]
        public IEnumerator BoxOnThePlatform_IsRegisteredInTheSlot_WhenItReachesTheConveyorEnd()
        {
            var kit = new KitData(1, 1, "LOT-1", "병원", "EM");
            KitBoxCarrier carrier = PlaceBoxOnPlatform(kit);
            Transform box = carrier.Box;

            CellEvents.RaiseAtConveyorEnd(kit);
            yield return new WaitForSeconds(WaitSeconds);
            yield return null;

            CargoBox registered = _storage.GetBox(0, LevelIndex);
            Assert.That(registered, Is.Not.Null, "박스가 칸에 등록되지 않았다");
            Assert.That(registered.transform, Is.SameAs(box), "등록된 것이 받침대에 앉은 박스가 아니다");
            Assert.That(box.parent, Is.SameAs(_slot), "박스가 칸의 자식이 되지 않았다");
            Assert.That(box.GetComponent<KitCargo>().Kit, Is.SameAs(kit), "박스가 어느 키트의 것인지 남지 않았다");
            Assert.That(box.GetComponent<Rigidbody>().isKinematic, Is.True, "박스가 물리로 움직일 수 있다");
            Assert.That(carrier == null, Is.True, "빈 받침이 남았다");
        }

        [UnityTest]
        public IEnumerator Box_IsNotRegistered_BeforeItHasSettled()
        {
            var kit = new KitData(1, 1, "LOT-1", "병원", "EM");
            PlaceBoxOnPlatform(kit);

            CellEvents.RaiseAtConveyorEnd(kit);
            yield return null;

            Assert.That(_storage.GetBox(0, LevelIndex), Is.Null, "정지를 확인하기 전에 등록했다");
        }

        [UnityTest]
        public IEnumerator NothingIsRegistered_AndItWarns_WhenNoBoxIsOnThePlatform()
        {
            LogAssert.Expect(LogType.Warning, new Regex("박스를 찾지 못했다"));

            CellEvents.RaiseAtConveyorEnd(new KitData(1, 1, "LOT-1", "병원", "EM"));
            yield return new WaitForSeconds(WaitSeconds);

            Assert.That(_storage.GetBox(0, LevelIndex), Is.Null);
        }

        private KitBoxCarrier PlaceBoxOnPlatform(KitData kit)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "TestBox";
            box.transform.localScale = new Vector3(0.0976f, BoxHeightM, 0.2387f);
            box.transform.position = _slot.position;
            _carrier = new KitBoxCarrierFactory().Create(box.transform, kit, Quaternion.identity);
            return _carrier;
        }
    }
}
#endif
