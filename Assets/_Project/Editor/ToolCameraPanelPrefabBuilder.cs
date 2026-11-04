using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 툴 카메라 패널 프리팹(PF_ToolCameraPanel)을 만든다. 머리줄(REC 표시), 카메라 그림(RawImage)과 그 위에 겹친 네 모서리 꺾쇠, 조준 원,
    /// 카메라 위치·찾은 물건 수 칩, 인식 네모, 그리고 아래 상태 줄이 모두 계층의 오브젝트로 들어가고,
    /// 화면 코드(ToolCameraPanelView)와 동작 코드(ToolCameraPanelPresenter)가 그 오브젝트들을 연결한다.
    /// 위치는 IMGUI 시절과 같은 자리 표(DemoPanelSlots.Camera)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class ToolCameraPanelPrefabBuilder
    {
        private const string RootName = "PF_ToolCameraPanel";

        // 찾은 물건 자리의 네모 색. 카메라 화면 위에 올라가므로 밝은 색을 쓴다.
        private static readonly Color FoundMarkerColor = Color.green;

        public static ToolCameraPanelPresenter Build()
        {
            DashboardPalette palette = DashboardPalette.Default;

            UiPanelFrame.Frame frame = UiPanelFrame.Build(RootName, DemoPanelSlots.Camera, ToolCameraPanelTexts.Title, ToolCameraPanelTexts.Subtitle);
            UiWidgets.AddChip(frame.Header, "RecChip", ToolCameraPanelTexts.Recording, ChipTone.Danger, hasDot: true);
            RectTransform body = UiPanelFrame.AddColumnBody(frame, DashboardMetrics.BlockGap);

            // 카메라 그림 영역: 높이는 정해져 있고 폭은 카드 폭을 채운다. 그림은 영역을 채우도록 가운데를 잘라 보인다.
            RectTransform imageRect = UiPrimitives.CreateRect("Image", body);
            UiPrimitives.SetLayout(imageRect, preferredHeight: DashboardMetrics.CameraImageHeight);
            var image = imageRect.gameObject.AddComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;

            AddBrackets(imageRect);
            AddCrosshair(imageRect);
            Text positionChip = AddOverlayChip(imageRect, "PositionChip", isRightSide: false);
            Text detectChip = AddOverlayChip(imageRect, "DetectChip", isRightSide: true);
            RectTransform marker = UiWidgets.AddOutlineFrame(
                imageRect, "FoundMarker", DashboardMetrics.CameraMarkerSize, DashboardMetrics.CameraMarkerLine, FoundMarkerColor);
            marker.gameObject.SetActive(false);

            UiStatusLine status = UiWidgets.AddStatusLine(body, "Status");

            frame.Root.AddComponent<CanvasGroup>();
            var view = frame.Root.AddComponent<ToolCameraPanelView>();
            SerializedFieldAssigner.SetReference(view, "_image", image);
            SerializedFieldAssigner.SetReference(view, "_positionChip", positionChip);
            SerializedFieldAssigner.SetReference(view, "_detectChip", detectChip);
            SerializedFieldAssigner.SetReference(view, "_marker", marker);
            SerializedFieldAssigner.SetReference(view, "_status", status);

            var presenter = frame.Root.AddComponent<ToolCameraPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<ToolCameraPanelPresenter>(frame.Root, ProjectAssetPaths.ToolCameraPanelPrefab);
        }

        // 네 모서리에 ㄱ자 꺾쇠.
        private static void AddBrackets(Transform parent)
        {
            Color color = DashboardPalette.Default.CameraFrame;
            RectTransform group = UiPrimitives.CreateRect("Brackets", parent);
            UiPrimitives.Stretch(group);

            AddBracket(group, "TopLeft", new Vector2(0f, 1f), color);
            AddBracket(group, "TopRight", new Vector2(1f, 1f), color);
            AddBracket(group, "BottomLeft", new Vector2(0f, 0f), color);
            AddBracket(group, "BottomRight", new Vector2(1f, 0f), color);
        }

        private static void AddBracket(Transform parent, string name, Vector2 corner, Color color)
        {
            UiWidgets.AddCornerBracket(
                parent, name, corner, DashboardMetrics.CameraBracketInset, DashboardMetrics.CameraBracketLength,
                DashboardMetrics.CameraBracketThickness, color);
        }

        // 화면 가운데의 조준 원과 점.
        private static void AddCrosshair(Transform parent)
        {
            Color color = DashboardPalette.Default.CameraFrame;

            RectTransform ring = UiPrimitives.CreateRect("CrosshairRing", parent);
            ring.anchorMin = new Vector2(0.5f, 0.5f);
            ring.anchorMax = new Vector2(0.5f, 0.5f);
            ring.sizeDelta = Vector2.one * (DashboardMetrics.CameraCrosshairRadius * 2f);
            UiPrimitives.AddRing(ring, color);

            RectTransform dot = UiPrimitives.CreateRect("CrosshairDot", parent);
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = Vector2.one * (DashboardMetrics.CameraCrosshairDotRadius * 2f);
            UiPrimitives.AddCircle(dot, color);
        }

        // 화면 아래쪽 모서리에 붙는 정보 칩. 글 길이에 맞춰 폭이 늘고 줄어든다.
        private static Text AddOverlayChip(Transform parent, string name, bool isRightSide)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform chip = UiPrimitives.CreateRect(name, parent);
            float side = isRightSide ? 1f : 0f;
            chip.anchorMin = new Vector2(side, 0f);
            chip.anchorMax = new Vector2(side, 0f);
            chip.pivot = new Vector2(side, 0f);
            chip.anchoredPosition = new Vector2(isRightSide ? -DashboardMetrics.CameraChipInset : DashboardMetrics.CameraChipInset, DashboardMetrics.CameraChipInset);
            UiPrimitives.AddImage(chip, palette.CameraOverlay, DashboardMetrics.CameraChipRadius);
            int padding = Mathf.RoundToInt(DashboardMetrics.CameraChipPaddingX);
            UiPrimitives.AddRow(chip, 0f, TextAnchor.MiddleCenter, new RectOffset(padding, padding, 0, 0));
            UiPrimitives.SetLayout(chip, preferredHeight: DashboardMetrics.CameraChipHeight);
            var fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return UiPrimitives.AddLabel(chip, "Label", string.Empty, TextRole.Mono, palette.Ink, TextAnchor.MiddleCenter);
        }
    }
}
