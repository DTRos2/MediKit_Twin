using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// .dae/.fbx 안에 내장된 머티리얼은 Standard 셰이더라 URP에서 마젠타로 보이고, 내장 머티리얼은 직접 고칠 수도 없다.
// 모델별로 URP/Lit 머티리얼을 만들어 Remap으로 연결한다.
public static class UrdfModelMaterialExtractor
{
    [MenuItem("Tools/URDF/Extract model materials as URP Lit (selected folder)")]
    static void Run()
    {
        string folder = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            folder = "Assets/fr5_description";

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP/Lit 셰이더를 찾을 수 없습니다.");
            return;
        }

        string outDir = folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(outDir))
            AssetDatabase.CreateFolder(folder, "Materials");

        int remapped = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;

            var embedded = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
            foreach (var src in embedded)
            {
                Color color = src.HasProperty("_Color") ? src.GetColor("_Color")
                            : src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : Color.white;
                Texture tex = src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;

                var mat = new Material(urpLit) { name = src.name };
                mat.SetColor("_BaseColor", color);
                if (tex != null) mat.SetTexture("_BaseMap", tex);

                string matPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{outDir}/{Path.GetFileNameWithoutExtension(path)}_{src.name}.mat");
                AssetDatabase.CreateAsset(mat, matPath);

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), mat);
                remapped++;
            }
            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"모델 머티리얼 {remapped}개를 URP Lit으로 추출/연결했습니다 ({folder}).");
    }
}
