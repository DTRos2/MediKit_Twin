using System.Collections.Generic;
using System.IO;
using System.Text;
using MediKitTwin.ArmA;
using MediKitTwin.Mobile;
using MediKitTwin.Demo;
using UnityEditor;
using UnityEngine;
using Warcool.WarehouseRobot;

// ArmA 프리팹 아래에 하역 로봇(robot_boxy)의 하부(바퀴, 궤도, 기어, 하부 차체)만 붙이고 팔을 그 위로 올린다.
// robot_boxy는 몸통, 바퀴, 궤도가 하나의 스킨 메시라서, 같은 메시에서 윗몸통(상판, 옆 램프, 팔)에 속한 삼각형만 지운
// 새 메시를 만들어 쓴다. 뼈와 가중치는 그대로라 궤도 애니메이션(GridRobotController)이 원본과 똑같이 동작한다.
// 이동은 키 입력이 아니라 MobileBaseDriver(코드 명령)가 맡고, 하역 로봇의 GridRobotController는 잠가서 궤도 애니메이션만 쓴다.
public static class ArmAMobileBaseBuilder
{
    private const string ArmAPrefabPath = "Assets/_Project/Prefabs/ArmA.prefab";
    private const string RobotPrefabPath = "Assets/Warehouse_Robot_Controller/Prefabs/robot_boxy.prefab";
    private const string MeshFolder = "Assets/_Project/Models/ArmABase";
    private const string OldWheelMeshFolder = "Assets/_Project/Models/ArmAWheels";
    private const string BaseName = "MobileBase";
    private const string OldWheelsName = "Wheels";

    // 이 이름을 포함한 뼈에 붙은 삼각형을 하부로 본다. 윗몸통은 bone_body, bone_spine*, bone_arm*, point_* 쪽이라 빠진다.
    // 하부에 빠진 부분이 있거나 윗부분이 남으면 이 목록을 조정한다.
    private static readonly string[] KeepBoneKeywords = { "wheel", "gear", "chassis", "dummy" };
    private static readonly string[] KeepBoneExactNames = { "bone_base" };

    [MenuItem("Tools/Arm/Add mobile base (lower body) to ArmA prefab")]
    private static void Run()
    {
        if (!File.Exists(ArmAPrefabPath))
        {
            Debug.LogError($"프리팹을 찾지 못했다: {ArmAPrefabPath}");
            return;
        }

        var robotAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath);
        if (robotAsset == null)
        {
            Debug.LogError($"프리팹을 찾지 못했다: {RobotPrefabPath}");
            return;
        }

        // 모델이 읽기 불가(Read/Write 꺼짐)면 정점을 읽을 수 없으므로 임시로 켰다가 끝나면 되돌린다.
        List<ModelImporter> toRestore = MakeMeshesReadable(robotAsset);
        try
        {
            Build(robotAsset);
        }
        finally
        {
            foreach (ModelImporter importer in toRestore)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }

    private static void Build(GameObject robotAsset)
    {
        GameObject armA = PrefabUtility.LoadPrefabContents(ArmAPrefabPath);
        try
        {
            // 이전에 만든 하부나 바퀴(이전 방식)가 있으면 지우고 다시 만든다. 몇 번 실행해도 결과가 같다.
            RemoveChild(armA.transform, BaseName);
            RemoveChild(armA.transform, OldWheelsName);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(armA);

            var armParts = new List<Transform>();
            foreach (Transform child in armA.transform)
            {
                armParts.Add(child);
            }

            float armBottom = MinWorldY(armParts);

            var robot = (GameObject)PrefabUtility.InstantiatePrefab(robotAsset, armA.transform);
            robot.name = BaseName;
            robot.transform.localPosition = Vector3.zero;
            robot.transform.localRotation = Quaternion.identity;

            float lowerTop = float.MinValue;
            var report = new StringBuilder();
            foreach (SkinnedMeshRenderer renderer in robot.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!TryBuildLowerMesh(renderer, report, out float top))
                {
                    continue;
                }

                lowerTop = Mathf.Max(lowerTop, top);
            }

            if (lowerTop == float.MinValue)
            {
                Debug.LogError("하부 메시를 만들지 못했다. 뼈 이름 목록이 맞는지 확인한다.\n" + report);
                return;
            }

            ConfigureRobot(robot);

            float lift = lowerTop - armBottom;
            foreach (Transform part in armParts)
            {
                part.localPosition += Vector3.up * lift;
            }

            AddIfMissing<MobileBaseDriver>(armA);
            var panel = AddIfMissing<MobileBaseDemoPanel>(armA);
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_mobileBase").objectReferenceValue = armA.GetComponent<MobileBaseDriver>();
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(armA, ArmAPrefabPath);
            AssetDatabase.DeleteAsset(OldWheelMeshFolder);
            Debug.Log($"{ArmAPrefabPath}: 하부(바퀴, 궤도, 하부 차체)를 붙이고 팔을 {lift:F2}m 옮겼다.\n{report}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(armA);
        }
    }

