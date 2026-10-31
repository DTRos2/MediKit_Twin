using System.Collections.Generic;
using System.Linq;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Demo;
using MediKitTwin.Inventory;
using MediKitTwin.Mobile;
using MediKitTwin.Orders;
using MediKitTwin.Recipes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 열려 있는 씬에 키팅에 필요한 컴포넌트를 붙이고 서로 연결한다.
// 씬에 이미 있는 지그(KitBoxSupply), 선반(ShelfStockPresenter), 로봇암(ArmController)을 찾아 쓰고,
// 주문 세션, 키팅 컨트롤러, 이송, 시험 패널을 한 오브젝트(KittingStation)와 로봇에 붙인다.
// 몇 번을 실행해도 같은 결과가 되도록 이미 붙은 컴포넌트는 다시 붙이지 않고 연결만 다시 한다.
public static class KittingStationSetup
{
    private const string StationName = "KittingStation";

    [MenuItem("Tools/MediKit/Setup Kitting Station")]
    private static void Setup()
    {
        if (!SceneObjectFinder.TryFindSingle(out KitBoxSupply boxSupply, "지그(KitBoxSupply)")
            || !SceneObjectFinder.TryFindSingle(out ShelfStockPresenter shelf, "선반(ShelfStockPresenter)")
            || !SceneObjectFinder.TryFindRobot(out ArmController arm))
        {
            return;
        }

        // GetComponentInChildren은 자기 자신을 먼저 찾으므로, 같은 오브젝트에 있으면 그것을 쓴다.
        var tool = arm.GetComponentInChildren<IkArmTarget>(true);
        var gripper = arm.GetComponentInChildren<JointGripper>(true);
        if (tool == null || gripper == null)
        {
            Debug.LogError($"[KittingStationSetup] {arm.name}에 IkArmTarget과 JointGripper가 모두 있어야 한다.", arm);
            return;
        }

        ScenarioConfigSO scenario = SceneObjectFinder.FindAsset<ScenarioConfigSO>();
        List<RecipeSO> recipes = SceneObjectFinder.FindAssets<RecipeSO>();
        if (scenario == null || recipes.Count == 0)
        {
            Debug.LogError("[KittingStationSetup] ScenarioConfigSO 에셋과 RecipeSO 에셋이 프로젝트에 있어야 한다.");
            return;
        }

        GameObject station = FindOrCreateStation();
        var orders = GetOrAdd<OrderSessionBehaviour>(station);
        var controller = GetOrAdd<KittingController>(station);
        var handoff = GetOrAdd<ConveyorHandoffStub>(station);
        var manual = GetOrAdd<ManualKittingStation>(station);
        GetOrAdd<OrderCsvDemoPanel>(station);
        GetOrAdd<ShelfStockDemoPanel>(station);
        GetOrAdd<KittingStatusDemoPanel>(station);
        var jointPanel = GetOrAdd<ArmJointDemoPanel>(station);

        var holder = GetOrAdd<ScriptAttachHolder>(arm.gameObject);
        var transfer = GetOrAdd<ArmItemTransfer>(arm.gameObject);
        bool isBaseWired = WireBaseMovement(arm, tool, shelf, transfer);

        bool isWired =
            SerializedFieldAssigner.SetReferenceList(orders, "_recipes", recipes.Cast<Object>().ToList())
            & SerializedFieldAssigner.SetReference(holder, "_gripper", gripper)
            & SerializedFieldAssigner.SetReference(holder, "_armState", arm)
            & SerializedFieldAssigner.SetReference(transfer, "_toolTarget", tool)
            & SerializedFieldAssigner.SetReference(transfer, "_holder", holder)
            & SerializedFieldAssigner.SetReference(controller, "_kitSource", orders)
            & SerializedFieldAssigner.SetReference(controller, "_stock", shelf)
            & SerializedFieldAssigner.SetReference(controller, "_itemLocator", shelf)
            & SerializedFieldAssigner.SetReference(controller, "_boxSupply", boxSupply)
            & SerializedFieldAssigner.SetReference(controller, "_itemTransfer", transfer)
            & SerializedFieldAssigner.SetReference(controller, "_scenario", scenario)
            & SerializedFieldAssigner.SetReference(handoff, "_boxSupply", boxSupply)
            & SerializedFieldAssigner.SetReference(jointPanel, "_arm", arm)
            & WireManualKitting(manual, shelf, boxSupply, transfer, controller, recipes)
            & isBaseWired;

        EditorSceneManager.MarkSceneDirty(station.scene);
        Selection.activeGameObject = station;

        Debug.Log(isWired
            ? $"[KittingStationSetup] 연결을 마쳤다. 로봇 {arm.name}, 지그 {boxSupply.name}, 선반 {shelf.name}, 레시피 {recipes.Count}개. 씬을 저장한다(Ctrl+S)."
            : "[KittingStationSetup] 일부 연결에 실패했다. 위의 오류를 확인한다.");
    }

