using MediKitTwin.Conveyor;
using MediKitTwin.Unloader;
using UnityEditor;
using UnityEngine;
using Warcool.WarehouseRobot;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 컨베이어 끝 받침대에 UnloaderRobot(robot_boxy)이 박스를 집을 수 있는 칸을 붙인다.
    /// 받침대 오브젝트를 Hierarchy에서 선택하고 실행한다. 받침대에 ShelfStorage를 달고, 자식으로 칸(Slot),
    /// 로봇이 설 자리(DockPoint), 로봇이 들어오면 감지하는 구역(PickZone: 트리거 + ShelfInteractionZone)을 만든다.
    /// 받침대에는 박스가 앉았을 때 칸에 등록하는 AgvPickPlatform도 붙인다.
    /// 여러 번 실행해도 같은 오브젝트를 다시 맞출 뿐 늘어나지 않는다. 박스 등록과 이동은 이 도구가 하지 않고 실행 중에 컴포넌트가 한다.
    /// 씬은 열린 채로 고치며 저장하지 않는다(Ctrl+S는 사용자가 한다).
    /// </summary>
    public static class AgvPlatformSetup
    {
        // 받침대는 비균일 스케일이라 그 자식으로 만들면 얹은 박스가 찌그러진다. 받침대와 같은 부모 아래 형제로 만든다.
        private const string SlotName = "AgvPick_Slot";
        private const string DockPointName = "AgvPick_DockPoint";
        private const string ZoneName = "AgvPick_Zone";
        internal const string UnloaderRobotName = "UnloaderRobot";

        // 로봇을 0.63배로 줄인 뒤 Measure AGV Pick Reach로 잰 값. 로봇 중심에서 박스를 잡는 지점까지의 앞쪽 거리(m).
        internal const float GripReachM = 0.394f;

        // 가장 높은 레벨(Level 2)이 벨트 높이(0.91)에 닿는다. 팔 동작은 레벨별 애니메이션이라 칸 번호가 곧 팔 높이이다.
        private const int PickLevelIndex = 2;

        // 박스 원점이 중심이라고 보고 윗면에서 박스 반 높이만큼 올린다(키트박스 높이 0.10m). 원점이 바닥이면 0으로 둔다.
        private const float BoxHalfHeightM = 0.05f;

        // 로봇이 서 있는 자리를 둘러싸는 감지 구역 크기(m).
        internal static readonly Vector3 ZoneSize = new Vector3(0.4f, 0.4f, 0.4f);

        [MenuItem("Tools/MediKit/Setup AGV Pick Platform")]
        public static void Setup()
        {
            GameObject platform = Selection.activeGameObject;
            if (platform == null)
            {
                Debug.LogError("[AgvPlatformSetup] Hierarchy에서 받침대 오브젝트를 먼저 선택하세요.");
                return;
            }

            Transform holder = platform.transform.parent;
            if (holder == null)
            {
                Debug.LogError($"[AgvPlatformSetup] '{platform.name}'가 최상위 오브젝트입니다. 부모(예: Unloading) 아래에 둔 뒤 실행하세요.");
                return;
            }

            Collider collider = platform.GetComponent<Collider>();
            Renderer renderer = platform.GetComponent<Renderer>();
            Bounds bounds;
            if (collider != null)
            {
                bounds = collider.bounds;
            }
            else if (renderer != null)
            {
                bounds = renderer.bounds;
            }
            else
            {
                Debug.LogError($"[AgvPlatformSetup] '{platform.name}'에 콜라이더나 렌더러가 없어 윗면 높이를 알 수 없습니다.");
                return;
            }

            // 씬에 RobotCargoInteractor가 여러 개 있을 수 있어(다른 robot_boxy), 이름이 UnloaderRobot인 오브젝트 아래의 것만 쓴다.
            RobotCargoInteractor interactor = null;
            int matches = 0;
            foreach (RobotCargoInteractor candidate in Object.FindObjectsByType<RobotCargoInteractor>(FindObjectsInactive.Include))
            {
                for (Transform t = candidate.transform; t != null; t = t.parent)
                {
                    if (t.name == UnloaderRobotName)
                    {
                        interactor = candidate;
                        matches++;
                        break;
                    }
                }
            }

            if (matches != 1)
            {
                Debug.LogError($"[AgvPlatformSetup] 이름이 '{UnloaderRobotName}'인 오브젝트 아래의 RobotCargoInteractor가 {matches}개입니다. 정확히 1개여야 합니다.");
                return;
            }

            // 로봇이 앞으로 향하는 방향(수평)을 정차 자리의 앞쪽으로 삼는다. 구역은 이 방향과 5도 안으로 정렬돼야 인정한다.
            Vector3 robotForward = interactor.transform.forward;
            robotForward.y = 0f;
            robotForward.Normalize();

            // 팔이 뻗는 방향은 Animator가 붙은 오브젝트의 앞방향이다. 인터랙터의 앞방향과 다르면 정차 판정과 팔 방향이 어긋난다.
            Transform armFrame = interactor.animator != null ? interactor.animator.transform : interactor.transform;
            Vector3 armForward = armFrame.forward;
            armForward.y = 0f;
            if (Vector3.Angle(armForward, robotForward) > 5f)
            {
                Debug.LogWarning($"[AgvPlatformSetup] 인터랙터({interactor.name}) 앞방향 {Fmt(robotForward)}와 Animator({armFrame.name}) 앞방향 {Fmt(armForward.normalized)}가 다릅니다. 팔이 뻗는 방향은 후자입니다.");
            }

            Vector3 slotWorld = new Vector3(bounds.center.x, bounds.max.y + BoxHalfHeightM, bounds.center.z);
            Vector3 dockWorld = new Vector3(slotWorld.x, 0f, slotWorld.z) - robotForward * GripReachM;

            Undo.SetCurrentGroupName("Setup AGV Pick Platform");
            int group = Undo.GetCurrentGroup();

            Transform slot = EnsureChild(holder, SlotName);
            Undo.RecordObject(slot, "Slot");
            slot.position = slotWorld;
            slot.rotation = Quaternion.identity;

            Transform dock = EnsureChild(holder, DockPointName);
            Undo.RecordObject(dock, "DockPoint");
            dock.position = dockWorld;
            dock.rotation = Quaternion.LookRotation(robotForward, Vector3.up);

            ShelfStorage storage = platform.GetComponent<ShelfStorage>();
            if (storage == null)
            {
                storage = Undo.AddComponent<ShelfStorage>(platform);
            }

            // 팔 높이는 칸 번호로 정해진다. 번호 앞쪽 칸은 쓰지 않으므로 비워 둔다(null 칸은 박스를 담지 않는다).
            var levels = new Transform[PickLevelIndex + 1];
            levels[PickLevelIndex] = slot;
            Undo.RecordObject(storage, "ShelfStorage");
            storage.columns = new[] { new ShelfStorage.StorageColumn { levels = levels } };

            // 박스가 받침대에 앉으면 칸에 등록하는 컴포넌트. 열은 구역이 보는 열(0)과 같다.
            AgvPickPlatform pickPlatform = platform.GetComponent<AgvPickPlatform>();
            if (pickPlatform == null)
            {
                pickPlatform = Undo.AddComponent<AgvPickPlatform>(platform);
            }

            var pickData = new SerializedObject(pickPlatform);
            pickData.FindProperty("_storage").objectReferenceValue = storage;
            pickData.FindProperty("_column").intValue = 0;
            pickData.FindProperty("_level").intValue = PickLevelIndex;
            pickData.ApplyModifiedProperties();

            Transform zoneTransform = EnsureChild(holder, ZoneName);
            Undo.RecordObject(zoneTransform, "PickZone");
            zoneTransform.position = new Vector3(dockWorld.x, ZoneSize.y * 0.5f, dockWorld.z);
            zoneTransform.rotation = Quaternion.identity;

            BoxCollider trigger = zoneTransform.GetComponent<BoxCollider>();
            if (trigger == null)
            {
                trigger = Undo.AddComponent<BoxCollider>(zoneTransform.gameObject);
            }

            Undo.RecordObject(trigger, "PickZone collider");
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.size = new Vector3(
                ZoneSize.x / Mathf.Max(0.0001f, zoneTransform.lossyScale.x),
                ZoneSize.y / Mathf.Max(0.0001f, zoneTransform.lossyScale.y),
                ZoneSize.z / Mathf.Max(0.0001f, zoneTransform.lossyScale.z));

            ShelfInteractionZone zone = zoneTransform.GetComponent<ShelfInteractionZone>();
            if (zone == null)
            {
                zone = Undo.AddComponent<ShelfInteractionZone>(zoneTransform.gameObject);
            }

            Undo.RecordObject(zone, "ShelfInteractionZone");
            zone.shelf = storage;
            zone.columnIndex = 0;
            zone.accessSide = ShelfInteractionZone.AccessSide.Front;
            zone.dockPoint = dock;

            Undo.CollapseUndoOperations(group);
            EditorUtility.SetDirty(platform);

            Debug.Log(
                $"[AgvPlatformSetup] '{platform.name}'에 칸을 붙였습니다 (저장은 직접 하세요).\n" +
                $"  Slot(박스 원점 위치) {Fmt(slotWorld)}, 칸 번호 {PickLevelIndex} → Load_Level_{PickLevelIndex}\n" +
                $"  DockPoint(로봇이 서야 할 자리) {Fmt(dockWorld)}, 앞방향 {Fmt(robotForward)}\n" +
                $"  PickZone 트리거 크기 {Fmt(ZoneSize)}\n" +
                $"  로봇 현재 위치 {Fmt(interactor.transform.position)}  (DockPoint와 5cm 안, 방향 5도 안이어야 인정)");
        }

        // 받침대 배치(m). Measure AGV Pick Reach·Measure AGV Layout로 잰 값에서 계산했다.
        // 박스 중심이 놓일 지점 x=-0.226(컨베이어 끝 0.0에서 박스 반 길이와 틈을 둔 자리), z=7.0(벨트·로봇 중심선).
        private static readonly Vector3 PlatformCenterXZ = new Vector3(-0.226f, 0f, 7.0f);
        private const float PlatformTopY = 0.85f;            // 박스 중심 0.90(Level 2 팔 높이) - 박스 반 높이 0.05
        private const float PlatformDepthX = 0.36f;
        private const float PlatformWidthZ = 0.26f;
        private const float StopperThicknessX = 0.02f;
        private const float StopperHeightY = 0.02f;
        private const float BoxLengthX = 0.0976f;            // 박스가 컨베이어 진행(x) 방향으로 차지하는 길이
        private const string StopperName = "AgvPick_Stopper";

        /// <summary>
        /// 받침대 크기·위치를 정해진 값으로 맞추고, 정지 턱을 만들고, 칸을 붙이고(Setup), UnloaderRobot을 DockPoint로 옮긴다.
        /// 받침대 오브젝트를 선택하고 실행한다. 여러 번 실행해도 같은 결과이다. 씬은 저장하지 않는다.
        /// </summary>
        [MenuItem("Tools/MediKit/Apply AGV Pick Layout")]
        public static void ApplyLayout()
        {
            GameObject platform = Selection.activeGameObject;
            if (platform == null || platform.transform.parent == null)
            {
                Debug.LogError("[AgvPlatformSetup] Hierarchy에서 부모(Unloading) 아래의 받침대 오브젝트를 먼저 선택하세요.");
                return;
            }

            Transform robotRoot = null;
            int robots = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == UnloaderRobotName)
                {
                    robotRoot = t;
                    robots++;
                }
            }

            if (robots != 1)
            {
                Debug.LogError($"[AgvPlatformSetup] 이름이 '{UnloaderRobotName}'인 오브젝트가 {robots}개입니다. 정확히 1개여야 합니다.");
                return;
            }

            Undo.SetCurrentGroupName("Apply AGV Pick Layout");
            int group = Undo.GetCurrentGroup();

            Transform holder = platform.transform.parent;
            Transform body = platform.transform;
            Undo.RecordObject(body, "Platform transform");
            body.rotation = Quaternion.identity;
            body.localScale = new Vector3(PlatformDepthX, PlatformTopY, PlatformWidthZ);
            body.position = new Vector3(PlatformCenterXZ.x, PlatformTopY * 0.5f, PlatformCenterXZ.z);

            // 박스 앞면(-x쪽)이 닿는 자리: 박스 중심에서 박스 반 길이만큼 -x. 턱은 그 바깥쪽에 붙는다.
            float stopperInnerX = PlatformCenterXZ.x - BoxLengthX * 0.5f;
            Transform stopper = FindInScene(StopperName);
            if (stopper == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = StopperName;
                Undo.RegisterCreatedObjectUndo(cube, "Create " + StopperName);
                cube.transform.SetParent(holder, false);
                stopper = cube.transform;
            }

            Undo.RecordObject(stopper, "Stopper transform");
            stopper.rotation = Quaternion.identity;
            stopper.localScale = new Vector3(StopperThicknessX, StopperHeightY, PlatformWidthZ);
            stopper.position = new Vector3(stopperInnerX - StopperThicknessX * 0.5f, PlatformTopY + StopperHeightY * 0.5f, PlatformCenterXZ.z);

            // 바뀐 위치가 콜라이더 bounds에 바로 반영되게 한다. Setup은 콜라이더 bounds로 윗면 높이를 읽는다.
            Physics.SyncTransforms();
            Setup();

            Transform dock = FindInScene(DockPointName);
            if (dock != null)
            {
                Undo.RecordObject(robotRoot, "Move UnloaderRobot");
                robotRoot.position = new Vector3(dock.position.x, robotRoot.position.y, dock.position.z);
            }

            WireConveyorRunout(FindInScene(SlotName));
            DisableGridSnap(robotRoot);

            Undo.CollapseUndoOperations(group);
            Debug.Log(
                $"[AgvPlatformSetup] 배치를 적용했습니다 (저장은 직접 하세요).\n" +
                $"  받침대 {Fmt(body.position)}, 크기 {Fmt(body.localScale)}\n" +
                $"  정지 턱 {Fmt(stopper.position)}, 크기 {Fmt(stopper.localScale)}\n" +
                $"  UnloaderRobot {Fmt(robotRoot.position)}");
        }

        // GridRobotController는 Start에서 로봇 위치를 gridPointStep(기본 0.25m) 격자에, 방향을 rotationStep(기본 45도)에 맞춘다.
        // 하역 로봇은 정해진 정차 자리에 서야 하므로 Play 시작 때 위치가 밀리지 않게 두 값을 거의 0으로 줄인다(ArmAMobileBaseBuilder와 같은 방식).
        // 위치는 0.5mm, 방향은 0.0005도 안에서만 바뀐다. 키보드 입력은 잠근다.
        private const float EffectivelyNoSnap = 0.001f;

        private static void DisableGridSnap(Transform robotRoot)
        {
            GridRobotController controller = robotRoot.GetComponentInChildren<GridRobotController>(true);
            if (controller == null)
            {
                Debug.LogWarning($"[AgvPlatformSetup] '{robotRoot.name}' 아래에 GridRobotController가 없어 격자 정렬을 끄지 못했습니다.");
                return;
            }

            var data = new SerializedObject(controller);
            data.FindProperty("gridPointStep").floatValue = EffectivelyNoSnap;
            data.FindProperty("rotationStep").floatValue = EffectivelyNoSnap;
            data.FindProperty("controlsLocked").boolValue = true;
            data.ApplyModifiedProperties();
        }

        // 컨베이어 끝 알림(ConveyorEndNotifier)이 있는 오브젝트에 끝 이송(ConveyorEndRunout)을 붙여 칸 위치로 보내도록 연결한다.
        // 알림 컴포넌트가 없으면 연결하지 않는다(컨베이어 끝 설정은 ConveyorHandoffSetup이 한다).
        private static void WireConveyorRunout(Transform slot)
        {
            ConveyorEndNotifier[] notifiers = Object.FindObjectsByType<ConveyorEndNotifier>(FindObjectsInactive.Include);
            if (slot == null || notifiers.Length != 1)
            {
                Debug.LogWarning($"[AgvPlatformSetup] 컨베이어 끝 이송을 연결하지 못했습니다. Slot={(slot != null ? "있음" : "없음")}, ConveyorEndNotifier {notifiers.Length}개(1개여야 함).");
                return;
            }

            ConveyorEndNotifier notifier = notifiers[0];
            ConveyorEndRunout runout = notifier.GetComponent<ConveyorEndRunout>();
            if (runout == null)
            {
                runout = Undo.AddComponent<ConveyorEndRunout>(notifier.gameObject);
            }

            var notifierData = new SerializedObject(notifier);
            var runoutData = new SerializedObject(runout);

            runoutData.FindProperty("_lastConveyor").objectReferenceValue = notifierData.FindProperty("_lastConveyor").objectReferenceValue;
            runoutData.FindProperty("_seatPoint").objectReferenceValue = slot;
            runoutData.ApplyModifiedProperties();

            notifierData.FindProperty("_runout").objectReferenceValue = runout;
            notifierData.ApplyModifiedProperties();
        }

        internal static Transform EnsureChild(Transform parent, string name)
        {
            // 계층을 바꿔 정리해도 같은 오브젝트를 다시 맞추도록, 부모 아래만이 아니라 씬 전체에서 이름으로 찾는다.
            Transform child = FindInScene(name);
            if (child != null)
            {
                return child;
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        internal static Transform FindInScene(string name)
        {
            Transform found = null;
            int count = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == name)
                {
                    found = t;
                    count++;
                }
            }

            if (count > 1)
            {
                Debug.LogWarning($"[AgvPlatformSetup] 이름이 '{name}'인 오브젝트가 {count}개입니다. 마지막으로 찾은 것을 씁니다. 중복을 정리하세요.");
            }

            return found;
        }

        internal static string Fmt(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }
}
