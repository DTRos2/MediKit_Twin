using MediKitTwin.Mobile;
using MediKitTwin.Unloader;
using UnityEditor;
using UnityEngine;
using Warcool.WarehouseRobot;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 하역 로봇(UnloaderRobot)이 박스를 집어 하역장까지 옮기는 시퀀스(AgvUnloadSequence)를 씬에 연결한다. (FR-12)
    /// 집기 컴포넌트(RobotCargoInteractor)가 있는 오브젝트에 이동 컴포넌트(MobileBaseDriver)를 붙이고, UnloaderRobot에 시퀀스를 붙여
    /// 컨베이어 끝 받침대, 그 구역, 하역장, AGVPath를 이름과 종류로 찾아 연결한다. 이동은 로봇이 서야 할 자리(DockPoint)의 위치와 방향으로 하므로
    /// 먼저 Apply AGV Pick Layout과 Setup Unloading Dock Platform을 실행해 두어야 한다.
    /// 여러 번 실행해도 같은 컴포넌트를 다시 연결할 뿐 늘어나지 않는다. 씬은 열린 채로 고치며 저장하지 않는다(Ctrl+S는 사용자가 한다).
    /// AGVPath는 건드리지 않는다.
    /// </summary>
    public static class UnloaderSequenceSetup
    {
        private const string PickZoneName = "AgvPick_Zone";
        private const string PathName = "AGVPath";

        // 시퀀스가 웨이포인트로 읽는 AGVPath 자식의 이름 앞부분(AgvUnloadSequence의 기본값). 레인(AGV_Strip_n)도 자식이라 이름으로 센다.
        private const string WaypointPrefix = "AGV_WP";

        [MenuItem("Tools/MediKit/Setup Unloader Sequence")]
        public static void Setup()
        {
            Transform robotRoot = AgvPlatformSetup.FindInScene(AgvPlatformSetup.UnloaderRobotName);
            if (robotRoot == null)
            {
                Debug.LogError($"[UnloaderSequenceSetup] 씬에 '{AgvPlatformSetup.UnloaderRobotName}' 오브젝트가 없습니다.");
                return;
            }

            RobotCargoInteractor interactor = robotRoot.GetComponentInChildren<RobotCargoInteractor>(true);
            if (interactor == null)
            {
                Debug.LogError($"[UnloaderSequenceSetup] '{robotRoot.name}' 아래에 RobotCargoInteractor가 없습니다.");
                return;
            }

            AgvPickPlatform pickPlatform = FindSingle<AgvPickPlatform>();
            UnloadingDockStock dock = FindSingle<UnloadingDockStock>();
            Transform pickZoneTransform = AgvPlatformSetup.FindInScene(PickZoneName);
            ShelfInteractionZone pickZone = pickZoneTransform != null ? pickZoneTransform.GetComponent<ShelfInteractionZone>() : null;
            Transform path = AgvPlatformSetup.FindInScene(PathName);

            if (pickPlatform == null || dock == null || pickZone == null)
            {
                Debug.LogError(
                    $"[UnloaderSequenceSetup] 연결할 대상이 부족합니다. AgvPickPlatform={(pickPlatform != null ? "있음" : "없음")}, " +
                    $"UnloadingDockStock={(dock != null ? "있음" : "없음")}, {PickZoneName}={(pickZone != null ? "있음" : "없음")}. " +
                    "Apply AGV Pick Layout과 Setup Unloading Dock Platform을 먼저 실행하세요.");
                return;
            }

            Undo.SetCurrentGroupName("Setup Unloader Sequence");
            int group = Undo.GetCurrentGroup();

            // 이동 컴포넌트는 집기 컴포넌트와 같은 오브젝트에 둔다. 정렬 판정(ShelfInteractionZone)이 그 오브젝트의 위치와 방향을 보기 때문이다.
            MobileBaseDriver driver = interactor.GetComponent<MobileBaseDriver>();
            if (driver == null)
            {
                driver = Undo.AddComponent<MobileBaseDriver>(interactor.gameObject);
            }

            AgvUnloadSequence sequence = robotRoot.GetComponent<AgvUnloadSequence>();
            if (sequence == null)
            {
                sequence = Undo.AddComponent<AgvUnloadSequence>(robotRoot.gameObject);
            }

            // 경로 위에서 물체와 겹치는 곳을 피할 때 로봇 몸 크기로 쓰는 박스 콜라이더(구역 감지용 트리거와 같은 것).
            BoxCollider footprint = interactor.GetComponent<BoxCollider>();

            var data = new SerializedObject(sequence);
            data.FindProperty("_footprint").objectReferenceValue = footprint;
            data.FindProperty("_interactor").objectReferenceValue = interactor;
            data.FindProperty("_mobileBase").objectReferenceValue = driver;
            data.FindProperty("_pickPlatform").objectReferenceValue = pickPlatform;
            data.FindProperty("_pickZone").objectReferenceValue = pickZone;
            data.FindProperty("_dock").objectReferenceValue = dock;
            data.FindProperty("_path").objectReferenceValue = path;
            data.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(group);
            EditorUtility.SetDirty(robotRoot.gameObject);

            string footprintText = "없음(경로를 그대로 따른다)";
            if (footprint != null)
            {
                Vector3 scale = footprint.transform.lossyScale;
                footprintText = $"폭 {footprint.size.x * Mathf.Abs(scale.x):F2}m, 길이 {footprint.size.z * Mathf.Abs(scale.z):F2}m, 높이 {footprint.size.y * Mathf.Abs(scale.y):F2}m";
            }

            Debug.Log(
                $"[UnloaderSequenceSetup] 하역 시퀀스를 연결했습니다 (저장은 직접 하세요).\n" +
                $"  이동 컴포넌트(MobileBaseDriver): '{interactor.name}'\n" +
                $"  로봇 몸 크기(경로가 물체와 겹치는지 보는 기준): {footprintText}\n" +
                $"  받침대 {pickPlatform.name}, 받침대 구역 {pickZone.name}, 하역장 {dock.name}\n" +
                $"  AGVPath: {(path != null ? $"웨이포인트 {CountWaypoints(path)}개 (자식 {path.childCount}개 중 이름이 {WaypointPrefix}로 시작하는 것)" : "없음(곧장 이동)")}");
        }

        private static int CountWaypoints(Transform path)
        {
            int count = 0;
            for (int i = 0; i < path.childCount; i++)
            {
                if (path.GetChild(i).name.StartsWith(WaypointPrefix, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static T FindSingle<T>() where T : Object
        {
            T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            if (found.Length != 1)
            {
                Debug.LogWarning($"[UnloaderSequenceSetup] {typeof(T).Name}이(가) {found.Length}개입니다. 정확히 1개여야 합니다.");
                return null;
            }

            return found[0];
        }
    }
}
