using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>키 모양 칸과 그 키가 하는 일을 한 쌍으로 늘어놓는다(예: [R-Shift] 빠르게). 가로 줄 안에서 부른다.</summary>
    public static class KeyHint
    {
        private const float KeyToMeaningGap = 6f;
        private const float HintToHintGap = 14f;

        public static void Draw(string key, string meaning)
        {
            KeyCap.Draw(key);
            GUILayout.Space(KeyToMeaningGap);
            DashboardText.Inline(meaning, TextRole.Small, DashboardPalette.Default.Muted);
            GUILayout.Space(HintToHintGap);
        }
    }
}
