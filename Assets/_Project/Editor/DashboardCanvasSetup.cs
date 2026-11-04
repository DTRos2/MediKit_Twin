using MediKitTwin.Dashboard;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 열린 씬에 대시보드 uGUI가 올라갈 캔버스(DashboardCanvas)와 눌림을 받는 이벤트 시스템을 준비한다. 패널 프리팹은 이 캔버스 아래에 놓인다.
    /// 화면 크기에 맞춰 커지고 줄어드는 방식과 기준 화면 크기는 IMGUI 패널과 같은 값(DashboardMetrics)을 쓰므로 두 방식의 패널이 같은 배율로 보인다.
    /// 이미 있으면 새로 만들지 않아 여러 번 불러도 같은 결과이다.
    /// </summary>
    public static class DashboardCanvasSetup
    {
        private const string CanvasName = "DashboardCanvas";
        private const string EventSystemName = "EventSystem";

        public static Canvas EnsureCanvas()
        {
            foreach (Canvas existing in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (existing.name == CanvasName)
                {
                    return existing;
                }
            }

            var go = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = LayerMask.NameToLayer("UI"),
            };
            Undo.RegisterCreatedObjectUndo(go, "Create Dashboard Canvas");

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 가로와 세로 중 더 모자란 쪽에 맞춰(Expand) 패널이 화면 밖으로 나가지 않게 한다. IMGUI의 DashboardScale과 같은 방식이다.
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DashboardMetrics.ReferenceWidth, DashboardMetrics.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return canvas;
        }

        /// <summary>
        /// 이 프로젝트는 새 입력 시스템만 쓰므로 옛 입력을 읽는 모듈(StandaloneInputModule)은 쓸 수 없고 InputSystemUIInputModule을 붙인다.
        /// 이 모듈은 켜질 때 입력 동작이 비어 있으면 기본 동작을 스스로 채운다.
        /// </summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var go = new GameObject(EventSystemName, typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create Event System");
        }
    }
}
