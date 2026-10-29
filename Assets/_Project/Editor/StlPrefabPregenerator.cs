using System.IO;
using Unity.Robotics.UrdfImporter;
using UnityEditor;
using UnityEngine;

// URDF Importer는 STL 프리팹을 import 도중에 만들고 곧바로 VHACD에 넘기는데,
// 이때 메시가 아직 로드되지 않아 NullReferenceException이 난다.
// import 전에 프리팹을 미리 만들어 두면 이 문제를 피하면서 VHACD를 그대로 쓸 수 있다.
public static class StlPrefabPregenerator
{
    [MenuItem("Tools/URDF/Pre-generate STL Prefabs (selected folder)")]
    static void Run()
    {
        string folder = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
        {
            // 선택이 없거나 폴더가 아니면 기본 폴더를 사용한다.
            folder = "Assets/fr5_description";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError("Assets/fr5_description 폴더를 찾을 수 없습니다.");
                return;
            }
        }

        int created = 0;
        foreach (string guid in AssetDatabase.FindAssets("", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".stl", System.StringComparison.OrdinalIgnoreCase)) continue;

            string prefabPath = StlAssetPostProcessor.GetPrefabAssetPath(path);
            if (File.Exists(prefabPath)) continue;

            StlAssetPostProcessor.PostprocessStlFile(path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"STL 프리팹 {created}개 생성 완료. 이제 URDF를 import하세요.");
    }
}