    // 하부 뼈에 속한 삼각형만 남긴 메시를 만들어 렌더러에 끼운다. 정점, 뼈 가중치, 바인드 포즈는 그대로 둔다.
    private static bool TryBuildLowerMesh(SkinnedMeshRenderer renderer, StringBuilder report, out float topWorldY)
    {
        topWorldY = float.MinValue;
        Mesh source = renderer.sharedMesh;
        Transform[] bones = renderer.bones;
        if (source == null || bones == null || bones.Length == 0)
        {
            return false;
        }

        BoneWeight[] weights = source.boneWeights;
        Vector3[] vertices = source.vertices;
        Matrix4x4[] bindPoses = source.bindposes;

        var keepBone = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            keepBone[i] = bones[i] != null && ShouldKeep(bones[i].name);
        }

        var mesh = new Mesh { name = $"ArmABase_{source.name}" };
        mesh.indexFormat = source.indexFormat;
        mesh.vertices = vertices;
        mesh.normals = source.normals;
        mesh.tangents = source.tangents;
        mesh.uv = source.uv;
        mesh.uv2 = source.uv2;
        mesh.colors = source.colors;
        mesh.boneWeights = weights;
        mesh.bindposes = bindPoses;
        mesh.subMeshCount = source.subMeshCount;

        var used = new HashSet<int>();
        int keptTriangles = 0;
        int totalTriangles = 0;
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            int[] triangles = source.GetTriangles(sub);
            var kept = new List<int>();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                totalTriangles++;
                int owned = 0;
                for (int k = 0; k < 3; k++)
                {
                    int bone = DominantBone(weights[triangles[t + k]]);
                    if (bone >= 0 && bone < keepBone.Length && keepBone[bone])
                    {
                        owned++;
                    }
                }

                // 두 정점 이상이 하부 뼈에 속하면 그 삼각형을 하부로 본다(경계 삼각형 구멍 방지).
                if (owned < 2)
                {
                    continue;
                }

