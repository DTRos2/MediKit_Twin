using MediKitTwin.ArmA;
using MediKitTwin.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 설정 에셋에 적힌 레이어 이름대로 레이어를 만들고, 로봇암과 선반 아래 오브젝트 전부에 붙인다.
// 이동 중 충돌을 끄는 쪽(ArmBasePositionerBehaviour)은 레이어 이름만 보므로, 이 도구를 실행해야 레이어가 실제로 나뉜다.
public static class KittingCollisionLayerSetup
{
    /// <summary>두 레이어를 준비하고 오브젝트에 지정한다. 이름이 비어 있으면(쓰지 않는 설정) 아무것도 하지 않고 true이다.</summary>
    public static bool Apply(ArmController arm, ShelfStockPresenter shelf, ArmBaseApproachConfigSO approach)
    {
        if (string.IsNullOrEmpty(approach.ArmLayerName) || string.IsNullOrEmpty(approach.ShelfLayerName))
        {
            return true;
        }

        int armLayer = LayerRegistrar.EnsureLayer(approach.ArmLayerName);
        int shelfLayer = LayerRegistrar.EnsureLayer(approach.ShelfLayerName);
        if (armLayer < 0 || shelfLayer < 0)
        {
            return false;
        }

        int armCount = AssignLayer(arm.gameObject, armLayer);
        int shelfCount = AssignLayer(shelf.gameObject, shelfLayer);

        Debug.Log($"[KittingCollisionLayerSetup] '{approach.ArmLayerName}' 레이어 {armCount}개, '{approach.ShelfLayerName}' 레이어 {shelfCount}개 지정. " +
                  $"선반 충돌체가 {shelf.name} 밖에 있으면 그 오브젝트에도 '{approach.ShelfLayerName}' 레이어를 직접 지정한다.");
        return true;
    }

    // Setup으로 지정한 레이어를 되돌린다. 레이어를 지정한 뒤 플레이가 죽거나 물리가 이상할 때 원인을 가려내는 데 쓴다.
    // 레이어 이름 자체는 프로젝트에 남는다. Setup을 다시 돌려도 지정되지 않게 하려면 설정 에셋의 레이어 이름을 비운다.
    [MenuItem("Tools/MediKit/Reset Kitting Collision Layers")]
    private static void ResetToDefault()
    {
        if (!SceneObjectFinder.TryFindSingle(out ShelfStockPresenter shelf, "선반(ShelfStockPresenter)")
            || !SceneObjectFinder.TryFindRobot(out ArmController arm))
        {
            return;
        }

        int armCount = AssignLayer(arm.gameObject, 0);
        int shelfCount = AssignLayer(shelf.gameObject, 0);
        EditorSceneManager.MarkSceneDirty(arm.gameObject.scene);

        Debug.Log($"[KittingCollisionLayerSetup] 로봇 {armCount}개, 선반 {shelfCount}개를 Default 레이어로 되돌렸다. 씬을 저장한다(Ctrl+S).");
    }

    private static int AssignLayer(GameObject root, int layer)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform node in all)
        {
            if (node.gameObject.layer != layer)
            {
                Undo.RecordObject(node.gameObject, "Assign Collision Layer");
                node.gameObject.layer = layer;
            }
        }

        return all.Length;
    }
}
