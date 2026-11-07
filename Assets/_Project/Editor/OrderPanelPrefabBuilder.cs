using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 주문 패널 프리팹(PF_OrderPanel)을 만든다. 머리줄, 안내 글, 주문 목록, CSV 입력창, 버튼 세 개, 현황 칩, 경고 목록이
    /// 모두 계층의 오브젝트로 들어가고, 화면 코드(OrderPanelView)와 동작 코드(OrderPanelPresenter)가 그 오브젝트들을 연결한다.
    /// 위치는 IMGUI 시절과 같은 자리 표(DemoPanelSlots.Orders)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class OrderPanelPrefabBuilder
    {
        // 주문 목록은 이 줄 수까지 보이고, 넘치면 목록 안에서 스크롤한다. 주문이 많아도 패널 높이가 늘지 않게 한다.
        private const int VisibleOrderRows = 3;

        // 계층에 미리 놓아 두는 줄 수. 실행 중에는 줄을 만들지 않으므로 이 수가 한 번에 보이는 주문과 경고의 최대 수다.
        // 넘치는 개수는 목록 맨 아래에 "외 N건 더"로 알린다.
        private const int OrderRowCapacity = 12;
        private const int IssueRowCapacity = 6;

        // 경고 목록이 차지하는 최대 높이(px). 넘치면 목록 안에서 스크롤한다.
        private const float IssuesMaxHeight = 60f;

        // CSV 입력창의 높이(px).
        private const float CsvAreaHeight = 72f;

        private const string RootName = "PF_OrderPanel";

        public static OrderPanelPresenter Build(OrderRowItem orderRowPrefab, UiTextRow issueRowPrefab)
        {
            DashboardPalette palette = DashboardPalette.Default;

            UiPanelFrame.Frame frame = UiPanelFrame.Build(RootName, DemoPanelSlots.Orders, OrderPanelTexts.Title, OrderPanelTexts.Subtitle);
            Text summary = frame.Summary;
            UiWidgets.ScrollView body = UiPanelFrame.AddScrollBody(frame, DashboardMetrics.RowGap);

            UiPrimitives.AddLabel(body.Content, "Guide", OrderPanelTexts.Guide, TextRole.Small, palette.Muted, TextAnchor.UpperLeft, isWrapped: true);

            float orderListMaxHeight = VisibleOrderRows * OrderRowItemPrefabBuilder.RowHeight
                                       + (VisibleOrderRows - 1) * OrderRowItemPrefabBuilder.RowSpacing;
            UiScrollListSizer orderList = UiWidgets.AddFittedList(
                body.Content, "OrderList", OrderRowItemPrefabBuilder.RowSpacing, orderListMaxHeight, out RectTransform orderRowContainer);
            PlaceRows(orderRowContainer, orderRowPrefab, OrderRowCapacity);
            Text orderOverflow = AddOverflowLabel(orderRowContainer, palette);
            Text emptyNotice = UiPrimitives.AddLabel(body.Content, "EmptyNotice", OrderPanelTexts.EmptyOrders, TextRole.Small, palette.Faint);

            InputField csvInput = UiWidgets.AddMultilineInput(body.Content, "CsvInput", CsvAreaHeight);

            RectTransform buttonRow = UiPrimitives.CreateRect("Buttons", body.Content);
            UiPrimitives.AddRow(buttonRow, DashboardMetrics.BlockGap);
            UiPrimitives.SetLayout(buttonRow, preferredHeight: DashboardMetrics.ControlHeight);
            Button apply = UiWidgets.AddButton(buttonRow, "ApplyButton", OrderPanelTexts.ApplyButton, ButtonKind.Primary, DashboardMetrics.ControlHeight);
            Button sample = UiWidgets.AddButton(buttonRow, "SampleButton", OrderPanelTexts.SampleButton, ButtonKind.Secondary, DashboardMetrics.ControlHeight);
            Button file = UiWidgets.AddButton(buttonRow, "FileButton", OrderPanelTexts.FileButton, ButtonKind.Secondary, DashboardMetrics.ControlHeight);

            RectTransform chipRow = UiPrimitives.CreateRect("Chips", body.Content);
            UiPrimitives.AddRow(chipRow, DashboardMetrics.BlockGap);
            UiPrimitives.SetLayout(chipRow, preferredHeight: DashboardMetrics.ChipHeight);
            UiChip heldChip = UiWidgets.AddChip(chipRow, "HeldChip", OrderPanelTexts.Held(0), ChipTone.Neutral, hasDot: false);
            UiChip pendingChip = UiWidgets.AddChip(chipRow, "PendingChip", OrderPanelTexts.Pending(0), ChipTone.Neutral, hasDot: false);
            UiChip issueChip = UiWidgets.AddChip(chipRow, "IssueChip", OrderPanelTexts.Issues(0), OrderChipTones.ForIssues(0), hasDot: true);

            UiScrollListSizer issueList = UiWidgets.AddFittedList(body.Content, "IssueList", 0f, IssuesMaxHeight, out RectTransform issueRowContainer);
            PlaceRows(issueRowContainer, issueRowPrefab, IssueRowCapacity);
            Text issueOverflow = AddOverflowLabel(issueRowContainer, palette);

            var view = frame.Root.AddComponent<OrderPanelView>();
            SerializedFieldAssigner.SetReference(view, "_summary", summary);
            SerializedFieldAssigner.SetReference(view, "_csvInput", csvInput);
            SerializedFieldAssigner.SetReference(view, "_applyButton", apply);
            SerializedFieldAssigner.SetReference(view, "_sampleButton", sample);
            SerializedFieldAssigner.SetReference(view, "_fileButton", file);
            SerializedFieldAssigner.SetReference(view, "_orderRowContainer", orderRowContainer);
            SerializedFieldAssigner.SetReference(view, "_orderOverflow", orderOverflow);
            SerializedFieldAssigner.SetReference(view, "_orderList", orderList);
            SerializedFieldAssigner.SetReference(view, "_emptyNotice", emptyNotice.gameObject);
            SerializedFieldAssigner.SetReference(view, "_heldChip", heldChip);
            SerializedFieldAssigner.SetReference(view, "_pendingChip", pendingChip);
            SerializedFieldAssigner.SetReference(view, "_issueChip", issueChip);
            SerializedFieldAssigner.SetReference(view, "_issueRowContainer", issueRowContainer);
            SerializedFieldAssigner.SetReference(view, "_issueOverflow", issueOverflow);
            SerializedFieldAssigner.SetReference(view, "_issueList", issueList);

            var presenter = frame.Root.AddComponent<OrderPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<OrderPanelPresenter>(frame.Root, ProjectAssetPaths.OrderPanelPrefab);
        }

        // 줄을 미리 계층에 놓는다. 처음에는 꺼 두고, 실행 중에는 켜고 끄기만 한다(오브젝트를 새로 만들지 않는다).
        private static void PlaceRows(RectTransform container, Component rowPrefab, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var row = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab.gameObject, container);
                row.name = $"{rowPrefab.gameObject.name}_{i + 1:00}";
                row.SetActive(false);
            }
        }

        // 목록 맨 아래에서 칸에 못 들어간 개수를 알리는 글. 넘치지 않으면 꺼 둔다.
        private static Text AddOverflowLabel(RectTransform container, DashboardPalette palette)
        {
            Text label = UiPrimitives.AddLabel(container, "Overflow", OrderPanelTexts.Overflow(0), TextRole.Small, palette.Faint);
            label.gameObject.SetActive(false);
            return label;
        }
    }
}
