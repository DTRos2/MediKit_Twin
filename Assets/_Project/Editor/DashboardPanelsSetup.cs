using System;
using MediKitTwin.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 대시보드 패널들을 열린 씬(Scene_Cell)의 계층에 한꺼번에 올리는 입구(메뉴와 배치 모드). 패널이 늘면 아래 목록에 한 줄을 더한다.
    /// 패널은 모두 프리팹이고 씬의 DashboardCanvas 아래에 눈에 보이는 오브젝트로 놓이며, 코드가 실행 중에 만들거나 그리지 않는다.
    ///  - Setup: 프리팹이 없으면 만들고, 씬에 없는 패널만 올린다(사람이 고친 프리팹과 씬은 그대로 둔다).
    ///  - Rebuild: 모든 프리팹을 처음 모양으로 다시 만든다. 씬의 인스턴스는 프리팹을 따라가므로 끊기지 않는다.
    /// 몇 번을 실행해도 같은 결과가 된다.
    /// </summary>
    public static class DashboardPanelsSetup
    {
        private readonly struct Panel
        {
            public Panel(string name, Func<bool, bool> apply)
            {
                Name = name;
                Apply = apply;
            }

            public string Name { get; }

            public Func<bool, bool> Apply { get; }
        }

        private static readonly Panel[] Panels =
        {
            new Panel("주문", OrderPanelSetup.TryApply),
            new Panel("배속", SimSpeedPanelSetup.Apply),
            new Panel("공정 현황", ProcessStatusPanelSetup.Apply),
            new Panel("컨베이어 속도", ConveyorSpeedPanelSetup.Apply),
            new Panel("선반 재고", ShelfStockPanelSetup.Apply),
            new Panel("관절 제어", ArmJointPanelSetup.Apply),
            new Panel("키팅 로그", KittingLogPanelSetup.Apply),
            new Panel("툴 카메라", ToolCameraPanelSetup.Apply),
            new Panel("인식 표시", RecognitionMarkerSetup.Apply),
        };

        [MenuItem("Tools/MediKit/Setup Dashboard Panels")]
        private static void SetupMenu()
        {
            Run(Apply, isRebuild: false);
        }

        [MenuItem("Tools/MediKit/Rebuild Dashboard Panel Prefabs")]
        private static void RebuildMenu()
        {
            Run(Apply, isRebuild: true);
        }

        // 허용 배속(SimClock.AllowedSpeeds)을 바꾼 뒤 쓴다. 다른 패널의 프리팹은 건드리지 않아 손으로 고친 부분이 남는다.
        [MenuItem("Tools/MediKit/Rebuild Sim Speed Panel Prefab")]
        private static void RebuildSimSpeedMenu()
        {
            Run(SimSpeedPanelSetup.Apply, isRebuild: true);
        }

        /// <summary>배치 모드용: Scene_Cell을 열어 패널을 올리고 저장한다. -executeMethod로 부른다.</summary>
        public static void SetupAndSave()
        {
            SetupAndSaveCore(Apply, isRebuild: false);
        }

        /// <summary>배치 모드용: 프리팹을 처음 모양으로 다시 만든 뒤 Scene_Cell을 저장한다. -executeMethod로 부른다.</summary>
        public static void RebuildAndSave()
        {
            SetupAndSaveCore(Apply, isRebuild: true);
        }

        /// <summary>배치 모드용: 배속 패널 프리팹만 처음 모양으로 다시 만든 뒤 Scene_Cell을 저장한다. 다른 패널은 건드리지 않는다. -executeMethod로 부른다.</summary>
        public static void RebuildSimSpeedAndSave()
        {
            SetupAndSaveCore(SimSpeedPanelSetup.Apply, isRebuild: true);
        }

        /// <summary>열린 씬이 Scene_Cell일 때 모든 패널을 준비한다. 하나라도 못 하면 false.</summary>
        public static bool Apply(bool isRebuild)
        {
            bool allDone = true;
            LegacyPanelCleanup.RemoveMissingScripts();
            foreach (Panel panel in Panels)
            {
                bool done = panel.Apply(isRebuild);
                allDone &= done;
                Debug.Log($"[DashboardPanelsSetup] {panel.Name} 패널: {(done ? "준비했다" : "중단 — 위 오류를 확인하세요")}.");
            }

            return allDone;
        }

        private static void Run(Func<bool, bool> apply, bool isRebuild)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[DashboardPanelsSetup] 플레이 모드를 끈 뒤 실행하세요.");
                return;
            }

            bool done = apply(isRebuild);
            Debug.Log($"[DashboardPanelsSetup] {(done ? "완료. 씬을 확인한 뒤 저장하세요." : "일부 패널을 준비하지 못했습니다.")}");
        }

        private static void SetupAndSaveCore(Func<bool, bool> apply, bool isRebuild)
        {
            EditorSceneManager.OpenScene(ProjectAssetPaths.CellScene, OpenSceneMode.Single);
            bool done = apply(isRebuild);
            if (done)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log($"[DashboardPanelsSetup] {(done ? "설정하고 저장했습니다." : "중단했습니다.")}");
            EditorApplication.Exit(done ? 0 : 1);
        }
    }
}
