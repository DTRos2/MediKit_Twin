using MediKitTwin.Common;
using MediKitTwin.Demo;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 컨베이어 속도 패널을 열린 씬(Scene_Cell)의 계층에 올린다. 패널은 프리팹(PF_ConveyorSpeedPanel)이고, 씬의 DashboardCanvas 아래에
    /// 그 인스턴스가 눈에 보이는 오브젝트로 놓인다. 속도 조절 컴포넌트(IConveyorSpeedControl)는 실행할 때 인터페이스로 찾으므로 이을 참조가 없다.
    /// 놓는 순서는 모든 패널이 같아서 DashboardPanelPlacer가 맡고, 메뉴와 배치 모드 입구는 DashboardPanelsSetup이 맡는다.
    /// </summary>
    public static class ConveyorSpeedPanelSetup
    {
        public static bool Apply(bool isRebuild)
        {
            DashboardPanelPlacer.Place<ConveyorSpeedPanelPresenter>(
                ProjectAssetPaths.ConveyorSpeedPanelPrefab, () => ConveyorSpeedPanelPrefabBuilder.Build().gameObject, isRebuild);
            return true;
        }
    }
}
