using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// uGUI 오브젝트를 만드는 가장 작은 부품들(영역, 이미지, 글자, 배치 그룹). 색과 크기는 모두 인자로 받고,
    /// 부르는 쪽이 DashboardPalette와 DashboardMetrics에서 가져온다. 어떤 패널을 만들든 같은 부품을 쓴다.
    /// 만든 결과는 프리팹으로 저장되어 계층에 그대로 남으므로, 나중에 Inspector에서 손으로 고칠 수 있다.
    /// </summary>
    public static class UiPrimitives
    {
        /// <summary>LayoutElement가 "정하지 않음"으로 읽는 값.</summary>
        public const float NotSet = -1f;

        private const string UiLayerName = "UI";
        private const string RoundedSpritePath = "UI/Skin/UISprite.psd";
        private const string CircleSpritePath = "UI/Skin/Knob.psd";
        private const string FillName = "Fill";

        /// <summary>바탕과 테두리로 이루어진 둥근 틀. 안에 넣을 것은 Fill 아래에 둔다.</summary>
        public sealed class BorderedPanel
        {
            public BorderedPanel(RectTransform root, RectTransform fill, Image fillImage)
            {
                Root = root;
                Fill = fill;
                FillImage = fillImage;
            }

            public RectTransform Root { get; }

            /// <summary>테두리 안쪽 바탕. 테두리가 없으면 Root와 같다.</summary>
            public RectTransform Fill { get; }

            public Image FillImage { get; }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer(UiLayerName) };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>부모를 가득 채우되 가장자리에서 정한 만큼 안쪽으로 들인다.</summary>
        public static void Stretch(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>부모의 위쪽에 붙은 정해진 높이의 띠로 만든다.</summary>
        public static void AnchorTopStrip(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -height);
            rect.offsetMax = Vector2.zero;
        }

        public static Image AddImage(RectTransform rect, Color color, float radius = 0f, bool isRaycastTarget = false)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = isRaycastTarget;
            if (radius > 0f)
            {
                Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSpritePath);
                image.sprite = sprite;
                image.type = Image.Type.Sliced;

                // 9분할 이미지의 모서리 둥글기는 그림의 테두리 픽셀을 배율로 나눈 값이므로, 원하는 반지름이 되도록 배율을 거꾸로 구한다.
                image.pixelsPerUnitMultiplier = sprite.border.x / radius;
            }

            return image;
        }

        /// <summary>가운데가 빈 링을 칠한다. 링의 굵기는 그림(UiSprites.Ring)이 정하고, 색은 인자로 입힌다.</summary>
        public static Image AddRing(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprites.Ring();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image AddCircle(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(CircleSpritePath);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// 테두리가 있는 둥근 틀을 만든다. 테두리가 없으면 이미지 한 장만 둔다(같은 모양을 겹쳐 그리면 가장자리에 바탕이 비쳐 보이기 때문이다).
        /// </summary>
        public static BorderedPanel AddBorderedPanel(
            Transform parent, string name, Color fill, Color border, float radius, bool hasBorder, bool isRaycastTarget)
        {
            RectTransform root = CreateRect(name, parent);
            if (!hasBorder)
            {
                Image only = AddImage(root, fill, radius, isRaycastTarget);
                return new BorderedPanel(root, root, only);
            }

            AddImage(root, border, radius);
            float inset = DashboardMetrics.BorderWidth;
            RectTransform fillRect = CreateRect(FillName, root);
            Stretch(fillRect, inset, inset, inset, inset);
            Image fillImage = AddImage(fillRect, fill, Mathf.Max(0f, radius - inset), isRaycastTarget);
            return new BorderedPanel(root, fillRect, fillImage);
        }

        /// <summary>글자를 붙인다. 크기와 굵기와 글꼴 종류는 TextRole이 정하고, 글꼴 자체는 시작할 때 UiFontBinding이 끼운다.</summary>
        public static Text AddText(
            RectTransform rect, string text, TextRole role, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool isWrapped = false)
        {
            TextSpec spec = TextSpecs.For(role);

            Text label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.fontSize = spec.Size;
            label.fontStyle = spec.Style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = isWrapped ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            label.raycastTarget = false;

            var binding = rect.gameObject.AddComponent<UiFontBinding>();
            SerializedFieldAssigner.SetEnum(binding, "_role", spec.Font);
            return label;
        }

        /// <summary>부모 아래에 새 영역을 만들고 글자를 붙인다. 배치 그룹 안에서 쓸 크기도 함께 정한다.</summary>
        public static Text AddLabel(
            Transform parent, string name, string text, TextRole role, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool isWrapped = false,
            float preferredWidth = NotSet, float flexibleWidth = NotSet)
        {
            RectTransform rect = CreateRect(name, parent);
            Text label = AddText(rect, text, role, color, anchor, isWrapped);
            SetLayout(rect, preferredWidth: preferredWidth, flexibleWidth: flexibleWidth);
            return label;
        }

        public static LayoutElement SetLayout(
            RectTransform rect, float preferredWidth = NotSet, float preferredHeight = NotSet,
            float flexibleWidth = NotSet, float flexibleHeight = NotSet, float minHeight = NotSet)
        {
            LayoutElement element = rect.gameObject.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = rect.gameObject.AddComponent<LayoutElement>();
            }

            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            element.minHeight = minHeight;
            return element;
        }

        /// <summary>자식을 가로로 늘어놓는다. 자식의 크기는 자식이 정한 값(LayoutElement)을 따른다.</summary>
        public static HorizontalLayoutGroup AddRow(
            RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.MiddleLeft, RectOffset padding = null)
        {
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.padding = padding ?? new RectOffset();
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        /// <summary>자식을 세로로 쌓는다. 자식은 가로를 가득 채우고 높이는 자식이 정한 값을 따른다.</summary>
        public static VerticalLayoutGroup AddColumn(RectTransform rect, float spacing, RectOffset padding = null)
        {
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.UpperLeft;
            group.padding = padding ?? new RectOffset();
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }
    }
}
