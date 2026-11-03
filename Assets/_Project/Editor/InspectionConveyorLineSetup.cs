using MediKitTwin.Conveyor;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// PF_InspectionStation에 검사 구간 컨베이어 제어(InspectionConveyorLine)를 붙인다. (FR-05 일부)
    /// 프리팹을 다시 만들지 않고 기존 루트에 컴포넌트만 더한다. 이미 있으면 아무것도 하지 않는다. 씬 파일은 바뀌지 않는다.
    /// </summary>
    public static class InspectionConveyorLineSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/PF_InspectionStation.prefab";

        [MenuItem("Tools/MediKit/Add Conveyor Line To InspectionStation Prefab")]
        public static void AddLine()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"[InspectionConveyorLineSetup] 프리팹이 없습니다: {PrefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<InspectionConveyorLine>() != null)
                {
                    Debug.Log("[InspectionConveyorLineSetup] PF_InspectionStation에 이미 InspectionConveyorLine이 있습니다.");
                    return;
                }

                root.AddComponent<InspectionConveyorLine>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[InspectionConveyorLineSetup] PF_InspectionStation에 InspectionConveyorLine을 추가했습니다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
