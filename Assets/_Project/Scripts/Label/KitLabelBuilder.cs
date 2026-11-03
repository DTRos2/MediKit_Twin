using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Label
{
    /// <summary>
    /// 라벨 오브젝트를 만든다. 흰 종이(얇은 판)와 그 위의 글자(TextMesh)로 이루어지고, 뚜껑의 자식으로 붙어 박스를 따라 움직인다. (FR-11)
    /// 종이에는 충돌체를 두지 않는다. 박스를 집거나 미는 설비와 부딪히면 안 되기 때문이다.
    /// </summary>
    public sealed class KitLabelBuilder
    {
        private const string LabelName = "KitLabel";
        private const string PaperName = "Paper";
        private const string TextName = "Text";

        // 글자는 종이보다 이만큼(m) 위에 두어 종이에 묻히지 않게 한다.
        private const float TextLiftM = 0.0002f;

        // 글자를 위를 향해 눕힌다. TextMesh의 앞면은 -Z를 향하므로 X축으로 90도 돌리면 앞면이 위(+Y)를 보고, 글자의 위쪽이 +Z가 된다.
        private static readonly Quaternion FaceUp = Quaternion.Euler(90f, 0f, 0f);

        private readonly KitLabelLook _look;
        private readonly Material _paperMaterial;
        private readonly Font _font;

        /// <param name="paperMaterial">종이 재질. 색은 만들 때 덧입히므로 공유 재질을 바꾸지 않는다.</param>
        public KitLabelBuilder(KitLabelLook look, Material paperMaterial, Font font)
        {
            _look = look;
            _paperMaterial = paperMaterial;
            _font = font;
        }

        /// <summary>뚜껑 윗면 위에 라벨을 만든다. 뚜껑의 자식이 되고, 뚜껑과 같은 방향으로 놓인다.</summary>
        /// <param name="topCenterWorld">뚜껑 윗면 가운데(월드).</param>
        public GameObject BuildOnLid(KitData kit, string[] lines, Transform lid, Vector3 topCenterWorld)
        {
            var label = new GameObject(LabelName);
            label.transform.SetPositionAndRotation(topCenterWorld + lid.up * (_look.LiftAboveSurface + _look.Thickness * 0.5f), lid.rotation);
            label.transform.SetParent(lid, true);
            label.AddComponent<KitLabelTag>().Configure(kit, lines);

            BuildPaper(label.transform);
            CreateText(label.transform, new Vector3(0f, _look.Thickness * 0.5f + TextLiftM, 0f), FaceUp, LabelText.Join(lines), _look.CharacterSize);
            return label;
        }

        /// <summary>글자만 만든다. 표시판처럼 종이가 필요 없는 곳에 쓴다. localRotation으로 글자가 향할 쪽을 정한다(TextMesh의 앞면은 -Z).</summary>
        public TextMesh CreateText(Transform parent, Vector3 localPosition, Quaternion localRotation, string text, float characterSize)
        {
            var textObject = new GameObject(TextName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = localRotation;

            var mesh = textObject.AddComponent<TextMesh>();
            mesh.font = _font;
            mesh.fontSize = _look.FontSize;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = _look.TextColor;
            mesh.text = text;

            // 동적 글꼴은 자기 재질을 가지고 있고, 그것을 렌더러에 줘야 글자가 그려진다.
            textObject.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            return mesh;
        }

        private void BuildPaper(Transform label)
        {
            GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.name = PaperName;

            // 종이는 보이기만 하면 되므로 충돌체를 뗀다.
            Object.Destroy(paper.GetComponent<Collider>());

            paper.transform.SetParent(label, false);
            paper.transform.localScale = new Vector3(_look.Size.x, _look.Thickness, _look.Size.y);

            var renderer = paper.GetComponent<MeshRenderer>();
            if (_paperMaterial != null)
            {
                renderer.sharedMaterial = _paperMaterial;
            }

            // 공유 재질의 색을 바꾸지 않도록 이 종이에만 색을 입힌다.
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", _look.PaperColor);
            block.SetColor("_Color", _look.PaperColor);
            renderer.SetPropertyBlock(block);
        }
    }
}
