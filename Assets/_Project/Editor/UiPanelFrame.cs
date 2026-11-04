using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 패널 프리팹의 겉틀을 만든다: 둥근 카드, 자리 표(DemoPanelSlots)에 맞춘 위치와 크기, 머리줄, 머리줄 아래의 스크롤 본문.
    /// 패널마다 같은 코드를 복사하지 않도록 모든 패널 빌더가 이것으로 시작한다.
    /// </summary>
    public static class UiPanelFrame
    {
        /// <summary>겉틀의 두 부분: 카드(안에 본문을 더하는 자리)와 머리줄 오른쪽 끝 글(요약이나 현재 값을 보여 줄 자리).</summary>
        public sealed class Frame
        {
            public Frame(UiPrimitives.BorderedPanel card, Text summary)
            {
                Card = card;
                Summary = summary;
            }

            public UiPrimitives.BorderedPanel Card { get; }

            public Text Summary { get; }

            public GameObject Root => Card.Root.gameObject;

            /// <summary>머리줄. 요약 글이 머리줄의 자식이므로 그 부모다. 머리줄에 칩이나 접기 표시를 더할 때 쓴다.</summary>
            public RectTransform Header => (RectTransform)Summary.transform.parent;
        }

        public static Frame Build(string rootName, DemoPanelSlot slot, string title, string subtitle)
        {
            DashboardPalette palette = DashboardPalette.Default;

            UiPrimitives.BorderedPanel card = UiPrimitives.AddBorderedPanel(
                null, rootName, palette.Card, palette.CardBorder, DashboardMetrics.CardRadius, hasBorder: true, isRaycastTarget: true);
            DemoPanelRectAnchor.From(slot).ApplyTo(card.Root);

            Text summary = UiPanelHeader.Build(card.Fill, title, subtitle);
            return new Frame(card, summary);
        }

        /// <summary>
        /// 머리줄을 누르면 본문이 접히고 펼쳐지게 한다: 머리줄 끝에 접힘 표시(−, +)를 더하고, 머리줄 전체를 단추로 만들며,
        /// 패널 루트에 UiCollapsiblePanel을 붙여 잇는다. 머리줄에 더한 칩 같은 단추는 그 자리에서 먼저 눌린다.
        /// 접힌 높이는 머리줄 높이이고, 펼친 높이는 프리팹에 정해진 패널 높이이다.
        /// </summary>
        /// <param name="body">접을 때 꺼지는 본문(AddScrollBody나 AddColumnBody가 돌려준 오브젝트).</param>
        public static UiCollapsiblePanel AddCollapse(Frame frame, GameObject body, bool startsCollapsed)
        {
            DashboardPalette palette = DashboardPalette.Default;

            // 머리줄 전체가 눌림을 받도록 보이지 않는 바탕을 깐다.
            Image headerImage = UiPrimitives.AddImage(frame.Header, Color.clear, isRaycastTarget: true);
            var headerButton = frame.Header.gameObject.AddComponent<Button>();
            headerButton.targetGraphic = headerImage;
            headerButton.navigation = new Navigation { mode = Navigation.Mode.None };
            headerButton.transition = Selectable.Transition.None;

            RectTransform toggle = UiPrimitives.CreateRect("Toggle", frame.Header);
            UiPrimitives.AddImage(toggle, palette.Surface, DashboardMetrics.HeaderToggleRadius);
            UiPrimitives.SetLayout(toggle, preferredWidth: DashboardMetrics.HeaderToggleSize, preferredHeight: DashboardMetrics.HeaderToggleSize);
            RectTransform markRect = UiPrimitives.CreateRect("Mark", toggle);
            UiPrimitives.Stretch(markRect);
            Text mark = UiPrimitives.AddText(markRect, string.Empty, TextRole.MonoBold, palette.Muted, TextAnchor.MiddleCenter);

            var collapsible = frame.Root.AddComponent<UiCollapsiblePanel>();
            SerializedFieldAssigner.SetReference(collapsible, "_headerButton", headerButton);
            SerializedFieldAssigner.SetReference(collapsible, "_body", body);
            SerializedFieldAssigner.SetReference(collapsible, "_mark", mark);
            SerializedFieldAssigner.SetBool(collapsible, "_startsCollapsed", startsCollapsed);
            return collapsible;
        }

        /// <summary>머리줄 아래를 채우는 세로 본문. 줄이 정해진 높이로 위에서부터 쌓이고 스크롤하지 않는다(내용이 항상 짧은 패널용).</summary>
        public static RectTransform AddColumnBody(Frame frame, float spacing)
        {
            RectTransform body = UiPrimitives.CreateRect("Body", frame.Card.Fill);
            UiPrimitives.Stretch(
                body, DashboardMetrics.CardPadding, DashboardMetrics.HeaderHeight, DashboardMetrics.CardPadding, DashboardMetrics.CardPadding);
            UiPrimitives.AddColumn(body, spacing);
            return body;
        }

        /// <summary>머리줄 아래를 채우는 세로 스크롤 본문. 내용이 길면 안에서 스크롤한다.</summary>
        public static UiWidgets.ScrollView AddScrollBody(Frame frame, float spacing)
        {
            UiWidgets.ScrollView body = UiWidgets.AddScrollView(frame.Card.Fill, "Body", spacing);
            UiPrimitives.Stretch(
                body.Root, DashboardMetrics.CardPadding, DashboardMetrics.HeaderHeight, DashboardMetrics.CardPadding, DashboardMetrics.CardPadding);
            return body;
        }
    }
}
