using ConveyorSystem;
using MediKitTwin.Common;
using MediKitTwin.Label;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 컨베이어 끝쪽에서 키트 박스 뚜껑에 라벨을 자동으로 붙이는 라벨 설비를 열린 씬(Scene_Cell)에 준비한다. (FR-11)
    /// 1) 라벨 종이 재질(M_Label)을 만든다. 이미 있으면 그대로 둔다.
    /// 2) PF_LabelStation 프리팹 루트에 LabelStation을 붙이고 표시판(LabelDisplay)과 종이 재질을 연결한다. 프리팹을 다시 만들지 않고 컴포넌트만 더한다.
    /// 3) 씬에 LabelStation이 없으면 포장 구역(Packing) 아래 놓는다. 자리는 CellSceneBuilder가 정한 곳(포장 컨베이어 둘째 구간 중심)이고,
    ///    표시판 기둥이 컨베이어 옆에 오도록 컨베이어의 진행 방향에 맞춰 돌린다. 이미 있으면 사람이 옮겼을 수 있으니 자리를 건드리지 않는다.
    /// 여러 번 실행해도 결과가 같다.
    /// </summary>
    public static class LabelStationSetup
    {
        private const string StationName = "LabelStation";
        private const string DisplayName = "LabelDisplay";
        private const string PostName = "Post";
        private const string PackingZoneName = "Packing";
        private const string PaperMaterialName = "M_Label";

        // CellSceneBuilder가 라벨 설비를 두는 컨베이어: 포장 컨베이어의 둘째 구간(line.Packing[1]). 빌더와 같은 자리를 쓰려고 맞춘다.
        private const string PackingConveyorPrefix = "Conv_Packing_";
        private const int LabelConveyorNumber = 2;

        [MenuItem("Tools/MediKit/Setup Label Station")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[LabelStationSetup] 플레이 모드를 끈 뒤 실행하세요.");
                return;
            }

            bool done = TrySetup();
            Debug.Log($"[LabelStationSetup] {(done ? "완료. 씬을 확인한 뒤 저장하세요." : "중단 — 위 오류를 확인하세요.")}");
        }

        /// <summary>배치 모드용: Scene_Cell을 열어 설정하고 저장한다. -executeMethod로 부른다.</summary>
        public static void SetupAndSave()
        {
            EditorSceneManager.OpenScene(ProjectAssetPaths.CellScene, OpenSceneMode.Single);
            bool done = TrySetup();
            if (done)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log($"[LabelStationSetup] {(done ? "설정하고 저장했습니다." : "중단했습니다.")}");
            EditorApplication.Exit(done ? 0 : 1);
        }

        private static bool TrySetup()
        {
            Material paper = EnsurePaperMaterial();
            if (!WirePrefab(paper))
            {
                return false;
            }

            return EnsureSceneInstance();
        }

        private static Material EnsurePaperMaterial()
        {
            string path = $"{ProjectAssetPaths.MaterialsFolder}/{PaperMaterialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = Color.white };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            AssetDatabase.CreateAsset(material, path);
            Debug.Log($"[LabelStationSetup] 라벨 종이 재질을 만들었습니다: {path}");
            return material;
        }

        // 프리팹 루트에 LabelStation을 붙이고 연결한다. 이미 있으면 연결만 맞춘다.
        private static bool WirePrefab(Material paper)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.LabelStationPrefab) == null)
            {
                Debug.LogError($"[LabelStationSetup] 프리팹이 없습니다: {ProjectAssetPaths.LabelStationPrefab} (먼저 Tools/MediKit의 셀 씬 빌더로 만든다)");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(ProjectAssetPaths.LabelStationPrefab);
            try
            {
                Transform display = root.transform.Find(DisplayName);
                if (display == null)
                {
                    Debug.LogError($"[LabelStationSetup] 프리팹에 표시판({DisplayName})이 없습니다.");
                    return false;
                }

                var station = root.GetComponent<LabelStation>();
                if (station == null)
                {
                    station = root.AddComponent<LabelStation>();
                }

                bool wired = SerializedFieldAssigner.SetReference(station, "_display", display)
                             & SerializedFieldAssigner.SetReference(station, "_paperMaterial", paper);
                if (!wired)
                {
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, ProjectAssetPaths.LabelStationPrefab);
                AssetDatabase.SaveAssets();
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool EnsureSceneInstance()
        {
            if (Object.FindFirstObjectByType<LabelStation>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("[LabelStationSetup] 씬에 라벨 설비가 이미 있어 자리는 그대로 둡니다.");
                return true;
            }

            GameObject zone = GameObject.Find(PackingZoneName);
            ConveyorBase conveyor = FindLabelConveyor();
            if (zone == null || conveyor == null)
            {
                Debug.LogError($"[LabelStationSetup] 포장 구역({PackingZoneName}) {(zone != null)}, 컨베이어({PackingConveyorPrefix}{LabelConveyorNumber:00}) {(conveyor != null)} — 둘 다 있어야 한다.");
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.LabelStationPrefab);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, zone.transform);
            instance.name = StationName;

            Vector3 center = conveyor.CenterPoint.position;
            Vector3 flow = conveyor.OutputPoints[0].position - conveyor.InputPoint.position;
            flow.y = 0f;
            Vector3 spot = new Vector3(center.x, 0f, center.z);
            instance.transform.SetPositionAndRotation(spot, FaceAwayFromArmB(instance.transform, spot, flow));
            Undo.RegisterCreatedObjectUndo(instance, "Create LabelStation");
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Debug.Log($"[LabelStationSetup] 라벨 설비를 {conveyor.name} 중심({center})에 놓았습니다. 진행 방향 {flow}.");
            return true;
        }

        // 표시판 기둥이 컨베이어를 사이에 두고 팔 B 반대쪽에 서도록 방향을 정한다.
        // 기둥이 팔 B와 같은 쪽에 있으면 팔 기초에서 10cm 안이라 뚜껑을 향해 뻗는 팔 앞을 막는다.
        private static Quaternion FaceAwayFromArmB(Transform station, Vector3 spot, Vector3 flow)
        {
            Quaternion alongFlow = Quaternion.LookRotation(flow, Vector3.up);
            GameObject armB = GameObject.Find(CellObjectNames.ArmB);
            Transform post = station.Find(PostName);
            if (armB == null || post == null)
            {
                Debug.LogWarning($"[LabelStationSetup] 팔 B({armB != null}) 또는 기둥({post != null})을 찾지 못해 진행 방향에만 맞춰 놓습니다.");
                return alongFlow;
            }

            Vector3 postOffset = alongFlow * post.localPosition;
            Vector3 towardArm = armB.transform.position - spot;
            towardArm.y = 0f;
            postOffset.y = 0f;
            bool postOnArmSide = Vector3.Dot(postOffset, towardArm) > 0f;
            return postOnArmSide ? Quaternion.LookRotation(-flow, Vector3.up) : alongFlow;
        }

        private static ConveyorBase FindLabelConveyor()
        {
            string wanted = $"{PackingConveyorPrefix}{LabelConveyorNumber:00}";
            foreach (ConveyorBase conveyor in Object.FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include))
            {
                if (conveyor.name == wanted && conveyor.OutputPoints != null && conveyor.OutputPoints.Length > 0 && conveyor.OutputPoints[0] != null)
                {
                    return conveyor;
                }
            }

            return null;
        }
    }
}
