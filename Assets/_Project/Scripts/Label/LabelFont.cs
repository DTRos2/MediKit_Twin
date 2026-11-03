using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Label
{
    /// <summary>
    /// 라벨 글자에 쓸 글꼴을 고른다. 이름 목록을 앞에서부터 보고 컴퓨터에 깔린 첫 글꼴로 동적 글꼴을 만든다(한글이 나오는 글꼴을 앞에 둔다).
    /// 하나도 없으면 Unity 기본 글꼴을 쓴다. 같은 이름 목록으로는 한 번만 만든다. (FR-11)
    /// </summary>
    public static class LabelFont
    {
        private const string BuiltinFontName = "LegacyRuntime.ttf";

        private static Font _cached;
        private static string _cachedKey;

        public static Font Resolve(IReadOnlyList<string> names, int fontSize)
        {
            string key = names == null ? string.Empty : string.Join("|", names) + "|" + fontSize;
            if (_cached != null && key == _cachedKey)
            {
                return _cached;
            }

            _cached = Pick(names, fontSize) ?? Resources.GetBuiltinResource<Font>(BuiltinFontName);
            _cachedKey = key;
            return _cached;
        }

        private static Font Pick(IReadOnlyList<string> names, int fontSize)
        {
            if (names == null || names.Count == 0)
            {
                return null;
            }

            var installed = new HashSet<string>(Font.GetOSInstalledFontNames(), StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name) && installed.Contains(name))
                {
                    Font font = Font.CreateDynamicFontFromOSFont(name, fontSize);
                    if (font != null)
                    {
                        // 씬을 다시 열어도 글꼴이 남아 있게 하고, 에디터가 저장 대상으로 잡지 않게 한다.
                        font.hideFlags = HideFlags.HideAndDontSave;
                        return font;
                    }
                }
            }

            return null;
        }
    }
}
