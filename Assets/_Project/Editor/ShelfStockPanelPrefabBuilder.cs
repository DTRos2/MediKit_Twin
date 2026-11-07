using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 선반 재고 패널 프리팹(PF_ShelfStockPanel)을 만든다. 스위치 줄 둘, 수동 키팅 버튼 줄, 행마다 칸 타일, 안내 글이 모두 계층의 오브젝트로
    /// 들어가고, 화면 코드(ShelfStockPanelView)와 동작 코드(ShelfStockPanelPresenter)가 그 오브젝트들을 연결한다.
    /// 칸 타일은 씬 선반의 칸 ID마다, 키트 종류 버튼은 레시피의 키트 코드마다 하나씩 만들어지므로 선반이나 레시피가 바뀌면 이 빌더를 다시 돌리면 된다.
    /// 위치는 IMGUI 시절과 같은 자리 표(DemoPanelSlots.Shelf)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class ShelfStockPanelPrefabBuilder
    {
        private const string RootName = "PF_ShelfStockPanel";

        /// <param name="slotIds">선반의 칸 ID 전체(씬의 선반 칸 화면에서 읽는다). 첫 글자가 같은 것끼리 한 행이 된다.</param>
        /// <param name="kitCodes">수동 키팅에서 고를 수 있는 키트 종류 코드들.</param>
        public static ShelfStockPanelPresenter Build(IReadOnlyList<string> slotIds, IReadOnlyList<string> kitCodes)
        {
            DashboardPalette palette = DashboardPalette.Default;

            UiPanelFrame.Frame frame = UiPanelFrame.Build(RootName, DemoPanelSlots.Shelf, ShelfStockPanelTexts.Title, ShelfStockPanelTexts.Subtitle);
            UiWidgets.ScrollView body = UiPanelFrame.AddScrollBody(frame, DashboardMetrics.RowGap);

            UiSwitchRow manualSwitch = UiWidgets.AddSwitchRow(body.Content, "ManualSwitch");
            UiSwitchRow returnSwitch = UiWidgets.AddSwitchRow(body.Content, "ReturnSwitch");

            // 수동 모드에서만 보이는 줄: 키트 종류 버튼들과 새 박스.
            RectTransform operations = UiPrimitives.CreateRect("ManualOperations", body.Content);
            UiPrimitives.AddRow(operations, DashboardMetrics.BlockGap);
            UiPrimitives.SetLayout(operations, preferredHeight: DashboardMetrics.SmallControlHeight);
            foreach (string kitCode in kitCodes)
            {
                AddKitCodeButton(operations, kitCode);
            }

            UiKindButton newBox = UiWidgets.AddKindButton(
                operations, "NewBoxButton", ShelfStockPanelTexts.NewBox, ButtonKind.Secondary, DashboardMetrics.SmallControlHeight);
            operations.gameObject.SetActive(false);

            foreach (ShelfRow row in ShelfRows.Group(slotIds))
            {
                AddRow(body.Content, row);
            }

            Text message = UiPrimitives.AddLabel(
                body.Content, "Message", ShelfStockPanelTexts.Guide, TextRole.Small, palette.Muted, TextAnchor.UpperLeft, isWrapped: true);

            var view = frame.Root.AddComponent<ShelfStockPanelView>();
            SerializedFieldAssigner.SetReference(view, "_total", frame.Summary);
            SerializedFieldAssigner.SetReference(view, "_manualSwitch", manualSwitch);
            SerializedFieldAssigner.SetReference(view, "_returnSwitch", returnSwitch);
            SerializedFieldAssigner.SetReference(view, "_manualOperations", operations.gameObject);
            SerializedFieldAssigner.SetReference(view, "_newBoxButton", newBox);
            SerializedFieldAssigner.SetReference(view, "_message", message);

            var presenter = frame.Root.AddComponent<ShelfStockPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<ShelfStockPanelPresenter>(frame.Root, ProjectAssetPaths.ShelfStockPanelPrefab);
        }

        private static void AddKitCodeButton(Transform parent, string kitCode)
        {
            UiKindButton button = UiWidgets.AddKindButton(parent, $"Kit_{kitCode}", kitCode, ButtonKind.Secondary, DashboardMetrics.SmallControlHeight);
            var kitButton = button.gameObject.AddComponent<ManualKitCodeButton>();
            SerializedFieldAssigner.SetString(kitButton, "_kitCode", kitCode);
            SerializedFieldAssigner.SetReference(kitButton, "_button", button);
        }

        // 한 행: 왼쪽에 행 글자와 키트 종류, 오른쪽에 칸마다 타일.
        private static void AddRow(Transform parent, ShelfRow shelfRow)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect($"ShelfRow_{shelfRow.Letter}", parent);
            UiPrimitives.AddRow(root, DashboardMetrics.RowGap);
            UiPrimitives.SetLayout(root, preferredHeight: DashboardMetrics.StockTileHeight);

            RectTransform label = UiPrimitives.CreateRect("Label", root);
            UiPrimitives.AddColumn(label, 0f);
            UiPrimitives.SetLayout(label, preferredWidth: DashboardMetrics.StockRowLabelWidth);
            label.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Text letterText = UiPrimitives.AddLabel(label, "Letter", shelfRow.Letter.ToString(), TextRole.BodyBold, palette.Muted, TextAnchor.MiddleCenter);
            UiPrimitives.SetLayout((RectTransform)letterText.transform, preferredHeight: DashboardMetrics.StockRowLetterHeight);
            Text kitCodeText = UiPrimitives.AddLabel(label, "KitCode", string.Empty, TextRole.Small, palette.Faint, TextAnchor.MiddleCenter);
            UiPrimitives.SetLayout((RectTransform)kitCodeText.transform, preferredHeight: DashboardMetrics.StockRowKitCodeHeight);

            foreach (string slotId in shelfRow.SlotIds)
            {
                AddTile(root, slotId);
            }

            var item = root.gameObject.AddComponent<ShelfRowItem>();
            SerializedFieldAssigner.SetString(item, "_letter", shelfRow.Letter.ToString());
            SerializedFieldAssigner.SetReference(item, "_letterText", letterText);
            SerializedFieldAssigner.SetReference(item, "_kitCodeText", kitCodeText);
        }

        // 칸 하나: 바탕(눌러 볼 수 있다), 위쪽 색띠, 칸 이름, 큰 재고 숫자와 단위, 귀퉁이의 보충 남은 시간.
        private static void AddTile(Transform parent, string slotId)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect($"Tile_{slotId}", parent);
            Image background = UiPrimitives.AddImage(root, Color.white, DashboardMetrics.StockTileRadius, isRaycastTarget: true);
            UiPrimitives.SetLayout(root, preferredWidth: 0f, preferredHeight: DashboardMetrics.StockTileHeight, flexibleWidth: 1f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.transition = Selectable.Transition.ColorTint;
            button.colors = UiButtonColors.FromBackground(palette.Surface, palette);

            RectTransform stripRect = UiPrimitives.CreateRect("Strip", root);
            stripRect.anchorMin = new Vector2(0f, 1f);
            stripRect.anchorMax = new Vector2(1f, 1f);
            stripRect.pivot = new Vector2(0.5f, 1f);
            stripRect.offsetMin = new Vector2(DashboardMetrics.StockTileStripInset, -DashboardMetrics.StockTileStripHeight);
            stripRect.offsetMax = new Vector2(-DashboardMetrics.StockTileStripInset, 0f);
            Image strip = UiPrimitives.AddImage(stripRect, palette.Track, DashboardMetrics.StockTileStripHeight * 0.5f);

            float inset = DashboardMetrics.StockTileTextInset;
            Text id = UiWidgets.AddTopLine(
                root, "Id", TextRole.Mono, palette.Muted, DashboardMetrics.StockTileIdTop, DashboardMetrics.StockTileIdHeight, inset, inset);
            Text note = UiWidgets.AddTopLine(
                root, "RefillNote", TextRole.Small, palette.Warning, DashboardMetrics.StockTileIdTop, DashboardMetrics.StockTileIdHeight, inset, inset,
                TextAnchor.MiddleRight);
            note.gameObject.SetActive(false);

            RectTransform quantityLine = UiPrimitives.CreateRect("QuantityLine", root);
            quantityLine.anchorMin = new Vector2(0f, 1f);
            quantityLine.anchorMax = new Vector2(1f, 1f);
            quantityLine.pivot = new Vector2(0.5f, 1f);
            quantityLine.offsetMin = new Vector2(inset, -(DashboardMetrics.StockTileQuantityTop + DashboardMetrics.StockTileQuantityHeight));
            quantityLine.offsetMax = new Vector2(-inset, -DashboardMetrics.StockTileQuantityTop);
            UiPrimitives.AddRow(quantityLine, DashboardMetrics.StockTileUnitGap, TextAnchor.LowerLeft);

            Text quantity = UiPrimitives.AddLabel(quantityLine, "Quantity", "0", TextRole.MonoBold, palette.Ink, TextAnchor.LowerLeft);
            quantity.fontSize = DashboardMetrics.StockTileQuantityFontSize;
            Text unit = UiPrimitives.AddLabel(quantityLine, "Unit", ShelfStockPanelTexts.Unit, TextRole.Small, palette.Muted, TextAnchor.LowerLeft);

            var tile = root.gameObject.AddComponent<ShelfSlotTile>();
            SerializedFieldAssigner.SetString(tile, "_slotId", slotId);
            SerializedFieldAssigner.SetReference(tile, "_button", button);
            SerializedFieldAssigner.SetReference(tile, "_strip", strip);
            SerializedFieldAssigner.SetReference(tile, "_id", id);
            SerializedFieldAssigner.SetReference(tile, "_quantity", quantity);
            SerializedFieldAssigner.SetReference(tile, "_unit", unit);
            SerializedFieldAssigner.SetReference(tile, "_note", note);
        }
    }
}
