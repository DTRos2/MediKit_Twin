using System;
using MediKitTwin.Dashboard;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 패널 프리팹을 열린 씬의 DashboardCanvas 아래에 놓는 공통 순서. 패널마다 같은 코드를 복사하지 않도록 한곳에 두었다.
    /// 1) 프리팹이 없거나 다시 만들라고 했으면 만든다(있으면 사람이 고친 것을 지키려고 그대로 둔다).
    /// 2) 캔버스와 이벤트 시스템이 없으면 만든다.
    /// 3) 씬에 이 패널이 없으면 캔버스 아래에 프리팹 인스턴스를 놓는다(있으면 그대로 둔다).
    /// 몇 번을 실행해도 같은 결과가 된다.
    /// </summary>
    public static class DashboardPanelPlacer
    {
        /// <param name="prefabPath">패널 프리팹 에셋 경로.</param>
        /// <param name="build">프리팹을 처음 모양으로 만들어 저장하고 그 프리팹의 루트를 돌려주는 함수.</param>
        /// <param name="isRebuild">true이면 프리팹이 있어도 처음 모양으로 다시 만든다.</param>
        /// <typeparam name="TPresenter">패널 루트에 붙은 동작 코드. 씬에서 이 패널이 이미 놓였는지 찾는 열쇠이기도 하다.</typeparam>
        public static TPresenter Place<TPresenter>(string prefabPath, Func<GameObject> build, bool isRebuild)
            where TPresenter : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || isRebuild)
            {
                prefab = build();
            }

            Canvas canvas = DashboardCanvasSetup.EnsureCanvas();
            DashboardCanvasSetup.EnsureEventSystem();

            TPresenter presenter = Object.FindAnyObjectByType<TPresenter>(FindObjectsInactive.Include);
            if (presenter == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
                Undo.RegisterCreatedObjectUndo(instance, $"Place {prefab.name}");
                presenter = instance.GetComponent<TPresenter>();
            }

            EditorSceneManager.MarkSceneDirty(presenter.gameObject.scene);
            return presenter;
        }
    }
}
