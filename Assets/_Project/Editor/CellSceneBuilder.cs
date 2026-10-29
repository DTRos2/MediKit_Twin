using System;
using System.Collections.Generic;
using System.IO;
using ConveyorSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 공정 내부도 기준으로 Scene_Cell 뼈대를 만든다. (Tools > MediKit > Build Cell Scene)
    /// 컨베이어는 서드파티 프리팹을 입출력 지점에 맞춰 ㄷ자로 잇고, 나머지 설비는
    /// 프리미티브 자리표시 프리팹(PF_*)으로 만든다. 이미 있는 PF_* 프리팹은 덮어쓰지 않고 재사용한다.
    /// </summary>
    public static class CellSceneBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Scene_Cell.unity";
        private const string PrefabDir = ProjectRoot + "/Prefabs";
        private const string MaterialDir = ProjectRoot + "/Materials";
        private const string ConveyorPrefabDir = "Assets/Conveyor System/Prefab";
        private const string UnloaderRobotModelPath = "Assets/Warehouse_Robot_Controller/Prefabs/robot_boxy.prefab";

        // 각 구간의 직선 컨베이어 개수 (1개 = 1m)
        private const int KittingLength = 4;
        private const int InspectionLength = 6;
        private const int PackingLength = 4;

        // 공정 내부도 범례 색: 주황 = 최치언 담당, 파랑 = 도희정 담당
        private static readonly Color ColorOrange = new Color(0.95f, 0.45f, 0.10f);
        private static readonly Color ColorBlue = new Color(0.25f, 0.50f, 0.95f);
        private static readonly Color ColorRed = new Color(0.85f, 0.25f, 0.25f);
        private static readonly Color ColorDark = new Color(0.10f, 0.12f, 0.18f);
        private static readonly Color ColorGray = new Color(0.70f, 0.72f, 0.76f);
        private static readonly Color ColorYellow = new Color(0.98f, 0.88f, 0.45f);
        private static readonly Color ColorFloor = new Color(0.90f, 0.92f, 0.95f);

        // 속이 빈 상자의 벽 두께와 불량 박스의 바깥 크기(x 0.98 · y 0.45 · z 1.68m)
        private const float WallThickness = 0.04f;
        private static readonly Vector3 BinSize = new Vector3(0.98f, 0.45f, 1.68f);

        // 지그 위 키트 박스의 바깥 크기(x 0.44 · y 0.10 · z 0.34m)와 벽 두께. 지그 판(0.5 x 0.4m)보다 작다.
        private static readonly Vector3 KitBoxSize = new Vector3(0.44f, 0.10f, 0.34f);
        private const float KitBoxWall = 0.01f;
        private static readonly Color ColorCardboard = new Color(0.78f, 0.60f, 0.38f);

        // 뚜껑: 바깥 크기(0.46 x 0.04 x 0.36m), 벽 두께. 값은 씬에서 직접 고친 PF_LidSupplyTable과 같다.
        // 뚜껑 안쪽은 키트 박스보다 사방 LidClearance(0.5cm)씩 넓다. 로봇암 B의 덮기 허용 오차가 이 틈이다.
        // 바깥 크기 = 키트 박스 + 2 x (틈 + 벽 두께) = 0.47 x 0.04 x 0.37m
        private const float LidClearance = 0.005f;
        private static readonly Vector3 LidSize = new Vector3(
            KitBoxSize.x + 2f * (LidClearance + LidWall), LidHeight, KitBoxSize.z + 2f * (LidClearance + LidWall));
        private const float LidHeight = 0.04f;
        private const float LidWall = 0.01f;

        // 뚜껑 박스: 맨 위 뚜껑이 항상 같은 높이(LidPositionY)에 나와 있고, 박스 윗면(림)은 뚜껑 아래쪽보다 LidRimClearance만큼 낮다.
        // 그래야 로봇암 B의 손가락이 벽에 걸리지 않고 뚜껑 테두리를 잡는다. 남은 뚜껑 수와 상관없이 높이가 같도록 구현 때 맞춘다.
        private const float LidPositionY = 0.639f;
        private const float LidRimClearance = 0.02f;
        private const float LidBoxBottomY = 0.004f;
        private const float LidBoxHeight = LidPositionY - LidHeight * 0.5f - LidRimClearance - LidBoxBottomY; // 0.595
        private const float LidBoxCenterY = LidBoxBottomY + LidBoxHeight * 0.5f; // 0.3015
        private static readonly Vector3 LidBoxSize = new Vector3(0.54f, LidBoxHeight, 0.44f);
        private static readonly Vector3 LidPositionInBox = new Vector3(0f, LidPositionY, 0.008f);
        private const float LidBoxWall = 0.01f;
        private const int LidCapacity = 5;

        // 뚜껑은 위로 쌓으므로 한 장씩 LidHeight만큼 올라간다. 맨 위(Lid_01)가 로봇암이 잡는 높이(LidPositionY)이고,
        // 아래 뚜껑은 Lid_02, Lid_03, ... 순서이다. 쌓인 뚜껑은 아래에서 받치는 승강판(LidLift)과 기둥 위에 놓인다.
        private const float LidStackPitch = LidHeight;
        private const float LidLiftThickness = 0.01f;
        private const float LidLiftPostSize = 0.06f;
        private static readonly Color ColorLid = new Color(0.93f, 0.90f, 0.82f);

        // 컨베이어 위 박스가 놓이는 높이. 컨베이어를 깐 뒤에 정해진다.
        private static float beltY;

        private sealed class ConveyorLine
        {
            public readonly List<ConveyorBase> Kitting = new List<ConveyorBase>();
            public readonly List<ConveyorBase> Inspection = new List<ConveyorBase>();
            public readonly List<ConveyorBase> Packing = new List<ConveyorBase>();
        }

        [MenuItem("Tools/MediKit/Build Cell Scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Scene_Cell 다시 만들기",
                    "Scene_Cell.unity가 이미 있습니다. 새로 만들면 씬의 수동 변경이 사라집니다. (PF_* 프리팹은 유지됩니다)",
                    "다시 만들기", "취소"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cell = new GameObject("Cell").transform;
            var zone = new Dictionary<string, Transform>();
            foreach (var name in new[] { "Shelf", "KittingZone", "Conveyor", "Inspection", "Pusher", "Packing", "Unloading", "AGVPath", "Managers" })
            {
                var t = new GameObject(name).transform;
                t.SetParent(cell, false);
                zone[name] = t;
            }

            var line = BuildConveyor(zone["Conveyor"]);
            beltY = line.Kitting[0].CenterPoint.position.y;

            BuildKitting(zone, line);
            BuildInspection(zone, line);
            BuildPacking(zone, line);
            BuildUnloading(zone, line);
            BuildManagers(zone["Managers"]);

            BuildFloor(cell);
            SetupCamera(cell);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[CellSceneBuilder] Scene_Cell 생성 완료: " + ScenePath);
        }

        // 이미 저장된 PF_RejectBin, PF_Shelf의 내용을 새 모양으로 다시 만든다. 씬에 놓인 인스턴스의 위치는 그대로 유지된다.
        [MenuItem("Tools/MediKit/Rebuild RejectBin And Shelf Prefabs")]
        public static void RebuildHollowPrefabs()
        {
            RebuildPrefab("PF_RejectBin", BuildRejectBin);
            RebuildPrefab("PF_Shelf", BuildShelf);
            AssetDatabase.SaveAssets();
            Debug.Log("[CellSceneBuilder] PF_RejectBin, PF_Shelf 재생성 완료");
        }

        [MenuItem("Tools/MediKit/Rebuild KittingJig Prefab")]
        public static void RebuildKittingJigPrefab()
        {
            // 지그 높이는 열려 있는 씬의 컨베이어 높이를 따른다. 컨베이어가 없으면 중단한다.
            var conveyor = UnityEngine.Object.FindFirstObjectByType<ConveyorBase>();
            if (conveyor == null)
            {
                Debug.LogError("[CellSceneBuilder] 열려 있는 씬에 컨베이어가 없습니다. Scene_Cell을 연 뒤 다시 실행하세요.");
                return;
            }

            beltY = conveyor.CenterPoint.position.y;
            RebuildPrefab("PF_KittingJig", BuildJig);
            AssetDatabase.SaveAssets();
            Debug.Log("[CellSceneBuilder] PF_KittingJig 재생성 완료");
        }

        [MenuItem("Tools/MediKit/Rebuild UnloaderRobot Prefab")]
        public static void RebuildUnloaderRobotPrefab()
        {
            RebuildPrefab("PF_UnloaderRobot", BuildUnloaderRobot);
            AssetDatabase.SaveAssets();
            Debug.Log("[CellSceneBuilder] PF_UnloaderRobot 재생성 완료");
        }

        private static void RebuildPrefab(string prefabName, Action<Transform> build)
        {
            string path = $"{PrefabDir}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning("[CellSceneBuilder] 프리팹이 없어 건너뜁니다: " + path);
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

                build(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ───────────────────────── 컨베이어 ─────────────────────────

        private static ConveyorLine BuildConveyor(Transform parent)
        {
            var straight = LoadConveyor("StraightConveyor");
            var corner = PickRightTurnCorner(LoadConveyor("CornerConveyor"), LoadConveyor("CornerConveyorLeft"));

            var line = new ConveyorLine();
            ConveyorBase tail = null;
            Vector3 flow = Vector3.right; // 키팅 구간은 +X 방향으로 흐른다

            for (int i = 0; i < KittingLength; i++)
            {
                tail = Place(straight, $"Conv_Kitting_{i + 1:00}", parent, tail, flow);
                line.Kitting.Add(tail);
            }
            flow = OutFlow(tail);

            tail = Place(corner, "Conv_Corner_1", parent, tail, flow);
            flow = OutFlow(tail);
            for (int i = 0; i < InspectionLength; i++)
            {
                tail = Place(straight, $"Conv_Inspection_{i + 1:00}", parent, tail, flow);
                line.Inspection.Add(tail);
            }
            flow = OutFlow(tail);

            tail = Place(corner, "Conv_Corner_2", parent, tail, flow);
            flow = OutFlow(tail);
            for (int i = 0; i < PackingLength; i++)
            {
                tail = Place(straight, $"Conv_Packing_{i + 1:00}", parent, tail, flow);
                line.Packing.Add(tail);
            }

            return line;
        }

        private static GameObject LoadConveyor(string name)
        {
            string path = $"{ConveyorPrefabDir}/{name}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("컨베이어 프리팹이 없습니다: " + path);
            return prefab;
        }

        // 위에서 볼 때 오른쪽으로 꺾이는 모서리(진행 방향 +X → -Z)를 고른다.
        private static GameObject PickRightTurnCorner(GameObject a, GameObject b)
        {
            return TurnAngle(a.GetComponent<ConveyorBase>()) < 0f ? a : b;
        }

        private static float TurnAngle(ConveyorBase c)
        {
            return Vector3.SignedAngle(InFlow(c), OutFlow(c), Vector3.up);
        }

        private static Vector3 InFlow(ConveyorBase c)
        {
            return Flat(c.CenterPoint.position - c.InputPoint.position);
        }

        private static Vector3 OutFlow(ConveyorBase c)
        {
            return Flat(c.OutputPoints[0].position - c.CenterPoint.position);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }

        // 입력 지점이 앞 컨베이어의 출력 지점에 닿고 진행 방향이 이어지도록 놓는다. 처음 조각은 원점에서 시작한다.
        private static ConveyorBase Place(GameObject prefab, string name, Transform parent, ConveyorBase tail, Vector3 flow)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            var conveyor = go.GetComponent<ConveyorBase>();

            float yaw = Vector3.SignedAngle(InFlow(conveyor), flow, Vector3.up);
            go.transform.Rotate(0f, yaw, 0f, Space.World);

            Vector3 target = tail != null ? tail.OutputPoints[0].position : Vector3.zero;
            Vector3 offset = target - conveyor.InputPoint.position;
            if (tail == null) offset.y = 0f;
            go.transform.position += offset;
            return conveyor;
        }

        // ───────────────────────── 구역별 설비 ─────────────────────────

        private static void BuildKitting(Dictionary<string, Transform> zone, ConveyorLine line)
        {
            Vector3 jigPos = Floor(line.Kitting[1].CenterPoint.position);

            Station("PF_KittingJig", "KittingJig", zone["KittingZone"], jigPos, 0f, BuildJig);
            Station("PF_ArmA", "ArmA", zone["KittingZone"], jigPos + new Vector3(0f, 0f, 1.0f), 180f, t => BuildArm(t, ColorOrange, 1f));
            Station("PF_Shelf", "Shelf", zone["Shelf"], jigPos + new Vector3(0f, 0f, 2.6f), 180f, BuildShelf);
        }

        private static void BuildInspection(Dictionary<string, Transform> zone, ConveyorLine line)
        {
            Vector3 inspectionPos = Floor(line.Inspection[1].CenterPoint.position);
            Station("PF_InspectionStation", "InspectionStation", zone["Inspection"], inspectionPos, 0f, BuildInspectionStation);

            // 푸셔는 U자 안쪽(-X)에서 바깥(+X)의 불량 박스로 민다.
            Vector3 pusherPos = Floor(line.Inspection[3].CenterPoint.position);
            Station("PF_Pusher", "Pusher", zone["Pusher"], pusherPos + new Vector3(-0.9f, 0f, 0f), 0f, BuildPusher);
            Station("PF_RejectBin", "RejectBin", zone["Pusher"], pusherPos + new Vector3(1.1f, 0f, 0f), 0f, BuildRejectBin);
        }

        private static void BuildPacking(Dictionary<string, Transform> zone, ConveyorLine line)
        {
            Vector3 packingPos = Floor(line.Inspection[5].CenterPoint.position);
            Station("PF_ArmB", "ArmB", zone["Packing"], packingPos + new Vector3(-1.1f, 0f, 0f), 270f, t => BuildArm(t, ColorBlue, 0.8f));
            Station("PF_LidSupplyTable", "LidSupplyTable", zone["Packing"], packingPos + new Vector3(-2.0f, 0f, 0.4f), 0f, BuildLidSupplyTable);
            Sensor("Sensor_Packing", zone["Packing"], line.Inspection[5].CenterPoint.position, new Vector3(0.5f, 0.3f, 0.5f));

            Vector3 labelPos = Floor(line.Packing[1].CenterPoint.position);
            Station("PF_LabelStation", "LabelStation", zone["Packing"], labelPos, 0f, BuildLabelStation);
        }

        private static void BuildUnloading(Dictionary<string, Transform> zone, ConveyorLine line)
        {
            ConveyorBase last = line.Packing[line.Packing.Count - 1];
            Vector3 end = last.OutputPoints[0].position;
            Vector3 flow = OutFlow(last);

            Sensor("Sensor_ConveyorEnd", zone["Unloading"], end, new Vector3(0.4f, 0.3f, 0.5f));

            Vector3 robotPos = Floor(end) + flow * 1.2f;
            Station("PF_UnloaderRobot", "UnloaderRobot", zone["Unloading"], robotPos, 0f, BuildUnloaderRobot);

            // AGV는 하역 로봇 옆에서 출발해 U자 바깥쪽을 돌아 하역장으로 간다.
            float agvZ = robotPos.z - 4f;
            float dockX = Floor(line.Inspection[1].CenterPoint.position).x + 5f;
            var waypoints = new[]
            {
                robotPos + new Vector3(0f, 0f, -1.2f),
                new Vector3(robotPos.x, 0f, agvZ),
                new Vector3(dockX, 0f, agvZ),
                new Vector3(dockX + 2f, 0f, agvZ),
            };
            BuildAgvPath(zone["AGVPath"], waypoints);
            Station("PF_UnloadingDock", "UnloadingDock", zone["Unloading"], waypoints[3] + new Vector3(0f, 0f, 1.6f), 0f, BuildUnloadingDock);
        }

        private static void BuildAgvPath(Transform parent, Vector3[] points)
        {
            Material mat = GetMaterial("M_AgvPath", ColorYellow);
            for (int i = 0; i < points.Length; i++)
            {
                var wp = new GameObject($"AGV_WP_{i}").transform;
                wp.SetParent(parent, false);
                wp.position = points[i];
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"AGV_Strip_{i}";
                strip.transform.SetParent(parent, false);
                strip.transform.position = (a + b) * 0.5f + Vector3.up * 0.01f;
                strip.transform.rotation = Quaternion.LookRotation(b - a);
                strip.transform.localScale = new Vector3(0.4f, 0.02f, (b - a).magnitude);
                strip.GetComponent<Renderer>().sharedMaterial = mat;
                UnityEngine.Object.DestroyImmediate(strip.GetComponent<Collider>());
            }
        }

        private static void BuildManagers(Transform parent)
        {
            foreach (var name in new[] { "SimClock", "OrderManager", "Inventory", "ProductionLogger", "Dashboard" })
            {
                new GameObject(name).transform.SetParent(parent, false);
            }
        }

        private static void BuildFloor(Transform cell)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var r in cell.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            bounds.Expand(6f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(cell, false);
            floor.transform.position = new Vector3(bounds.center.x, -0.01f, bounds.center.z);
            floor.transform.localScale = new Vector3(bounds.size.x / 10f, 1f, bounds.size.z / 10f);
            floor.GetComponent<Renderer>().sharedMaterial = GetMaterial("M_Floor", ColorFloor);
        }

        private static void SetupCamera(Transform cell)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var r in cell.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }

            Vector3 center = bounds.center;
            cam.transform.position = center + new Vector3(-9f, 13f, -12f);
            cam.transform.LookAt(center);

            // 마우스 오른쪽 버튼으로 카메라를 조작한다.
            if (cam.GetComponent<MediKitTwin.CameraRig.CellCameraController>() == null)
                cam.gameObject.AddComponent<MediKitTwin.CameraRig.CellCameraController>();
        }

        // ───────────────────────── 설비 모양 (자리표시) ─────────────────────────

        private static void BuildJig(Transform root)
        {
            Prim("JigPlate", root, new Vector3(0f, beltY + 0.02f, 0f), new Vector3(0.5f, 0.03f, 0.4f), ColorBlue, "M_Blue");

            // 지그 위 빈 키트 박스. 레이아웃 확인용이며, 나중에 박스 공급(IBoxSupply)이 지그가 비면 만들어 놓는다.
            // 박스 중심이 지그 중심에 오므로 슬롯 센서의 박스 기준 좌표와 같다.
            float plateTop = beltY + 0.02f + 0.015f;
            OpenBox("KitBox", root, new Vector3(0f, plateTop + KitBoxSize.y * 0.5f, 0f), KitBoxSize, KitBoxWall, ColorCardboard, "M_KitBox", openTop: true, openFront: false);

            // 슬롯 센서 위치는 RecipeSO.SlotLocalPosition과 같다. (박스 기준 로컬 좌표, 지그 회전 0)
            var slots = new[]
            {
                new Vector2(-0.08f, 0.05f), new Vector2(0.08f, 0.05f),
                new Vector2(-0.08f, -0.05f), new Vector2(0.08f, -0.05f),
            };
            for (int i = 0; i < slots.Length; i++)
            {
                Sensor($"Sensor_Slot{i + 1}", root, new Vector3(slots[i].x, beltY + 0.06f, slots[i].y), new Vector3(0.06f, 0.06f, 0.06f));
            }
        }

        private static void BuildArm(Transform root, Color color, float scale)
        {
            Prim("Base", root, new Vector3(0f, 0.15f * scale, 0f), new Vector3(0.4f, 0.3f, 0.4f) * scale, color, color == ColorOrange ? "M_Orange" : "M_Blue");
            Prim("UpperArm", root, new Vector3(0f, 0.75f * scale, 0f), new Vector3(0.12f, 0.9f, 0.12f) * scale, color, color == ColorOrange ? "M_Orange" : "M_Blue");
            Prim("Forearm", root, new Vector3(0f, 1.2f * scale, -0.35f * scale), new Vector3(0.1f, 0.1f, 0.8f) * scale, color, color == ColorOrange ? "M_Orange" : "M_Blue");
        }

        private static void BuildShelf(Transform root)
        {
            const float cellWidth = 0.6f;
            const float cellHeight = 0.5f;
            Vector3 cellSize = new Vector3(0.56f, 0.46f, 0.5f);

            var componentByShelfSlot = LoadComponentsByShelfSlot();

            // 3행(A=EM, B=BL, C=DX) x 4열. 칸 이름은 선반 칸 ID(`A-1` ~ `C-4`)와 같다.
            // 칸은 속이 빈 상자이고, 로봇암이 있는 쪽(로컬 +Z) 면이 뚫려 있다. 칸마다 레시피의 구성품 1개를 놓는다.
            for (int row = 0; row < 3; row++)
            {
                for (int col = 1; col <= 4; col++)
                {
                    string id = $"{(char)('A' + row)}-{col}";
                    Vector3 pos = new Vector3((col - 2.5f) * cellWidth, 0.3f + row * cellHeight, 0f);
                    Transform cell = OpenBox($"Shelf_{id}", root, pos, cellSize, WallThickness, ColorOrange, "M_Orange", openTop: false, openFront: true);

                    if (componentByShelfSlot.TryGetValue(id, out string componentId))
                        AddShelfItem(cell, componentId, -cellSize.y * 0.5f + WallThickness);
                    else
                        Debug.LogWarning($"[CellSceneBuilder] 레시피에 선반 칸 {id}의 구성품이 없어 비워 둡니다.");
                }
            }

            Prim("Base", root, new Vector3(0f, 0.03f, 0f), new Vector3(2.5f, 0.06f, 0.55f), ColorDark, "M_Dark");
        }

        // 레시피(RecipeSO)에서 선반 칸 ID → 구성품 ID를 읽는다. 구성품 이름은 레시피가 정하므로 여기서 따로 적지 않는다.
        private static Dictionary<string, string> LoadComponentsByShelfSlot()
        {
            var result = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets("t:RecipeSO"))
            {
                var recipe = AssetDatabase.LoadAssetAtPath<MediKitTwin.Recipes.RecipeSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe == null) continue;
                foreach (var item in recipe.Items) result[item.ShelfSlotId] = item.ComponentId;
            }
            return result;
        }

        // 구성품 모양(자리표시): 종류, 전체 크기(m, 원기둥은 x=지름·y=높이), 색
        private struct ItemLook
        {
            public PrimitiveType Shape;
            public Vector3 Size;
            public Color Color;

            public ItemLook(PrimitiveType shape, Vector3 size, Color color)
            {
                Shape = shape;
                Size = size;
                Color = color;
            }
        }

        private static readonly Dictionary<string, ItemLook> ItemLooks = new Dictionary<string, ItemLook>
        {
            // A행 EM
            { "gauze", new ItemLook(PrimitiveType.Cube, new Vector3(0.20f, 0.06f, 0.20f), new Color(0.96f, 0.96f, 0.94f)) },
            { "bandage", new ItemLook(PrimitiveType.Cylinder, new Vector3(0.12f, 0.06f, 0.12f), new Color(0.95f, 0.88f, 0.75f)) },
            { "antiseptic", new ItemLook(PrimitiveType.Cylinder, new Vector3(0.08f, 0.20f, 0.08f), new Color(0.55f, 0.30f, 0.15f)) },
            { "gloves", new ItemLook(PrimitiveType.Cube, new Vector3(0.22f, 0.10f, 0.14f), new Color(0.25f, 0.55f, 0.95f)) },
            // B행 BL
            { "blood_tube", new ItemLook(PrimitiveType.Cylinder, new Vector3(0.04f, 0.18f, 0.04f), new Color(0.75f, 0.10f, 0.15f)) },
            { "needle", new ItemLook(PrimitiveType.Cube, new Vector3(0.04f, 0.04f, 0.20f), new Color(0.65f, 0.67f, 0.70f)) },
            { "alcohol_pad", new ItemLook(PrimitiveType.Cube, new Vector3(0.12f, 0.02f, 0.12f), new Color(0.95f, 0.65f, 0.75f)) },
            { "tourniquet", new ItemLook(PrimitiveType.Cylinder, new Vector3(0.14f, 0.04f, 0.14f), new Color(0.15f, 0.65f, 0.60f)) },
            // C행 DX
            { "swab", new ItemLook(PrimitiveType.Cube, new Vector3(0.02f, 0.02f, 0.22f), new Color(0.85f, 0.72f, 0.50f)) },
            { "buffer_vial", new ItemLook(PrimitiveType.Cylinder, new Vector3(0.06f, 0.14f, 0.06f), new Color(0.20f, 0.70f, 0.85f)) },
            { "test_cassette", new ItemLook(PrimitiveType.Cube, new Vector3(0.16f, 0.02f, 0.10f), new Color(0.97f, 0.97f, 0.97f)) },
            { "sample_bag", new ItemLook(PrimitiveType.Cube, new Vector3(0.20f, 0.03f, 0.14f), new Color(0.80f, 0.82f, 0.86f)) },
        };

        // 칸 바닥(floorY, 칸 기준 로컬) 위에 구성품 1개를 칸 중앙에 놓는다. 이름은 Item_<구성품 ID>.
        private static void AddShelfItem(Transform cell, string componentId, float floorY)
        {
            if (!ItemLooks.TryGetValue(componentId, out ItemLook look))
            {
                Debug.LogWarning($"[CellSceneBuilder] 구성품 {componentId}의 모양이 없어 회색 큐브로 대체합니다.");
                look = new ItemLook(PrimitiveType.Cube, new Vector3(0.10f, 0.10f, 0.10f), ColorGray);
            }

            var item = GameObject.CreatePrimitive(look.Shape);
            item.name = $"Item_{componentId}";
            item.transform.SetParent(cell, false);
            item.transform.localPosition = new Vector3(0f, floorY + look.Size.y * 0.5f, 0f);

            // 원기둥 프리미티브는 높이가 scale.y의 2배이다.
            item.transform.localScale = look.Shape == PrimitiveType.Cylinder
                ? new Vector3(look.Size.x, look.Size.y * 0.5f, look.Size.z)
                : look.Size;

            item.GetComponent<Renderer>().sharedMaterial = GetMaterial($"M_Item_{componentId}", look.Color);
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
        }

        private static void BuildInspectionStation(Transform root)
        {
            Prim("PostL", root, new Vector3(-0.6f, 0.6f, 0f), new Vector3(0.08f, 1.2f, 0.08f), ColorDark, "M_Dark");
            Prim("PostR", root, new Vector3(0.6f, 0.6f, 0f), new Vector3(0.08f, 1.2f, 0.08f), ColorDark, "M_Dark");
            Prim("Beam", root, new Vector3(0f, 1.2f, 0f), new Vector3(1.28f, 0.08f, 0.08f), ColorDark, "M_Dark");
            Prim("Camera", root, new Vector3(0f, 1.05f, 0f), new Vector3(0.2f, 0.15f, 0.2f), ColorBlue, "M_Blue");
            Sensor("Sensor_Inspection", root, new Vector3(0f, beltY + 0.15f, 0f), new Vector3(0.5f, 0.3f, 0.5f));
        }

        private static void BuildPusher(Transform root)
        {
            Prim("Body", root, new Vector3(0f, 0.35f, 0f), new Vector3(0.4f, 0.7f, 0.4f), ColorBlue, "M_Blue");
            Prim("PushHead", root, new Vector3(0.35f, beltY + 0.1f, 0f), new Vector3(0.1f, 0.2f, 0.4f), ColorRed, "M_Red");
        }

        private static void BuildRejectBin(Transform root)
        {
            // 위가 뚫린 상자. 키트 박스(약 0.5 x 0.4m)를 바닥에 3 x 2로 놓으면 6개가 들어간다.
            // 벽 높이(0.45m)는 컨베이어 윗면보다 낮아서, 푸셔가 민 박스가 벽 위로 넘어 떨어진다.
            OpenBox("Bin", root, new Vector3(0f, BinSize.y * 0.5f, 0f), BinSize, WallThickness, ColorRed, "M_Red", openTop: true, openFront: false);
        }

        private static void BuildLidSupplyTable(Transform root)
        {
            // 뚜껑 박스: 바닥에 놓인 위가 뚫린 상자. 안에 뚜껑이 LidCapacity(5)장 쌓여 있다.
            // 뚜껑을 집으면 아래에서 올라오는 동작은 나중에 스크립트로 구현한다.
            OpenBox("LidBox", root, new Vector3(0f, LidBoxCenterY, 0f), LidBoxSize, LidBoxWall, ColorCardboard, "M_KitBox", openTop: true, openFront: false);
            BuildLidStack(root);
            root.gameObject.AddComponent<MediKitTwin.ArmB.LidDispenser>();
        }

        // 뚜껑 LidCapacity장을 위로 쌓고, 맨 아래 뚜껑 밑에 승강판과 기둥을 둔다. 이미 있는 Lid_*, LidLift, LidLiftPost는 호출 전에 지운다.
        private static void BuildLidStack(Transform root)
        {
            for (int i = 0; i < LidCapacity; i++)
            {
                Vector3 position = LidPositionInBox - Vector3.up * (LidStackPitch * i);
                BuildLid($"Lid_{i + 1:00}", root, position);
            }

            // 승강판은 맨 아래 뚜껑의 아래쪽에 붙고, 기둥은 박스 바닥에서 승강판까지 이어진다.
            float bottomLidY = LidPositionInBox.y - LidStackPitch * (LidCapacity - 1);
            float liftTopY = bottomLidY - LidHeight * 0.5f;
            float liftCenterY = liftTopY - LidLiftThickness * 0.5f;
            Prim("LidLift", root, new Vector3(LidPositionInBox.x, liftCenterY, LidPositionInBox.z),
                new Vector3(LidSize.x - 2f * LidWall, LidLiftThickness, LidSize.z - 2f * LidWall), ColorGray, "M_Gray");

            float postBottomY = LidBoxBottomY + LidBoxWall;
            float postTopY = liftCenterY - LidLiftThickness * 0.5f;
            float postHeight = postTopY - postBottomY;
            Prim("LidLiftPost", root, new Vector3(LidPositionInBox.x, postBottomY + postHeight * 0.5f, LidPositionInBox.z),
                new Vector3(LidLiftPostSize, postHeight, LidLiftPostSize), ColorGray, "M_Gray");
        }

        // 키트 박스(0.44 x 0.34m)에 씌우는 뚜껑: 윗판과 아래로 내려온 테두리 4면, 아래는 뚫려 있다. 중심은 부모 기준 위치이다.
        private static void BuildLid(string name, Transform parent, Vector3 center)
        {
            var lid = new GameObject(name).transform;
            lid.SetParent(parent, false);
            lid.localPosition = center;

            float t = LidWall;
            Vector3 h = LidSize * 0.5f;
            float skirtHeight = LidHeight - t;
            float skirtY = -LidHeight * 0.5f + skirtHeight * 0.5f;

            Prim("Top", lid, new Vector3(0f, LidHeight * 0.5f - t * 0.5f, 0f), new Vector3(LidSize.x, t, LidSize.z), ColorLid, "M_Lid");
            Prim("Skirt_NegX", lid, new Vector3(-h.x + t * 0.5f, skirtY, 0f), new Vector3(t, skirtHeight, LidSize.z), ColorLid, "M_Lid");
            Prim("Skirt_PosX", lid, new Vector3(h.x - t * 0.5f, skirtY, 0f), new Vector3(t, skirtHeight, LidSize.z), ColorLid, "M_Lid");
            Prim("Skirt_NegZ", lid, new Vector3(0f, skirtY, -h.z + t * 0.5f), new Vector3(LidSize.x, skirtHeight, t), ColorLid, "M_Lid");
            Prim("Skirt_PosZ", lid, new Vector3(0f, skirtY, h.z - t * 0.5f), new Vector3(LidSize.x, skirtHeight, t), ColorLid, "M_Lid");
        }

        private static void BuildLabelStation(Transform root)
        {
            Prim("Post", root, new Vector3(-0.55f, 0.5f, 0f), new Vector3(0.06f, 1.0f, 0.06f), ColorDark, "M_Dark");
            Prim("LabelDisplay", root, new Vector3(-0.55f, 1.05f, 0f), new Vector3(0.3f, 0.2f, 0.03f), ColorYellow, "M_AgvPath");
        }

        private static void BuildUnloaderRobot(Transform root)
        {
            var robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnloaderRobotModelPath);
            if (robotPrefab == null) throw new FileNotFoundException("하역 로봇 프리팹이 없습니다: " + UnloaderRobotModelPath);

            var robot = (GameObject)PrefabUtility.InstantiatePrefab(robotPrefab, root);
            robot.name = "Robot";
            robot.transform.localPosition = Vector3.zero;
            robot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // 컨베이어 끝(+X) 쪽을 본다

            // 키보드 조작용 스크립트는 끈다. 시뮬레이션에서는 하역 스크립트가 움직임을 맡는다.
            // GridRobotController는 잠그면 입력을 읽지 않고 애니메이션만 갱신한다.
            // RobotCargoInteractor는 Update에서 Input.GetKeyDown을 쓰므로 꺼 둔다. (활성 입력 처리 방식이 Input System 전용이면 오류가 난다)
            var controller = robot.GetComponent<Warcool.WarehouseRobot.GridRobotController>();
            if (controller != null) controller.controlsLocked = true;

            var interactor = robot.GetComponent<Warcool.WarehouseRobot.RobotCargoInteractor>();
            if (interactor != null) interactor.enabled = false;
        }

        private static void BuildUnloadingDock(Transform root)
        {
            Prim("Dock", root, new Vector3(0f, 0.01f, 0f), new Vector3(3f, 0.02f, 3f), ColorGray, "M_Gray");
        }

        // ───────────────────────── 헬퍼 ─────────────────────────

        // PF_* 프리팹이 없으면 build로 만들어 저장하고, 있으면 그대로 쓴다. 씬에는 인스턴스만 놓는다.
        private static void Station(string prefabName, string instanceName, Transform parent, Vector3 position, float yaw, Action<Transform> build)
        {
            string path = $"{PrefabDir}/{prefabName}.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                var temp = new GameObject(prefabName);
                build(temp.transform);
                asset = PrefabUtility.SaveAsPrefabAsset(temp, path);
                UnityEngine.Object.DestroyImmediate(temp);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.name = instanceName;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        private static GameObject Prim(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color, string materialName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = GetMaterial(materialName, color);
            return go;
        }

        // 바닥과 벽으로 이루어진 속이 빈 상자. size는 바깥 크기이고 center는 부모 기준 위치이다.
        // openTop이면 윗면이, openFront이면 +Z 쪽 면이 뚫린다.
        private static Transform OpenBox(string name, Transform parent, Vector3 center, Vector3 size, float t, Color color, string materialName, bool openTop, bool openFront)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = center;

            Vector3 h = size * 0.5f;
            Prim("Floor", root, new Vector3(0f, -h.y + t * 0.5f, 0f), new Vector3(size.x, t, size.z), color, materialName);
            if (!openTop)
                Prim("Top", root, new Vector3(0f, h.y - t * 0.5f, 0f), new Vector3(size.x, t, size.z), color, materialName);

            Prim("Wall_NegX", root, new Vector3(-h.x + t * 0.5f, 0f, 0f), new Vector3(t, size.y, size.z), color, materialName);
            Prim("Wall_PosX", root, new Vector3(h.x - t * 0.5f, 0f, 0f), new Vector3(t, size.y, size.z), color, materialName);
            Prim("Wall_NegZ", root, new Vector3(0f, 0f, -h.z + t * 0.5f), new Vector3(size.x, size.y, t), color, materialName);
            if (!openFront)
                Prim("Wall_PosZ", root, new Vector3(0f, 0f, h.z - t * 0.5f), new Vector3(size.x, size.y, t), color, materialName);

            return root;
        }

        private static void Sensor(string name, Transform parent, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // 구역 오브젝트는 원점·회전 0이라 로컬 좌표가 월드 좌표와 같다.
            go.transform.localPosition = position;

            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
        }

        private static Vector3 Floor(Vector3 p)
        {
            return new Vector3(p.x, 0f, p.z);
        }

        private static Material GetMaterial(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureFolders()
        {
            foreach (var dir in new[] { "Scenes", "Prefabs", "Materials", "Models" })
            {
                Directory.CreateDirectory(Path.Combine(ProjectRoot, dir));
            }
            AssetDatabase.Refresh();
        }
    }
}
