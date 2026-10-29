using UnityEditor;
using UnityEngine;

// URDF import로 생긴 Built-in(Standard) 머티리얼은 URP에서 마젠타로 보인다.
// 선택한 폴더(없으면 Assets/fr5_description) 안의 머티리얼을 URP/Lit으로 바꾼다.
public static class UrdfMaterialToUrp
{
    [MenuItem("Tools/URDF/Convert materials to URP Lit (selected folder)")]
    static void Run()
    {
        string folder = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            folder = "Assets/fr5_description";

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP/Lit 셰이더를 찾을 수 없습니다. URP가 설치/설정되어 있는지 확인하세요.");
            return;
        }

        int converted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == urpLit) continue;

            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;

            mat.shader = urpLit;
            mat.SetColor("_BaseColor", color);
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"머티리얼 {converted}개를 URP Lit으로 변환했습니다 ({folder}).");
    }
}