    // 씬에 굴러가는 베이스(MobileBaseDriver)가 하나 있으면 이동 모듈을 붙여 이송에 연결한다. 팔(fr5_robot)과 베이스(ArmA)는
    // 형제 오브젝트일 수 있으므로 부모가 아니라 씬 전체에서 찾는다. 없으면 이동 없이 닿는 곳만 집는 고정 팔로 두고 알린다.
    private static bool WireBaseMovement(ArmController arm, IkArmTarget tool, ShelfStockPresenter shelf, ArmItemTransfer transfer)
    {
        ArmBaseApproachConfigSO approach = SceneObjectFinder.FindAsset<ArmBaseApproachConfigSO>();
        MobileBaseDriver[] bases = Object.FindObjectsByType<MobileBaseDriver>(FindObjectsInactive.Include);
        if (bases.Length != 1 || approach == null)
        {
            Debug.LogWarning(approach == null
                ? "[KittingStationSetup] ArmBaseApproachConfig 에셋이 없어 이동 없이 고정 팔로 둔다. Create > MediKitTwin > Arm Base Approach Config로 만든다."
                : $"[KittingStationSetup] 씬에 MobileBaseDriver가 {bases.Length}개 있어 이동 없이 고정 팔로 둔다. 정확히 1개여야 한다.");
            return true;
        }

        var positioner = GetOrAdd<ArmBasePositionerBehaviour>(arm.gameObject);
        return SerializedFieldAssigner.SetReference(positioner, "_mobileBase", bases[0])
            & SerializedFieldAssigner.SetReference(positioner, "_armBase", arm)
            & SerializedFieldAssigner.SetReference(positioner, "_config", approach)
            & SerializedFieldAssigner.SetReference(positioner, "_toolTarget", tool)
            & SerializedFieldAssigner.SetReference(positioner, "_armDriver", arm)
            & SerializedFieldAssigner.SetReference(transfer, "_positioner", positioner)
            & SerializedFieldAssigner.SetReference(transfer, "_approach", approach)
            & KittingCollisionLayerSetup.Apply(arm, shelf, approach);
    }

    // 수동 키팅: 자동 키팅과 같은 재고, 선반, 박스, 이송을 쓰고, 수동 모드 동안 자동 키팅(controller)을 멈춘다.
    private static bool WireManualKitting(
        ManualKittingStation manual, ShelfStockPresenter shelf, KitBoxSupply boxSupply, ArmItemTransfer transfer,
        KittingController controller, List<RecipeSO> recipes)
    {
        return SerializedFieldAssigner.SetReference(manual, "_stock", shelf)
            & SerializedFieldAssigner.SetReference(manual, "_itemLocator", shelf)
            & SerializedFieldAssigner.SetReference(manual, "_boxSupply", boxSupply)
            & SerializedFieldAssigner.SetReference(manual, "_itemTransfer", transfer)
            & SerializedFieldAssigner.SetReference(manual, "_autoKitting", controller)
            & SerializedFieldAssigner.SetReferenceList(manual, "_recipes", recipes.Cast<Object>().ToList());
    }

    private static GameObject FindOrCreateStation()
    {
        GameObject existing = GameObject.Find(StationName);
        if (existing != null)
        {
            return existing;
        }

        var station = new GameObject(StationName);
        Undo.RegisterCreatedObjectUndo(station, "Create Kitting Station");
        return station;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(target);
    }
}
