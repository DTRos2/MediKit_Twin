using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Demo;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 관절 제어 패널을 열린 씬(Scene_Cell)의 계층에 올린다. 패널은 프리팹(PF_ArmJointPanel)이고, 씬의 DashboardCanvas 아래에
    /// 그 인스턴스가 눈에 보이는 오브젝트로 놓인다. 놓은 뒤 조작할 로봇(키팅이 쓰는 로봇)을 패널의 _arm에 연결한다.
    /// 씬에 로봇이 둘(FR5, UR3)이어도 키팅이 쓰는 로봇만 연결해 짝이 어긋나지 않는다. 로봇을 하나로 정할 수 없으면(둘 이상이거나 없음)
    /// 연결을 비워 두고, 그러면 패널이 실행할 때 씬에서 찾은 로봇을 머리줄 칩으로 바꿔 가며 조작한다.
    /// 놓는 순서는 모든 패널이 같아서 DashboardPanelPlacer가 맡고, 메뉴와 배치 모드 입구는 DashboardPanelsSetup이 맡는다.
    /// </summary>
    public static class ArmJointPanelSetup
    {
        public static bool Apply(bool isRebuild)
        {
            ArmJointPanelPresenter presenter = DashboardPanelPlacer.Place<ArmJointPanelPresenter>(
                ProjectAssetPaths.ArmJointPanelPrefab, () => ArmJointPanelPrefabBuilder.Build().gameObject, isRebuild);

            // 키팅이 쓰는 로봇을 찾지 못해도 패널은 놓였으니 실패는 아니다. 칩으로 고르는 모양으로 남는다.
            if (SceneObjectFinder.TryFindRobot(out ArmController arm))
            {
                SerializedFieldAssigner.SetReference(presenter, "_arm", arm);
            }
            else
            {
                Debug.LogWarning("[ArmJointPanelSetup] 로봇을 하나로 정하지 못해 패널의 조작 로봇을 비워 둔다(실행할 때 씬에서 찾아 칩으로 고른다).");
            }

            return true;
        }
    }
}
