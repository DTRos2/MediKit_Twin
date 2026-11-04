using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 인식 표시 프리팹(PF_RecognitionMarker)을 만든다. 화면 전체를 덮는 영역 안에 초록 네모와 그 아래 글이 계층의 오브젝트로 들어가고,
    /// 화면 코드(RecognitionMarkerView)와 동작 코드(RecognitionMarkerPresenter)가 연결한다. 영역은 눌림을 받지 않아 뒤의 화면을 가리지 않는다.
    /// 만든 뒤에는 프리팹을 직접 열어 고치면 되고, 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class RecognitionMarkerPrefabBuilder
    {
        private const string RootName = "PF_RecognitionMarker";

        // 인식한 물건 자리의 네모와 글 색. 메인 화면의 물건 위에 올라가므로 밝은 색을 쓴다.
        private static readonly Color MarkerColor = Color.green;

        public static RecognitionMarkerPresenter Build()
        {
            // 화면 전체를 덮는 영역. 부모(캔버스)를 가득 채우고 가운데가 기준이다.
            var area = new GameObject(RootName, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var areaRect = (RectTransform)area.transform;
            UiPrimitives.Stretch(areaRect);

            RectTransform mark = UiWidgets.AddOutlineFrame(
                areaRect, "Mark", DashboardMetrics.RecognitionMarkerSize, DashboardMetrics.RecognitionMarkerLine, MarkerColor);
            Text label = AddLabel(mark);
            mark.gameObject.SetActive(false);

            var view = area.AddComponent<RecognitionMarkerView>();
            SerializedFieldAssigner.SetReference(view, "_area", areaRect);
            SerializedFieldAssigner.SetReference(view, "_mark", mark);
            SerializedFieldAssigner.SetReference(view, "_label", label);

            var presenter = area.AddComponent<RecognitionMarkerPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<RecognitionMarkerPresenter>(area, ProjectAssetPaths.RecognitionMarkerPrefab);
        }

        // 네모 아래 가운데에 붙는 글.
        private static Text AddLabel(RectTransform mark)
        {
            RectTransform rect = UiPrimitives.CreateRect("Label", mark);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -DashboardMetrics.RecognitionLabelGap);
            rect.sizeDelta = new Vector2(DashboardMetrics.RecognitionLabelWidth, DashboardMetrics.RecognitionLabelHeight);
            return UiPrimitives.AddText(rect, string.Empty, TextRole.BodyBold, MarkerColor, TextAnchor.MiddleCenter);
        }
    }
}
