using MediKitTwin.Unloader;
using UnityEditor;
using UnityEngine;
using Warcool.WarehouseRobot;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 하역장(UnloadingDock)에 하역 로봇이 박스를 내려놓는 단을 만든다. (FR-12)
    /// 단(DockPlace_Body)은 도크 바닥판을 x, z 전체로 덮고 높이는 로봇 팔의 가장 낮은 칸(Unload_Level_0)에 맞춘다.
    /// 박스는 월드 -x면 가장자리에만 놓인다. 로봇은 단 바깥 -x쪽 바닥에서 +x를 보고 서고, 키트 종류마다 가장자리를 따라 구역(칸 + 로봇 자리 +
    /// 감지 구역)을 하나씩 둔다. 구역 수와 순서는 UnloadingDockStock의 KitCodes를 따른다.
    /// 놓인 박스를 안쪽(+x)으로 미는 일은 실행 중에 UnloadingDockStock이 한다.
    /// 오브젝트는 모두 UnloadingDock 루트 아래에 만든다(바닥판 Dock은 비균일 스케일이라 그 아래에 두면 자식이 찌그러진다).
    /// 여러 번 실행해도 같은 오브젝트를 다시 맞출 뿐 늘어나지 않는다. 씬은 열린 채로 고치며 저장하지 않는다(Ctrl+S는 사용자가 한다).
    /// AGVPath는 건드리지 않는다. 경로 끝을 각 구역의 DockPoint에 맞추는 것은 사용자가 한다.
    /// </summary>
    public static class UnloadingDockSetup
    {
        private const string DockName = "UnloadingDock";
        private const string PlateName = "Dock";
        private const string PathName = "AGVPath";
        private const string BodyName = "DockPlace_Body";
        private const string StockName = "DockPlace_Stock";
        private const string SlotPrefix = "DockPlace_Slot_";
        private const string DockPointPrefix = "DockPlace_DockPoint_";
        private const string ZonePrefix = "DockPlace_Zone_";

        // 이전 버전이 만들던 이름(구역이 하나였고 DockPlace 폴더 아래에 있었다). 실행할 때 지운다.
        private const string LegacyFolderName = "DockPlace";
        private static readonly string[] LegacyNames = { "DockPlace_Slot", "DockPlace_DockPoint", "DockPlace_Zone" };

        // 박스를 내려놓는 칸. 팔 높이가 칸 번호로 정해지므로 가장 낮은 Level 0(Unload_Level_0)을 쓴다.
        private const int UnloadLevelIndex = 0;

        // Measure AGV Pick Reach로 잰 값(로봇 0.63배): Level 0 팔이 박스 원점을 두는 높이(m).
        private const float UnloadSlotHeightY = 0.268f;

        // 박스 원점이 박스 한가운데라서 단 윗면은 칸 높이에서 박스 반 높이(키트박스 0.10m)를 뺀 곳이다.
        private const float BoxHalfHeightM = 0.05f;

        // 박스 중심이 단 가장자리에서 안쪽으로 들어가 있는 거리(m). 컨베이어 끝 받침대와 같이 박스를 올릴 수 있는 단 깊이 0.36의 절반이다.
        // 로봇 중심은 박스 중심에서 팔 도달 거리(0.394)만큼 바깥이라, 단 가장자리에서 0.394 - 0.18 = 0.214 바깥에 선다.
        private const float EdgeInsetM = 0.18f;

        // 구역 사이 간격(m). 로봇 감지 구역(한 변 0.4m)과 로봇 몸(약 0.45m 폭)이 이웃 구역에 걸치지 않는 간격이다.
        private const float ZonePitchM = 0.6f;

        // 로봇이 단을 마주 보는 월드 방향. 단의 -x면에서 +x를 본다.
        private static readonly Vector3 Facing = Vector3.right;

        [MenuItem("Tools/MediKit/Setup Unloading Dock Platform")]
        public static void Setup()
        {
            Transform dock = AgvPlatformSetup.FindInScene(DockName);
            if (dock == null)
            {
                Debug.LogError($"[UnloadingDockSetup] 씬에 '{DockName}' 오브젝트가 없습니다.");
                return;
            }

            // 단(자식)의 콜라이더와 섞이지 않도록 이름으로 바닥판을 찾는다.
            Transform plateTransform = dock.Find(PlateName);
            Collider plate = plateTransform != null ? plateTransform.GetComponent<Collider>() : null;
            if (plate == null)
            {
                Debug.LogError($"[UnloadingDockSetup] '{DockName}' 아래에 콜라이더가 있는 '{PlateName}'(바닥판)이 없어 크기를 알 수 없습니다.");
                return;
            }

            Bounds bounds = plate.bounds;
            float topY = UnloadSlotHeightY - BoxHalfHeightM;
            Quaternion facing = Quaternion.LookRotation(Facing, Vector3.up);

            Undo.SetCurrentGroupName("Setup Unloading Dock Platform");
            int group = Undo.GetCurrentGroup();

            RemoveLegacy();

            Transform body = AgvPlatformSetup.FindInScene(BodyName);
            if (body == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = BodyName;
                Undo.RegisterCreatedObjectUndo(cube, "Create " + BodyName);
                body = cube.transform;
            }

            if (body.parent != dock)
            {
                Undo.SetTransformParent(body, dock, "Move Body");
            }

            RemoveEmptyLegacyFolder();

            Undo.RecordObject(body, "Body transform");
            body.rotation = Quaternion.identity;
            Vector3 parentScale = dock.lossyScale;
            body.localScale = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, parentScale.x),
                topY / Mathf.Max(0.0001f, parentScale.y),
                bounds.size.z / Mathf.Max(0.0001f, parentScale.z));
            body.position = new Vector3(bounds.center.x, topY * 0.5f, bounds.center.z);

            Transform stockRoot = AgvPlatformSetup.EnsureChild(dock, StockName);
            Undo.RecordObject(stockRoot, "Stock root");
            // 밀려난 박스를 담는 부모일 뿐이라 위치는 동작과 무관하지만, 도크에서 멀리 떨어져 보이지 않게 바닥판 중심 바닥에 둔다.
            stockRoot.position = new Vector3(bounds.center.x, 0f, bounds.center.z);
            stockRoot.rotation = Quaternion.identity;
            stockRoot.localScale = Vector3.one;

            ShelfStorage storage = body.GetComponent<ShelfStorage>();
            if (storage == null)
            {
                storage = Undo.AddComponent<ShelfStorage>(body.gameObject);
            }

            UnloadingDockStock stock = body.GetComponent<UnloadingDockStock>();
            if (stock == null)
            {
                stock = Undo.AddComponent<UnloadingDockStock>(body.gameObject);
            }

            int zoneCount = stock.ZoneCount;
            var slots = new Transform[zoneCount];
            var dockPoints = new Transform[zoneCount];
            var zones = new ShelfInteractionZone[zoneCount];
            Vector3 zoneSize = AgvPlatformSetup.ZoneSize;

            for (int i = 0; i < zoneCount; i++)
            {
                float z = bounds.center.z + (i - (zoneCount - 1) * 0.5f) * ZonePitchM;
                var slotWorld = new Vector3(bounds.min.x + EdgeInsetM, UnloadSlotHeightY, z);
                var dockWorld = new Vector3(slotWorld.x, 0f, slotWorld.z) - Facing * AgvPlatformSetup.GripReachM;

                slots[i] = AgvPlatformSetup.EnsureChild(dock, SlotPrefix + i);
                Undo.RecordObject(slots[i], "Slot");
                slots[i].position = slotWorld;
                slots[i].rotation = Quaternion.identity;

                dockPoints[i] = AgvPlatformSetup.EnsureChild(dock, DockPointPrefix + i);
                Undo.RecordObject(dockPoints[i], "DockPoint");
                dockPoints[i].position = dockWorld;
                dockPoints[i].rotation = facing;

                Transform zoneTransform = AgvPlatformSetup.EnsureChild(dock, ZonePrefix + i);
                Undo.RecordObject(zoneTransform, "Zone");
                zoneTransform.position = new Vector3(dockWorld.x, zoneSize.y * 0.5f, dockWorld.z);
                zoneTransform.rotation = Quaternion.identity;

                BoxCollider trigger = zoneTransform.GetComponent<BoxCollider>();
                if (trigger == null)
                {
                    trigger = Undo.AddComponent<BoxCollider>(zoneTransform.gameObject);
                }

                Undo.RecordObject(trigger, "Zone collider");
                trigger.isTrigger = true;
                trigger.center = Vector3.zero;
                trigger.size = new Vector3(
                    zoneSize.x / Mathf.Max(0.0001f, zoneTransform.lossyScale.x),
                    zoneSize.y / Mathf.Max(0.0001f, zoneTransform.lossyScale.y),
                    zoneSize.z / Mathf.Max(0.0001f, zoneTransform.lossyScale.z));

                zones[i] = zoneTransform.GetComponent<ShelfInteractionZone>();
                if (zones[i] == null)
                {
                    zones[i] = Undo.AddComponent<ShelfInteractionZone>(zoneTransform.gameObject);
                }

                Undo.RecordObject(zones[i], "ShelfInteractionZone");
                zones[i].shelf = storage;
                zones[i].columnIndex = i;
                zones[i].accessSide = ShelfInteractionZone.AccessSide.Front;
                zones[i].dockPoint = dockPoints[i];
            }

            // 구역(열)마다 칸 하나. 팔 높이는 칸 번호로 정해지므로 Level 0 하나만 둔다.
            var columns = new ShelfStorage.StorageColumn[zoneCount];
            for (int i = 0; i < zoneCount; i++)
            {
                var levels = new Transform[UnloadLevelIndex + 1];
                levels[UnloadLevelIndex] = slots[i];
                columns[i] = new ShelfStorage.StorageColumn { levels = levels };
            }

            Undo.RecordObject(storage, "ShelfStorage");
            storage.columns = columns;

            var stockData = new SerializedObject(stock);
            stockData.FindProperty("_storage").objectReferenceValue = storage;
            stockData.FindProperty("_stockRoot").objectReferenceValue = stockRoot;
            SerializedProperty zonesProperty = stockData.FindProperty("_zones");
            zonesProperty.arraySize = zoneCount;
            for (int i = 0; i < zoneCount; i++)
            {
                zonesProperty.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
            }

            stockData.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(group);
            EditorUtility.SetDirty(body.gameObject);

            LogResult(body, topY, bounds, zoneCount, slots, dockPoints);
        }

        // 구역이 하나이던 이전 버전의 오브젝트를 지운다.
        private static void RemoveLegacy()
        {
            foreach (string legacyName in LegacyNames)
            {
                Transform legacy = AgvPlatformSetup.FindInScene(legacyName);
                if (legacy != null)
                {
                    Undo.DestroyObjectImmediate(legacy.gameObject);
                }
            }
        }

        // 단을 도크로 옮긴 뒤 비어 있는 이전 DockPlace 폴더를 지운다. 다른 오브젝트가 남아 있으면 지우지 않는다.
        private static void RemoveEmptyLegacyFolder()
        {
            Transform folder = AgvPlatformSetup.FindInScene(LegacyFolderName);
            if (folder != null && folder.childCount == 0)
            {
                Undo.DestroyObjectImmediate(folder.gameObject);
            }
        }

        private static void LogResult(Transform body, float topY, Bounds bounds, int zoneCount, Transform[] slots, Transform[] dockPoints)
        {
            string text =
                $"[UnloadingDockSetup] 하역 단을 만들었습니다 (저장은 직접 하세요).\n" +
                $"  단(Body) 위치 {AgvPlatformSetup.Fmt(body.position)}, 크기 {AgvPlatformSetup.Fmt(body.localScale)}, 윗면 y={topY:F3} (바닥판 x {bounds.min.x:F2}~{bounds.max.x:F2}, z {bounds.min.z:F2}~{bounds.max.z:F2})\n" +
                $"  박스는 -x면 가장자리(x={bounds.min.x:F2})에 놓이고 +x(안쪽)로 밀린다. 로봇은 +x를 보고(yaw 90도) 단 바깥 -x쪽 바닥에 선다.\n";

            for (int i = 0; i < zoneCount; i++)
            {
                text += $"  구역 {i}: Slot {AgvPlatformSetup.Fmt(slots[i].position)}, DockPoint(로봇이 서야 할 자리) {AgvPlatformSetup.Fmt(dockPoints[i].position)}\n";
            }

            Transform path = AgvPlatformSetup.FindInScene(PathName);
            if (path != null && path.childCount > 0)
            {
                Vector3 end = path.GetChild(path.childCount - 1).position;
                text += $"  AGVPath 마지막 웨이포인트 {AgvPlatformSetup.Fmt(end)}. 경로 끝을 구역의 DockPoint 쪽으로 맞추세요(DockPoint와 5cm 안, 방향 5도 안이어야 인정).";
            }

            Debug.Log(text);
        }
    }
}
