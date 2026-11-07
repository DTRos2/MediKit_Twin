using MediKitTwin.Common;
using MediKitTwin.Demo;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Orders;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 주문 패널을 열린 씬(Scene_Cell)의 계층에 올린다. 패널은 코드가 실행 중에 그려 내는 것이 아니라 프리팹(PF_OrderPanel)이고,
    /// 씬의 DashboardCanvas 아래에 그 인스턴스가 눈에 보이는 오브젝트로 놓인다. 씬에는 인스턴스와 주문 관리와의 연결만 남는다.
    /// 놓는 순서는 모든 패널이 같아서 DashboardPanelPlacer가 맡는다. 이 클래스는 주문 패널만의 일을 더한다:
    /// 주문 관리(OrderSessionBehaviour)와 잇고, 예전 IMGUI 주문 패널이 남긴 "스크립트 없음" 컴포넌트를 주문 관리 오브젝트에서 지운다.
    /// 메뉴와 배치 모드 입구는 DashboardPanelsSetup이 맡는다. 몇 번을 실행해도 같은 결과가 된다.
    /// </summary>
    public static class OrderPanelSetup
    {
        /// <summary>
        /// 키팅 스테이션 설정(KittingStationSetup)이 부르는 입구. 주문 관리와 연결된 패널을 씬에 준비하고 성공 여부를 돌려준다.
        /// </summary>
        public static bool Apply(MonoBehaviour orders, bool isRebuild)
        {
            OrderPanelPresenter presenter = DashboardPanelPlacer.Place<OrderPanelPresenter>(
                ProjectAssetPaths.OrderPanelPrefab, BuildPrefab, isRebuild);

            bool isWired = SerializedFieldAssigner.SetReference(presenter, "_orders", orders);
            RemoveMissingScripts(orders.gameObject);
            return isWired;
        }

        /// <summary>씬에서 주문 관리를 찾아 패널을 준비한다. 주문 관리가 없으면 false.</summary>
        public static bool TryApply(bool isRebuild)
        {
            if (!SceneObjectFinder.TryFindSingle(out OrderSessionBehaviour orders, "주문 관리(OrderSessionBehaviour)"))
            {
                return false;
            }

            return Apply(orders, isRebuild);
        }

        private static GameObject BuildPrefab()
        {
            OrderRowItem orderRow = OrderRowItemPrefabBuilder.Build();
            UiTextRow issueRow = TextRowPrefabBuilder.Build();
            return OrderPanelPrefabBuilder.Build(orderRow, issueRow).gameObject;
        }

        private static void RemoveMissingScripts(GameObject target)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
            if (removed > 0)
            {
                Debug.Log($"[OrderPanelSetup] {target.name}에서 예전 주문 패널이 남긴 '스크립트 없음' 컴포넌트 {removed}개를 지웠다.");
            }
        }
    }
}
