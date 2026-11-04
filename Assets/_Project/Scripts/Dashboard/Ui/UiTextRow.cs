using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>글 한 줄(경고 문구 등)을 보여 주는 목록 줄. 글과 색만 받으므로 어떤 목록에도 쓴다.</summary>
    public sealed class UiTextRow : MonoBehaviour
    {
        [SerializeField] private Text _label;

        public void Show(string text, Color color)
        {
            _label.text = text;
            _label.color = color;
        }
    }
}
