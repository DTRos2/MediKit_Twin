using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 메인 화면에 겹치는 인식 표시의 화면: 인식한 물건 자리에 놓이는 초록 네모와 그 아래 짧은 글. 둘 다 계층에 놓인 오브젝트이고,
    /// 이 클래스는 화면 좌표(픽셀)를 받아 캔버스 위 자리로 옮기고 켜고 끌 뿐이다. 무엇을 인식했는지는 모른다.
    /// 캔버스는 화면에 맞춰 배율이 바뀌므로(CanvasScaler) 화면 좌표를 곧바로 쓰지 않고 이 영역 기준 좌표로 바꾼다.
    /// </summary>
    public sealed class RecognitionMarkerView : MonoBehaviour
    {
        [Tooltip("화면 전체를 덮는 영역. 화면 좌표를 이 영역 기준 좌표로 바꾸는 기준이다.")]
        [SerializeField] private RectTransform _area;
        [Tooltip("네모와 글이 든 오브젝트. 인식한 자리로 옮기고 인식이 없으면 끈다.")]
        [SerializeField] private RectTransform _mark;
        [SerializeField] private Text _label;

        /// <summary>화면 좌표(왼쪽 아래가 0, 픽셀)의 자리에 표시를 놓고 글을 적는다.</summary>
        public void Show(Vector2 screenPoint, string text)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screenPoint, null, out Vector2 local))
            {
                Hide();
                return;
            }

            _mark.anchoredPosition = local;
            _label.text = text;
            SetActive(true);
        }

        public void Hide()
        {
            SetActive(false);
        }

        private void SetActive(bool isActive)
        {
            if (_mark.gameObject.activeSelf != isActive)
            {
                _mark.gameObject.SetActive(isActive);
            }
        }
    }
}
