using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 대시보드가 쓰는 글꼴. 컴퓨터에 깔린 글꼴을 앞에서부터 찾아 쓴다(한글이 나오는 글꼴을 먼저).
    /// 하나도 없으면 Unity 기본 글꼴(null)로 대신한다. 글꼴 이름 목록만 고치면 다른 글꼴로 바뀐다.
    /// 이름만 있다고 쓸 수 있는 것이 아니다. IMGUI가 글자를 그리려면 글꼴이 폰트 에셋으로 바뀌어야 하는데,
    /// 깔려 있지 않은 글꼴이나 가변 글꼴(예: NotoSansKR-VF)은 바뀌지 않고, 바뀌지 않으면 글자를 재고 그릴 때마다 다시 시도하며
    /// 경고를 남겨 화면이 심하게 느려진다. 그래서 설치되어 있고 실제로 바뀌는 글꼴만 고른다.
    /// </summary>
    public static class DashboardFonts
    {
        private const int OsFontBaseSize = 16;

        private static readonly string[] SansNames =
        {
            "Pretendard", "Noto Sans KR", "Malgun Gothic", "Apple SD Gothic Neo", "Segoe UI", "Arial",
        };

        // 숫자와 영문 라벨에 쓰는 고정폭 글꼴.
        private static readonly string[] MonoNames =
        {
            "JetBrains Mono", "D2Coding", "Consolas", "Menlo", "Courier New",
        };

        private static Font _sans;
        private static Font _mono;
        private static bool _isSansResolved;
        private static bool _isMonoResolved;

        /// <summary>한글 본문 글꼴. 쓸 수 있는 것이 하나도 없으면 null(Unity 기본 글꼴).</summary>
        public static Font Sans
        {
            get
            {
                if (!_isSansResolved || IsDestroyed(_sans))
                {
                    _sans = Pick(SansNames);
                    _isSansResolved = true;
                }

                return _sans;
            }
        }

        /// <summary>숫자와 영문 라벨용 고정폭 글꼴. 쓸 수 있는 것이 하나도 없으면 null(Unity 기본 글꼴).</summary>
        public static Font Mono
        {
            get
            {
                if (!_isMonoResolved || IsDestroyed(_mono))
                {
                    _mono = Pick(MonoNames);
                    _isMonoResolved = true;
                }

                return _mono;
            }
        }

        /// <summary>글꼴 종류에 맞는 글꼴. 쓸 수 있는 것이 하나도 없으면 null(Unity 기본 글꼴).</summary>
        public static Font For(DashboardFontRole role)
        {
            return role == DashboardFontRole.Mono ? Mono : Sans;
        }

        /// <summary>
        /// 캐시한 글꼴을 버리고 다음에 쓸 때 다시 고르게 한다. Play를 시작할 때 부르며, 시험도 쓴다.
        /// Domain Reload를 끈 에디터에서는 정적 변수가 Play가 끝나도 남는다. 이전 Play에서 만든 글꼴 객체만 캐시에 남으면
        /// 두 번째 Play부터 글자가 모두 비어 그려지는 증상이 나서, 매 Play를 처음 실행처럼 새 글꼴로 시작한다.
        /// </summary>
        public static void ResetCache()
        {
            Discard(_sans);
            Discard(_mono);
            _sans = null;
            _mono = null;
            _isSansResolved = false;
            _isMonoResolved = false;
        }

        // CellEvents·SimClock처럼 Play를 시작할 때 이전 Play의 상태를 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetWhenPlayStarts()
        {
            ResetCache();
        }

        // 이미 파괴된 글꼴이 캐시에 남았는지 본다. 처음부터 null인 것(쓸 수 있는 글꼴이 없음)은 파괴된 것이 아니다.
        private static bool IsDestroyed(UnityEngine.Object unityObject)
        {
            return !object.ReferenceEquals(unityObject, null) && unityObject == null;
        }

        private static Font Pick(string[] names)
        {
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames(), StringComparer.OrdinalIgnoreCase);
            Font font = FontPicker.FirstUsable(names, name => installed.Contains(name) ? TryCreate(name) : null);
            if (font != null)
            {
                font.hideFlags = HideFlags.HideAndDontSave;
            }

            return font;
        }

        // 이름 하나로 글꼴을 만들고, IMGUI가 쓰는 폰트 에셋으로 바뀌는지 확인한다. 안 바뀌면 버리고 null.
        private static Font TryCreate(string name)
        {
            Font font = Font.CreateDynamicFontFromOSFont(name, OsFontBaseSize);
            if (font == null)
            {
                return null;
            }

            if (ConvertsToFontAsset(font))
            {
                return font;
            }

            Discard(font);
            return null;
        }

        // 바뀌지 않는 후보는 여기서 경고를 한 번 남기는데, 다음 후보로 넘어가는 정상적인 과정이므로 이 순간만 로그를 끈다.
        private static bool ConvertsToFontAsset(Font font)
        {
            bool wasLogEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;
            try
            {
                FontAsset asset = FontAsset.CreateFontAsset(font);
                if (asset == null)
                {
                    return false;
                }

                Discard(asset);
                return true;
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLogEnabled;
            }
        }

        private static void Discard(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(unityObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(unityObject);
            }
        }
    }
}
