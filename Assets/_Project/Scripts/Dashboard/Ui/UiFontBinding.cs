using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 글자(Text)에 대시보드 글꼴을 입힌다. 컴퓨터에 깔린 글꼴은 에셋으로 저장할 수 없으므로
    /// 씬과 프리팹에는 "어떤 종류의 글꼴인가"(역할)만 저장하고, 실제 글꼴은 시작할 때 DashboardFonts에서 받아 끼운다.
    /// 글꼴 후보를 바꾸려면 DashboardFonts 한 곳만 고치면 이 컴포넌트를 단 모든 글자가 따라 바뀐다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class UiFontBinding : MonoBehaviour
    {
        [Tooltip("Sans는 한글 본문, Mono는 숫자와 영문 라벨에 쓰는 고정폭 글꼴이다.")]
        [SerializeField] private DashboardFontRole _role;

        private void Awake()
        {
            Apply();
        }

        /// <summary>역할에 맞는 글꼴을 글자에 끼운다. 쓸 수 있는 글꼴이 없으면 Text가 가진 글꼴을 그대로 둔다.</summary>
        public void Apply()
        {
            Font font = DashboardFonts.For(_role);
            if (font != null)
            {
                GetComponent<Text>().font = font;
            }
        }
    }
}
