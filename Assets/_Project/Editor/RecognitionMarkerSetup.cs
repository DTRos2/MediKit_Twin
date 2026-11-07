using MediKitTwin.Common;
using MediKitTwin.Demo;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 인식 표시를 열린 씬(Scene_Cell)의 계층에 올린다. 표시는 프리팹(PF_RecognitionMarker)이고, 씬의 DashboardCanvas 아래에
    /// 그 인스턴스가 눈에 보이는 오브젝트로 놓인다. 예전에는 플레이를 시작할 때 코드가 만들었지만 이제는 씬에 미리 있다.
    /// 카메라는 실행할 때 인터페이스(IToolCameraView)로 찾으므로 연결할 씬 참조가 없다.
    /// 놓는 순서는 모든 패널이 같아서 DashboardPanelPlacer가 맡고, 메뉴와 배치 모드 입구는 DashboardPanelsSetup이 맡는다.
    /// </summary>
    public static class RecognitionMarkerSetup
    {
        public static bool Apply(bool isRebuild)
        {
            DashboardPanelPlacer.Place<RecognitionMarkerPresenter>(
                ProjectAssetPaths.RecognitionMarkerPrefab, () => RecognitionMarkerPrefabBuilder.Build().gameObject, isRebuild);
            return true;
        }
    }
}