                kept.Add(triangles[t]);
                kept.Add(triangles[t + 1]);
                kept.Add(triangles[t + 2]);
                used.Add(triangles[t]);
                used.Add(triangles[t + 1]);
                used.Add(triangles[t + 2]);
                keptTriangles++;
            }

            mesh.SetTriangles(kept, sub);
        }

        report.AppendLine($"  {renderer.name}: 삼각형 {keptTriangles}/{totalTriangles}개를 남겼다.");
        if (keptTriangles == 0)
        {
            Object.DestroyImmediate(mesh);
            return false;
        }

        // 쓰이지 않는 정점이 경계 상자를 키우지 않도록, 남긴 정점으로 경계와 윗면 높이를 다시 구한다.
        Bounds bounds = new Bounds(vertices[FirstOf(used)], Vector3.zero);
        foreach (int index in used)
        {
            bounds.Encapsulate(vertices[index]);
            float y = SkinVertex(vertices[index], weights[index], bones, bindPoses).y;
            topWorldY = Mathf.Max(topWorldY, y);
        }

        mesh.bounds = bounds;
        SaveMesh(mesh);
        renderer.sharedMesh = mesh;
        renderer.localBounds = bounds;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        return true;
    }

    private static bool ShouldKeep(string boneName)
    {
        string lower = boneName.ToLowerInvariant();
        foreach (string exact in KeepBoneExactNames)
        {
            if (lower == exact)
            {
                return true;
            }
        }

        foreach (string keyword in KeepBoneKeywords)
        {
            if (lower.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    // 키보드 조작은 끄고(Input System 전용 프로젝트에서 에러), 궤도 애니메이션만 쓴다.
    // GridRobotController는 시작할 때 위치를 격자에, 회전을 단위 각도에 맞추므로 사실상 맞추지 않도록 아주 작게 둔다.
    private static void ConfigureRobot(GameObject robot)
    {
        var controller = robot.GetComponent<GridRobotController>();
        if (controller != null)
        {
            controller.controlsLocked = true;
            controller.gridPointStep = 0.001f;
            controller.rotationStep = 0.001f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        }

        var interactor = robot.GetComponent<RobotCargoInteractor>();
        if (interactor != null)
        {
            interactor.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(interactor);
        }

        // 눈 깜빡임은 윗몸통(얼굴)용이라 하부만 쓰는 ArmA에는 필요 없다.
        var face = robot.GetComponent<RobotEyesController>();
        if (face != null)
        {
            face.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(face);
        }
    }

    private static void SaveMesh(Mesh mesh)
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Models"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Models");
        }

        if (!AssetDatabase.IsValidFolder(MeshFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Models", "ArmABase");
        }

        string path = $"{MeshFolder}/{mesh.name}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
    }

    private static Vector3 SkinVertex(Vector3 vertex, BoneWeight weight, Transform[] bones, Matrix4x4[] bindPoses)
    {
        Vector3 result = Vector3.zero;
        float total = 0f;
        Accumulate(vertex, weight.boneIndex0, weight.weight0, bones, bindPoses, ref result, ref total);
        Accumulate(vertex, weight.boneIndex1, weight.weight1, bones, bindPoses, ref result, ref total);
        Accumulate(vertex, weight.boneIndex2, weight.weight2, bones, bindPoses, ref result, ref total);
        Accumulate(vertex, weight.boneIndex3, weight.weight3, bones, bindPoses, ref result, ref total);
        return total > 0f ? result / total : vertex;
    }

    private static void Accumulate(
        Vector3 vertex, int index, float weight, Transform[] bones, Matrix4x4[] bindPoses, ref Vector3 sum, ref float total)
    {
        if (weight <= 0f || index < 0 || index >= bones.Length || bones[index] == null)
        {
            return;
        }

        Matrix4x4 skin = bones[index].localToWorldMatrix * bindPoses[index];
        sum += skin.MultiplyPoint3x4(vertex) * weight;
        total += weight;
    }

    private static int DominantBone(BoneWeight w)
    {
        int bone = w.boneIndex0;
        float best = w.weight0;
        if (w.weight1 > best) { best = w.weight1; bone = w.boneIndex1; }
        if (w.weight2 > best) { best = w.weight2; bone = w.boneIndex2; }
        if (w.weight3 > best) { bone = w.boneIndex3; }
        return bone;
    }

    private static int FirstOf(HashSet<int> set)
    {
        foreach (int value in set)
        {
            return value;
        }

        return 0;
    }

    private static List<ModelImporter> MakeMeshesReadable(GameObject robotAsset)
    {
        var changed = new List<ModelImporter>();
        foreach (SkinnedMeshRenderer renderer in robotAsset.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null || mesh.isReadable)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(mesh);
            if (AssetImporter.GetAtPath(path) is ModelImporter importer && !changed.Contains(importer))
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                changed.Add(importer);
            }
        }

        return changed;
    }

    private static void RemoveChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static T AddIfMissing<T>(GameObject target) where T : Component
    {
        var existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }

    private static float MinWorldY(List<Transform> roots)
    {
        float min = float.MaxValue;
        foreach (Transform root in roots)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                min = Mathf.Min(min, renderer.bounds.min.y);
            }
        }

        return min == float.MaxValue ? 0f : min;
    }
}
