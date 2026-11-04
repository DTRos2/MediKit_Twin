using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>색 점과 한 줄 글로 된 상태 줄(예: "● 물건 탐색 안 함"). 글과 두 색만 받는다. 글이 줄 폭보다 길면 잘린다.</summary>
    public sealed class UiStatusLine : MonoBehaviour
    {
        [SerializeField] private Image _dot;
        [SerializeField] private Text _label;

        public void Show(string text, Color dotColor, Color textColor)
        {
            _dot.color = dotColor;
            _label.text = text;
            _label.color = textColor;
        }
    }
}
