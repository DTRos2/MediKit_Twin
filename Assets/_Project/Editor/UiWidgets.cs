using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 대시보드 모양의 uGUI 위젯(버튼, 입력창, 스크롤 목록, 칩)을 만든다. IMGUI 위젯과 같은 색 규칙(ButtonColors, ChipColors, InteractionShade)과
    /// 치수(DashboardMetrics)를 쓰므로 두 화면의 위젯이 같은 모양이다. 도메인(주문 등)은 모르고 위젯 하나하나를 만들어 돌려줄 뿐이다.
    /// </summary>
    public static class UiWidgets
    {
        // 마우스 휠 한 칸이 움직이는 거리. 입력 시스템이 휠 한 칸을 작은 값으로 알려 주므로 기본값 1로는 거의 움직이지 않는다.
        private const float WheelSensitivity = 24f;

        // 입력창에서 글자를 끌어 고를 때 보이는 선택 영역의 투명도. 글자가 가려지지 않을 만큼만 비친다.
        private const float SelectionAlpha = 0.35f;

        // 칩 앞 점의 크기와 점·글자 사이 간격.
        private const float ChipDotSize = 6f;
        private const float ChipDotGap = 6f;

        /// <summary>스크롤 영역의 세 부분: 바깥 틀, 보이는 창, 줄이 쌓이는 내용.</summary>
        public sealed class ScrollView
        {
            public ScrollView(RectTransform root, RectTransform content)
            {
                Root = root;
                Content = content;
            }

            public RectTransform Root { get; }

            public RectTransform Content { get; }
        }

        /// <summary>줄이 쌓이는 세로 스크롤 영역. 내용이 창보다 길면 안에서 스크롤한다.</summary>
        public static ScrollView AddScrollView(Transform parent, string name, float spacing)
        {
            RectTransform root = UiPrimitives.CreateRect(name, parent);

            RectTransform viewport = UiPrimitives.CreateRect("Viewport", root);
            UiPrimitives.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            // 끌어서 스크롤하려면 눌림을 받는 그래픽이 있어야 하므로 보이지 않는 이미지를 깐다.
            UiPrimitives.AddImage(viewport, Color.clear, isRaycastTarget: true);

            RectTransform content = UiPrimitives.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            UiPrimitives.AddColumn(content, spacing);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = WheelSensitivity;
            return new ScrollView(root, content);
        }

        /// <summary>
        /// 높이가 내용에 맞고 최대 높이를 넘지 않는 스크롤 목록. 줄이 적으면 그만큼만 차지하고 많으면 안에서 스크롤한다.
        /// 내용이 바뀐 뒤 돌려준 UiScrollListSizer의 Refresh를 부르면 높이가 다시 맞춰진다.
        /// </summary>
        public static UiScrollListSizer AddFittedList(Transform parent, string name, float spacing, float maxHeight, out RectTransform content)
        {
            ScrollView list = AddScrollView(parent, name, spacing);
            content = list.Content;

            LayoutElement frame = UiPrimitives.SetLayout(list.Root, preferredHeight: maxHeight);
            var sizer = list.Root.gameObject.AddComponent<UiScrollListSizer>();
            SerializedFieldAssigner.SetReference(sizer, "_content", list.Content);
            SerializedFieldAssigner.SetReference(sizer, "_frame", frame);
            SerializedFieldAssigner.SetFloat(sizer, "_maxHeight", maxHeight);
            return sizer;
        }

        /// <summary>
        /// 둥근 버튼. 종류별 색은 IMGUI 버튼과 같은 ButtonColors가 정하고, 마우스를 올리거나 누를 때의 색은 InteractionShade가 정한다.
        /// 바탕 그림은 흰색으로 두고 상태별 실제 색은 버튼의 색 블록에 담는다(색 블록은 그림 색에 곱해지기 때문이다).
        /// </summary>
        public static Button AddButton(Transform parent, string name, string label, ButtonKind kind, float height)
        {
            return AddKindButton(parent, name, label, kind, height).Button;
        }

        /// <summary>
        /// AddButton과 같은 버튼이되, 실행 중에 종류를 바꿀 수 있게 UiKindButton이 붙어 있다(켜진 토글처럼 보이게 하거나 눌리지 않게 할 때 쓴다).
        /// 처음 모양은 AddButton과 같다.
        /// </summary>
        public static UiKindButton AddKindButton(Transform parent, string name, string label, ButtonKind kind, float height)
        {
            DashboardPalette palette = DashboardPalette.Default;
            ButtonColors.Resolve(kind, palette, out Color background, out Color foreground, out Color border, out bool hasBorder);

            UiPrimitives.BorderedPanel panel = UiPrimitives.AddBorderedPanel(
                parent, name, Color.white, hasBorder ? border : background, DashboardMetrics.RowRadius, hasBorder, isRaycastTarget: true);
            UiPrimitives.SetLayout(panel.Root, preferredHeight: height, flexibleWidth: 1f);

            var button = panel.Fill.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.FillImage;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.transition = Selectable.Transition.ColorTint;
            button.colors = UiButtonColors.For(kind, palette);

            var textRect = UiPrimitives.CreateRect("Label", panel.Fill);
            UiPrimitives.Stretch(textRect);
            TextRole role = kind == ButtonKind.Primary ? TextRole.BodyBold : TextRole.Body;
            Text labelText = UiPrimitives.AddText(textRect, label, role, foreground, TextAnchor.MiddleCenter);

            var kindButton = panel.Root.gameObject.AddComponent<UiKindButton>();
            SerializedFieldAssigner.SetReference(kindButton, "_button", button);
            SerializedFieldAssigner.SetReference(kindButton, "_border", hasBorder ? panel.Root.GetComponent<Image>() : null);
            SerializedFieldAssigner.SetReference(kindButton, "_label", labelText);
            return kindButton;
        }

        /// <summary>여러 줄을 적는 입력창. 흰 바탕 대신 어두운 입력 바탕을 쓰고, 글이 넘치면 줄 단위로 스크롤한다.</summary>
        public static InputField AddMultilineInput(Transform parent, string name, float height)
        {
            return AddInput(parent, name, height, InputField.LineType.MultiLineNewline, isWrapped: true, out _);
        }

        // 입력창의 공통 틀: 어두운 입력 바탕, 안쪽 여백, 글, 커서와 선택 색. 줄 수와 줄바꿈 여부만 다르다.
        private static InputField AddInput(
            Transform parent, string name, float height, InputField.LineType lineType, bool isWrapped, out RectTransform root)
        {
            DashboardPalette palette = DashboardPalette.Default;
            UiPrimitives.BorderedPanel panel = UiPrimitives.AddBorderedPanel(
                parent, name, palette.Field, palette.SurfaceBorder, DashboardMetrics.RowRadius, hasBorder: true, isRaycastTarget: true);
            UiPrimitives.SetLayout(panel.Root, preferredHeight: height);
            root = panel.Root;

            RectTransform textArea = UiPrimitives.CreateRect("Text Area", panel.Fill);
            UiPrimitives.Stretch(
                textArea, DashboardMetrics.FieldPaddingX, DashboardMetrics.FieldPaddingY, DashboardMetrics.FieldPaddingX, DashboardMetrics.FieldPaddingY);
            textArea.gameObject.AddComponent<RectMask2D>();

            RectTransform textRect = UiPrimitives.CreateRect("Text", textArea);
            UiPrimitives.Stretch(textRect);
            TextAnchor anchor = isWrapped ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            Text text = UiPrimitives.AddText(textRect, string.Empty, TextRole.Mono, palette.Ink, anchor, isWrapped);

            // 입력창은 넘친 줄을 잘라 내야 위아래로 스크롤하며 커서를 보여 준다.
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var input = panel.Fill.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.targetGraphic = panel.FillImage;
            input.lineType = lineType;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            input.transition = Selectable.Transition.None;
            input.customCaretColor = true;
            input.caretColor = palette.Accent;
            input.selectionColor = new Color(palette.Accent.r, palette.Accent.g, palette.Accent.b, SelectionAlpha);
            return input;
        }

        /// <summary>
        /// 값 조절 한 줄: 이름, − 버튼, 슬라이더, + 버튼, 값 글자. 범위와 한 번에 움직이는 크기는 쓰는 쪽이 실행 중에 UiSliderRow.SetRange로 정한다.
        /// </summary>
        public static UiSliderRow AddSliderRow(Transform parent, string name, string label)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect(name, parent);
            UiPrimitives.AddRow(root, DashboardMetrics.SliderGap);
            UiPrimitives.SetLayout(root, preferredHeight: DashboardMetrics.SliderHeight);
            root.gameObject.AddComponent<CanvasGroup>();

            Text nameText = UiPrimitives.AddLabel(
                root, "Name", label, TextRole.MonoBold, palette.Accent, preferredWidth: DashboardMetrics.SliderNameWidth);
            Button minus = AddIconButton(root, "Minus", "−");
            Slider slider = AddSlider(root, "Slider");
            Button plus = AddIconButton(root, "Plus", "+");
            Text valueText = UiPrimitives.AddLabel(
                root, "Value", string.Empty, TextRole.Mono, palette.InkSoft, TextAnchor.MiddleRight, preferredWidth: DashboardMetrics.SliderValueWidth);

            var row = root.gameObject.AddComponent<UiSliderRow>();
            SerializedFieldAssigner.SetReference(row, "_name", nameText);
            SerializedFieldAssigner.SetReference(row, "_minusButton", minus);
            SerializedFieldAssigner.SetReference(row, "_slider", slider);
            SerializedFieldAssigner.SetReference(row, "_plusButton", plus);
            SerializedFieldAssigner.SetReference(row, "_valueText", valueText);
            return row;
        }

        /// <summary>
        /// 제목과 설명이 왼쪽에, 켜고 끄는 스위치가 오른쪽에 있는 한 줄. 줄 어디를 눌러도 바뀐다.
        /// 제목과 설명 글은 쓰는 쪽이 실행 중에 UiSwitchRow.SetTexts로 정한다.
        /// </summary>
        public static UiSwitchRow AddSwitchRow(Transform parent, string name)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect(name, parent);
            Image background = UiPrimitives.AddImage(root, Color.white, DashboardMetrics.RowRadius, isRaycastTarget: true);
            UiPrimitives.SetLayout(root, preferredHeight: DashboardMetrics.SwitchRowHeight);

            // 바탕 그림은 흰색으로 두고 상태별 실제 색은 버튼의 색 블록에 담는다(색 블록은 그림 색에 곱해지기 때문이다).
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.transition = Selectable.Transition.ColorTint;
            button.colors = UiButtonColors.FromBackground(palette.Surface, palette);

            float textRight = DashboardMetrics.SwitchRowPaddingX + DashboardMetrics.SwitchWidth + DashboardMetrics.BlockGap;
            Text title = AddTopLine(
                root, "Title", TextRole.BodyBold, palette.Ink,
                DashboardMetrics.SwitchRowTitleTop, DashboardMetrics.SwitchRowTitleHeight, DashboardMetrics.SwitchRowPaddingX, textRight);
            Text description = AddTopLine(
                root, "Description", TextRole.Small, palette.Muted,
                DashboardMetrics.SwitchRowDescriptionTop, DashboardMetrics.SwitchRowDescriptionHeight, DashboardMetrics.SwitchRowPaddingX, textRight);

            RectTransform trackRect = UiPrimitives.CreateRect("Track", root);
            trackRect.anchorMin = new Vector2(1f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(1f, 0.5f);
            trackRect.anchoredPosition = new Vector2(-DashboardMetrics.SwitchRowPaddingX, 0f);
            trackRect.sizeDelta = new Vector2(DashboardMetrics.SwitchWidth, DashboardMetrics.SwitchHeight);
            Image track = UiPrimitives.AddImage(trackRect, palette.Track, DashboardMetrics.SwitchHeight * 0.5f);

            float diameter = DashboardMetrics.SwitchHeight - DashboardMetrics.SwitchThumbInset * 2f;
            RectTransform thumb = UiPrimitives.CreateRect("Thumb", trackRect);
            thumb.sizeDelta = new Vector2(diameter, diameter);
            UiPrimitives.AddCircle(thumb, palette.Thumb);

            var row = root.gameObject.AddComponent<UiSwitchRow>();
            SerializedFieldAssigner.SetReference(row, "_button", button);
            SerializedFieldAssigner.SetReference(row, "_title", title);
            SerializedFieldAssigner.SetReference(row, "_description", description);
            SerializedFieldAssigner.SetReference(row, "_track", track);
            SerializedFieldAssigner.SetReference(row, "_thumb", thumb);
            return row;
        }

        /// <summary>
        /// 부모의 위쪽에서 top만큼 내려온 자리에 놓이는 한 줄 글. 가로는 부모를 채우되 양쪽에서 left, right만큼 들인다.
        /// 눌림을 받는 틀(버튼, 타일) 위에 글을 얹을 때 쓴다.
        /// </summary>
        public static Text AddTopLine(
            Transform parent, string name, TextRole role, Color color, float top, float height, float left, float right,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            RectTransform rect = UiPrimitives.CreateRect(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
            return UiPrimitives.AddText(rect, string.Empty, role, color, anchor);
        }

        // 정사각형 작은 버튼(−, +). 일반 버튼과 같은 모양이되 폭이 고정이다.
        private static Button AddIconButton(Transform parent, string name, string mark)
        {
            UiKindButton button = AddKindButton(parent, name, mark, ButtonKind.Secondary, DashboardMetrics.IconButtonSize);
            UiPrimitives.SetLayout(
                (RectTransform)button.transform,
                preferredWidth: DashboardMetrics.IconButtonSize, preferredHeight: DashboardMetrics.IconButtonSize, flexibleWidth: 0f);
            return button.Button;
        }

        // 가는 막대와 둥근 손잡이가 있는 슬라이더. 칸 전체가 눌림을 받아 막대 어디를 눌러도 그 값으로 가고 끌 수 있다.
        private static Slider AddSlider(Transform parent, string name)
        {
            DashboardPalette palette = DashboardPalette.Default;
            float thumbSize = DashboardMetrics.SliderThumbSize;

            RectTransform root = UiPrimitives.CreateRect(name, parent);
            UiPrimitives.SetLayout(root, preferredHeight: DashboardMetrics.SliderHeight, flexibleWidth: 1f);
            UiPrimitives.AddImage(root, Color.clear, isRaycastTarget: true);

            RectTransform track = UiPrimitives.CreateRect("Track", root);
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, DashboardMetrics.SliderTrackHeight);
            UiPrimitives.AddImage(track, palette.Track, DashboardMetrics.SliderTrackHeight * 0.5f);

            // 손잡이 가운데가 움직이는 구간. 손잡이가 막대 밖으로 삐져나가지 않게 양끝을 손잡이 반지름만큼 뺀다.
            RectTransform handleArea = UiPrimitives.CreateRect("HandleArea", root);
            UiPrimitives.Stretch(handleArea, thumbSize * 0.5f, 0f, thumbSize * 0.5f, 0f);

            RectTransform handle = UiPrimitives.CreateRect("Handle", handleArea);
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(0f, 0.5f);
            handle.sizeDelta = new Vector2(thumbSize, thumbSize);
            Image ring = UiPrimitives.AddCircle(handle, palette.AccentThumb);
            ring.raycastTarget = true;

            RectTransform inner = UiPrimitives.CreateRect("Inner", handle);
            UiPrimitives.Stretch(inner, DashboardMetrics.SliderThumbBorder, DashboardMetrics.SliderThumbBorder, DashboardMetrics.SliderThumbBorder, DashboardMetrics.SliderThumbBorder);
            UiPrimitives.AddCircle(inner, palette.Thumb);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.handleRect = handle;
            slider.targetGraphic = ring;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.wholeNumbers = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        /// <summary>
        /// 이름표와 큰 숫자가 든 칸. 가로는 남는 폭을 같게 나눠 갖는다. 숫자 색은 만들 때 정하고, 실행 중에는 UiStatTile이 숫자만 바꾼다.
        /// </summary>
        public static UiStatTile AddStatTile(Transform parent, string name, string caption, Color valueColor)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect(name, parent);
            UiPrimitives.AddImage(root, palette.Surface, DashboardMetrics.StatTileRadius);
            UiPrimitives.AddColumn(
                root, 0f,
                new RectOffset(
                    Mathf.RoundToInt(DashboardMetrics.StatTilePaddingX), Mathf.RoundToInt(DashboardMetrics.StatTilePaddingX),
                    Mathf.RoundToInt(DashboardMetrics.StatTilePaddingTop), Mathf.RoundToInt(DashboardMetrics.StatTilePaddingBottom)));
            UiPrimitives.SetLayout(root, preferredWidth: 0f, preferredHeight: DashboardMetrics.StatTileHeight, flexibleWidth: 1f);

            RectTransform captionRect = UiPrimitives.CreateRect("Caption", root);
            Text captionText = UiPrimitives.AddText(captionRect, caption, TextRole.Small, palette.Muted);
            UiPrimitives.SetLayout(captionRect, preferredHeight: DashboardMetrics.StatTileCaptionHeight);

            RectTransform valueRect = UiPrimitives.CreateRect("Value", root);
            Text valueText = UiPrimitives.AddText(valueRect, "0", TextRole.BigNumber, valueColor);
            UiPrimitives.SetLayout(valueRect, preferredHeight: DashboardMetrics.StatTileValueHeight);

            var tile = root.gameObject.AddComponent<UiStatTile>();
            SerializedFieldAssigner.SetReference(tile, "_caption", captionText);
            SerializedFieldAssigner.SetReference(tile, "_value", valueText);
            return tile;
        }

        /// <summary>
        /// 패널 안에서 구간을 나누는 제목: 위에 가는 선을 긋고 그 아래에 굵은 작은 글씨를 쓴다. 돌려주는 영역 아래에 그 구간의 내용을 더한다.
        /// 제목이 비어 있으면 선만 긋는다(내용이 스스로 무엇인지 말하는 구간).
        /// </summary>
        public static RectTransform AddSection(Transform parent, string name, string heading, float spacing)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform section = UiPrimitives.CreateRect(name, parent);
            int topGap = Mathf.RoundToInt(DashboardMetrics.SectionGap - DashboardMetrics.BlockGap);
            UiPrimitives.AddColumn(section, spacing, new RectOffset(0, 0, topGap, 0));

            RectTransform divider = UiPrimitives.CreateRect("Divider", section);
            UiPrimitives.AddImage(divider, palette.SurfaceBorder);
            UiPrimitives.SetLayout(divider, preferredHeight: DashboardMetrics.DividerHeight);

            if (!string.IsNullOrEmpty(heading))
            {
                UiPrimitives.AddLabel(section, "Heading", heading, TextRole.BodyBold, palette.InkSoft);
            }

            return section;
        }

        /// <summary>
        /// 글 길이에 맞는 작은 알약 표시. 글과 색은 UiChip이 정하고, 여기서는 틀과 연결만 만든다.
        /// isClickable이면 칩 전체를 덮는 버튼이 붙어 눌림을 UiChip.Clicked로 알린다(실행 중에는 UiChip.SetClickable로 막고 풀 수 있다).
        /// </summary>
        public static UiChip AddChip(Transform parent, string name, string initialText, ChipTone tone, bool hasDot, bool isClickable = false)
        {
            ChipColors.Resolve(tone, DashboardPalette.Default, out Color background, out Color foreground);

            RectTransform root = UiPrimitives.CreateRect(name, parent);
            Image backgroundImage = UiPrimitives.AddImage(root, background, DashboardMetrics.ChipRadius);
            int sidePadding = Mathf.RoundToInt(DashboardMetrics.ChipPaddingX);
            UiPrimitives.AddRow(root, ChipDotGap, TextAnchor.MiddleLeft, new RectOffset(sidePadding, sidePadding, 0, 0));
            UiPrimitives.SetLayout(root, preferredHeight: DashboardMetrics.ChipHeight);

            RectTransform dotRect = UiPrimitives.CreateRect("Dot", root);
            Image dot = UiPrimitives.AddCircle(dotRect, foreground);
            UiPrimitives.SetLayout(dotRect, preferredWidth: ChipDotSize, preferredHeight: ChipDotSize);
            dotRect.gameObject.SetActive(hasDot);

            Text label = UiPrimitives.AddLabel(root, "Label", initialText, TextRole.Small, foreground);

            Button button = null;
            if (isClickable)
            {
                // 칩 색은 실행 중에 UiChip이 바꾸므로 버튼은 그 색에 밝기만 곱한다(흰색 바탕 위에 색 블록이 아니다).
                backgroundImage.raycastTarget = true;
                button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = backgroundImage;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.transition = Selectable.Transition.ColorTint;
                button.colors = UiButtonColors.Shaded(DashboardMetrics.ChipHoverShade, DashboardMetrics.ChipPressedShade);
            }

            var chip = root.gameObject.AddComponent<UiChip>();
            SerializedFieldAssigner.SetReference(chip, "_background", backgroundImage);
            SerializedFieldAssigner.SetReference(chip, "_dot", dot);
            SerializedFieldAssigner.SetReference(chip, "_label", label);
            SerializedFieldAssigner.SetReference(chip, "_button", button);
            return chip;
        }

        /// <summary>
        /// 머리줄 안에 놓는 작은 단추(복사, 지우기). 일반 버튼과 같은 모양이되 글에 맞춘 고정 폭이고 머리줄 높이 안에 들어간다.
        /// 머리줄의 요약 글이 남는 폭을 차지하므로 이 단추는 늘어나지 않는다.
        /// </summary>
        public static UiKindButton AddHeaderButton(Transform header, string name, string label, ButtonKind kind, float width)
        {
            UiKindButton button = AddKindButton(header, name, label, kind, DashboardMetrics.SmallControlHeight);
            UiPrimitives.SetLayout(
                (RectTransform)button.transform,
                preferredWidth: width, preferredHeight: DashboardMetrics.SmallControlHeight, flexibleWidth: 0f);
            return button;
        }

        /// <summary>
        /// 테두리만 있는 작은 태그(로그 줄의 READY …). 안쪽 바탕은 입력칸과 같은 어두운 색이고, 테두리와 글 색은 실행 중에 UiTag가 정한다.
        /// 폭은 고정이라 여러 줄에 놓아도 가지런하다.
        /// </summary>
        public static UiTag AddTag(Transform parent, string name, string initialText)
        {
            DashboardPalette palette = DashboardPalette.Default;

            UiPrimitives.BorderedPanel panel = UiPrimitives.AddBorderedPanel(
                parent, name, palette.Field, palette.Faint, DashboardMetrics.TagRadius, hasBorder: true, isRaycastTarget: false);
            UiPrimitives.SetLayout(panel.Root, preferredWidth: DashboardMetrics.TagWidth, preferredHeight: DashboardMetrics.TagHeight);

            RectTransform labelRect = UiPrimitives.CreateRect("Label", panel.Fill);
            UiPrimitives.Stretch(labelRect);
            Text label = UiPrimitives.AddText(labelRect, initialText, TextRole.MonoBold, palette.Muted, TextAnchor.MiddleCenter);

            var tag = panel.Root.gameObject.AddComponent<UiTag>();
            SerializedFieldAssigner.SetReference(tag, "_border", panel.Root.GetComponent<Image>());
            SerializedFieldAssigner.SetReference(tag, "_label", label);
            return tag;
        }

        /// <summary>
        /// 색 점과 한 줄 글로 된 상태 줄. 점과 글의 색은 실행 중에 UiStatusLine이 정한다.
        /// </summary>
        public static UiStatusLine AddStatusLine(Transform parent, string name)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform root = UiPrimitives.CreateRect(name, parent);
            UiPrimitives.AddRow(root, DashboardMetrics.StatusDotGap);
            UiPrimitives.SetLayout(root, preferredHeight: DashboardMetrics.StatusLineHeight);

            RectTransform dotRect = UiPrimitives.CreateRect("Dot", root);
            Image dot = UiPrimitives.AddCircle(dotRect, palette.Faint);
            UiPrimitives.SetLayout(dotRect, preferredWidth: DashboardMetrics.StatusDotSize, preferredHeight: DashboardMetrics.StatusDotSize);

            Text label = UiPrimitives.AddLabel(
                root, "Label", string.Empty, TextRole.Small, palette.Muted, preferredWidth: 0f, flexibleWidth: 1f);

            var line = root.gameObject.AddComponent<UiStatusLine>();
            SerializedFieldAssigner.SetReference(line, "_dot", dot);
            SerializedFieldAssigner.SetReference(line, "_label", label);
            return line;
        }

        /// <summary>
        /// 가운데가 빈 네모 테두리(인식한 물건 자리 표시). 네 변을 가는 막대로 그리며, 크기는 size×size이고 부모 가운데에 놓인다.
        /// 눌림은 받지 않는다. 켜고 끄는 것과 움직이는 것은 쓰는 쪽이 이 영역으로 한다.
        /// </summary>
        public static RectTransform AddOutlineFrame(Transform parent, string name, float size, float line, Color color)
        {
            RectTransform frame = UiPrimitives.CreateRect(name, parent);
            frame.anchorMin = new Vector2(0.5f, 0.5f);
            frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(size, size);

            AddBar(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -line), Vector2.zero, color);
            AddBar(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, line), color);
            AddBar(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(line, 0f), color);
            AddBar(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-line, 0f), Vector2.zero, color);
            return frame;
        }

        /// <summary>
        /// 부모의 한 모서리에 붙은 ㄱ자 꺾쇠. corner의 x, y가 각각 0이면 왼쪽·아래, 1이면 오른쪽·위 모서리이다. 안쪽으로 inset만큼 띄운다.
        /// </summary>
        public static RectTransform AddCornerBracket(
            Transform parent, string name, Vector2 corner, float inset, float length, float thickness, Color color)
        {
            RectTransform bracket = UiPrimitives.CreateRect(name, parent);
            bracket.anchorMin = corner;
            bracket.anchorMax = corner;
            bracket.pivot = corner;
            float insetX = corner.x > 0.5f ? -inset : inset;
            float insetY = corner.y > 0.5f ? -inset : inset;
            bracket.anchoredPosition = new Vector2(insetX, insetY);
            bracket.sizeDelta = new Vector2(length, length);

            // 가로 막대는 꼭짓점이 있는 쪽 가장자리에, 세로 막대는 꼭짓점이 있는 쪽 옆 가장자리에 붙는다.
            float horizontalY = corner.y;
            float verticalX = corner.x;
            AddBar(bracket, "Horizontal", new Vector2(0f, horizontalY), new Vector2(1f, horizontalY), BarOffsetMin(horizontalY, thickness), BarOffsetMax(horizontalY, thickness), color);
            AddBar(bracket, "Vertical", new Vector2(verticalX, 0f), new Vector2(verticalX, 1f), BarOffsetMinX(verticalX, thickness), BarOffsetMaxX(verticalX, thickness), color);
            return bracket;
        }

        // 위쪽 모서리(1)면 막대가 가장자리에서 아래로, 아래쪽(0)이면 위로 thickness만큼 두껍다.
        private static Vector2 BarOffsetMin(float edge, float thickness) => new Vector2(0f, edge > 0.5f ? -thickness : 0f);

        private static Vector2 BarOffsetMax(float edge, float thickness) => new Vector2(0f, edge > 0.5f ? 0f : thickness);

        private static Vector2 BarOffsetMinX(float edge, float thickness) => new Vector2(edge > 0.5f ? -thickness : 0f, 0f);

        private static Vector2 BarOffsetMaxX(float edge, float thickness) => new Vector2(edge > 0.5f ? 0f : thickness, 0f);

        // 부모의 앵커 띠에 맞춰 늘어나는 색 막대 하나.
        private static void AddBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            RectTransform bar = UiPrimitives.CreateRect(name, parent);
            bar.anchorMin = anchorMin;
            bar.anchorMax = anchorMax;
            bar.offsetMin = offsetMin;
            bar.offsetMax = offsetMax;
            UiPrimitives.AddImage(bar, color);
        }

        /// <summary>
        /// 키 안내 한 쌍: 키 모양 칸(R-Shift, Enter)과 그 키가 하는 일. 키 칸의 가로는 글자 길이에 맞고, 둘은 가로로 나란히 놓인다.
        /// 글은 만들 때 정해지는 고정 안내이므로 실행 중에 바꾸는 컴포넌트는 붙이지 않는다.
        /// </summary>
        public static RectTransform AddKeyHint(Transform parent, string key, string meaning)
        {
            DashboardPalette palette = DashboardPalette.Default;

            RectTransform pair = UiPrimitives.CreateRect($"Hint_{key}", parent);
            UiPrimitives.AddRow(pair, DashboardMetrics.KeyToMeaningGap);
            UiPrimitives.SetLayout(pair, preferredHeight: DashboardMetrics.KeyCapHeight);

            UiPrimitives.BorderedPanel cap = UiPrimitives.AddBorderedPanel(
                pair, "KeyCap", palette.Surface, palette.SurfaceBorder, DashboardMetrics.KeyCapRadius, hasBorder: true, isRaycastTarget: false);
            int inset = Mathf.RoundToInt(DashboardMetrics.BorderWidth);
            UiPrimitives.AddRow(cap.Root, 0f, TextAnchor.MiddleCenter, new RectOffset(inset, inset, inset, inset));
            UiPrimitives.SetLayout(cap.Root, preferredHeight: DashboardMetrics.KeyCapHeight);
            int padding = Mathf.RoundToInt(DashboardMetrics.KeyCapPaddingX);
            UiPrimitives.AddRow(cap.Fill, 0f, TextAnchor.MiddleCenter, new RectOffset(padding, padding, 0, 0));
            UiPrimitives.SetLayout(cap.Fill, flexibleHeight: 1f);
            UiPrimitives.AddLabel(cap.Fill, "Key", key, TextRole.Mono, palette.InkSoft, TextAnchor.MiddleCenter);

            UiPrimitives.AddLabel(pair, "Meaning", meaning, TextRole.Small, palette.Muted);
            return pair;
        }

        /// <summary>한 줄 글 입력칸(칸 ID처럼 짧은 글). 정해진 폭과 높이이며, 글자 수 제한은 쓰는 쪽이 정한다.</summary>
        public static InputField AddSingleLineInput(Transform parent, string name, float width, float height)
        {
            InputField input = AddInput(parent, name, height, InputField.LineType.SingleLine, isWrapped: false, out RectTransform root);
            UiPrimitives.SetLayout(root, preferredWidth: width, preferredHeight: height);
            return input;
        }
    }
}
