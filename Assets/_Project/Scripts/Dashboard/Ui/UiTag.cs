using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 테두리만 있는 작은 태그(로그 줄의 READY, CAMERA …). 글과 계열(TagTone)만 받고, 계열에 맞는 테두리색과 글자색은
    /// IMGUI 태그와 같은 TagColors가 정하므로 두 화면의 태그 색이 어긋나지 않는다. 안쪽 바탕은 만들 때 정해 두고 바꾸지 않는다.
    /// </summary>
    public sealed class UiTag : MonoBehaviour
    {
        [Tooltip("바깥 테두리 그림. 계열에 따라 색이 바뀐다.")]
        [SerializeField] private Image _border;
        [SerializeField] private Text _label;

        public void Show(string text, TagTone tone)
        {
            TagColors.Resolve(tone, DashboardPalette.Default, out Color border, out Color foreground);

            _border.color = border;
            _label.text = text;
            _label.color = foreground;
        }
    }
}
