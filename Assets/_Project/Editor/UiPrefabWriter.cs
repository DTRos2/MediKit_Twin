using System.IO;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>임시로 만든 uGUI 오브젝트를 프리팹 에셋으로 저장하고 임시 오브젝트를 치운다. 같은 경로에 다시 저장하면 에셋의 GUID가 그대로라 씬의 인스턴스가 끊기지 않는다.</summary>
    public static class UiPrefabWriter
    {
        public static T SaveAndLoad<T>(GameObject temporaryRoot, string assetPath) where T : Component
        {
            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            PrefabUtility.SaveAsPrefabAsset(temporaryRoot, assetPath);
            Object.DestroyImmediate(temporaryRoot);
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath).GetComponent<T>();
        }

        // 폴더가 없으면 위에서부터 차례로 만든다.
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
