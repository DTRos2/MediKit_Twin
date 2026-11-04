using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 실행 중에 종류(ButtonKind)를 바꿀 수 있는 둥근 버튼. 켜진 토글처럼 보이게 하거나(Selected) 눌러도 안 되게 할 때 쓴다.
    /// 종류별 색은 IMGUI 버튼과 같은 ButtonColors가, 상태별 색 블록은 UiButtonColors가 정하므로 만들 때와 같은 모양이 된다.
    /// </summary>
    public sealed class UiKindButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [Tooltip("바깥 테두리 그림. 테두리 없는 버튼이면 비워 둔다.")]
        [SerializeField] private Image _border;
        [SerializeField] private Text _label;

        public Button Button => _button;

        public void SetLabel(string text)
        {
            _label.text = text;
        }

        /// <param name="isInteractable">false이면 눌리지 않고 흐리게 보인다.</param>
        public void Show(ButtonKind kind, bool isInteractable)
        {
            DashboardPalette palette = DashboardPalette.Default;
            ButtonColors.Resolve(kind, palette, out Color background, out Color foreground, out Color border, out bool hasBorder);

            _button.colors = UiButtonColors.For(kind, palette);
            _button.interactable = isInteractable;

            // 테두리가 없는 종류는 테두리 그림을 바탕색으로 칠해 틀이 그대로 보이게 한다(만들 때와 같다).
            if (_border != null)
            {
                _border.color = hasBorder ? border : background;
            }

            float alpha = isInteractable ? 1f : DashboardMetrics.DisabledAlpha;
            _label.color = new Color(foreground.r, foreground.g, foreground.b, foreground.a * alpha);
        }
    }
}
