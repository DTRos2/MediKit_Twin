using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>글 한 줄을 보여 주는 목록 줄 프리팹(PF_TextRow)을 만든다. 경고 목록처럼 글만 쌓이는 곳이 함께 쓴다.</summary>
    public static class TextRowPrefabBuilder
    {
        public static UiTextRow Build()
        {
            RectTransform root = UiPrimitives.CreateRect("PF_TextRow", null);
            Text label = UiPrimitives.AddText(root, string.Empty, TextRole.Small, DashboardPalette.Default.Warning, isWrapped: true);

            var row = root.gameObject.AddComponent<UiTextRow>();
            SerializedFieldAssigner.SetReference(row, "_label", label);
            return UiPrefabWriter.SaveAndLoad<UiTextRow>(root.gameObject, ProjectAssetPaths.TextRowPrefab);
        }
    }
}
