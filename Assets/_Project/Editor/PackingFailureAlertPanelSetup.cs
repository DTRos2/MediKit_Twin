using MediKitTwin.Common;
using MediKitTwin.Demo;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 팩킹 실패 경고 패널을 열린 씬(Scene_Cell)의 계층에 올린다. 패널은 프리팹(PF_PackingFailureAlertPanel)이고, 씬의 DashboardCanvas 아래에
    /// 그 인스턴스가 눈에 보이는 오브젝트로 놓인다. 사건(CellEvents)은 정적이라 씬의 다른 오브젝트와 이을 것이 없다.
    /// 놓는 순서는 모든 패널이 같아서 DashboardPanelPlacer가 맡고, 메뉴와 배치 모드 입구는 DashboardPanelsSetup이 맡는다.
    /// </summary>
    public static class PackingFailureAlertPanelSetup
    {
        /// <summary>팩킹 실패 경고 패널을 씬에 준비한다. 열린 씬이 Scene_Cell이어야 한다.</summary>
        public static bool Apply(bool isRebuild)
        {
            DashboardPanelPlacer.Place<PackingFailureAlertPanelPresenter>(
                ProjectAssetPaths.PackingFailureAlertPanelPrefab, () => PackingFailureAlertPanelPrefabBuilder.Build().gameObject, isRebuild);
            return true;
        }
    }
}
