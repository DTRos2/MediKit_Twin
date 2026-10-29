using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// URDF가 참조하는 메시 에셋을 import 전에 로드해 보고, sharedMesh가 null인 MeshFilter를 찾는다.
public static class UrdfMeshDiagnostics
{
    [MenuItem("Tools/URDF/Diagnose selected URDF meshes")]
    static void Run()
    {
        string urdfPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(urdfPath) || !urdfPath.EndsWith(".urdf"))
        {
            Debug.LogError("Project 창에서 .urdf 파일을 선택하세요.");
            return;
        }

        string text = System.IO.File.ReadAllText(urdfPath);
        int bad = 0;
        foreach (Match m in Regex.Matches(text, "filename=\"package://([^\"]+)\""))
        {
            string rel = m.Groups[1].Value;
            string assetPath = "Assets/" + rel;
            if (assetPath.EndsWith(".STL", System.StringComparison.OrdinalIgnoreCase))
                assetPath = assetPath.Substring(0, assetPath.Length - 4) + ".prefab";

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (go == null) { Debug.LogError($"[로드 실패] {assetPath}"); bad++; continue; }

            var filters = go.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) { Debug.LogError($"[MeshFilter 없음] {assetPath}"); bad++; continue; }

            foreach (var f in filters)
            {
                if (f.sharedMesh == null) { Debug.LogError($"[sharedMesh null] {assetPath} / {f.name}"); bad++; }
                else Debug.Log($"[OK] {assetPath} / {f.name}  verts={f.sharedMesh.vertexCount} readable={f.sharedMesh.isReadable}");
            }
        }
        Debug.Log($"진단 완료: 문제 {bad}건");
    }
}
