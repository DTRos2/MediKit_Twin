using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 작은 이름표와 큰 숫자가 든 칸(완료, 불량품, 오류 횟수). 이름표와 색은 계층에서 정해 두고, 실행 중에는 숫자만 바뀐다.
    /// </summary>
    public sealed class UiStatTile : MonoBehaviour
    {
        [SerializeField] private Text _caption;
        [SerializeField] private Text _value;

        public void SetCaption(string caption)
        {
            _caption.text = caption;
        }

        public void Show(int value)
        {
            _value.text = value.ToString();
        }
    }
}
