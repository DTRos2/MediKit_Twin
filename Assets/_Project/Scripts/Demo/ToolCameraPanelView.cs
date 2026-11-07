using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 툴 카메라 패널의 화면. 카메라 그림(RawImage), 그 위에 겹친 꺾쇠·조준 원·정보 칩·인식 네모, 아래 상태 줄은 모두 계층에 놓인 오브젝트이고,
    /// 이 클래스는 그 연결 고리만 쥐고 값을 채운다. 카메라가 무엇인지는 모르고, 보여 줄 그림과 글을 밖에서 받는다.
    /// 그림은 칸에 늘리지 않고 가운데를 잘라 채우며(CameraImageFit), 인식 네모는 잘린 그림 속 위치에 맞춰 옮긴다.
    /// 보일지 말지는 패널 전체의 투명도와 눌림 받기(CanvasGroup)로 정하므로, 숨겨진 동안에도 동작 코드는 계속 돈다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ToolCameraPanelView : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        [SerializeField] private Text _positionChip;
        [SerializeField] private Text _detectChip;
        [Tooltip("찾은 물건 자리에 놓이는 네모. 그림 영역의 가운데를 기준으로 옮긴다.")]
        [SerializeField] private RectTransform _marker;
        [SerializeField] private UiStatusLine _status;

        private CanvasGroup _group;
        private Rect _visibleUv = new Rect(0f, 0f, 1f, 1f);

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        public void SetVisible(bool isVisible)
        {
            Group.alpha = isVisible ? 1f : 0f;
            Group.blocksRaycasts = isVisible;
        }

        /// <summary>카메라 그림을 그림 영역에 가운데를 잘라 채운다. 영역의 모양이 바뀌면 자르는 부분도 따라 바뀐다.</summary>
        public void ShowImage(Texture image)
        {
            Rect area = _image.rectTransform.rect;
            _visibleUv = CameraImageFit.VisibleUv(image.width, image.height, area.width, area.height);
            _image.texture = image;
            _image.uvRect = _visibleUv;
        }

        public void ShowChips(string positionText, string detectText)
        {
            _positionChip.text = positionText;
            _detectChip.text = detectText;
        }

        /// <summary>
        /// 그림 속 위치(0~1, 왼쪽 아래가 0)에 인식 네모를 놓는다. 잘려서 안 보이는 자리면 숨긴다.
        /// </summary>
        public void ShowMarker(bool hasMarker, Vector2 viewport)
        {
            Rect area = _image.rectTransform.rect;
            if (hasMarker && CameraImageFit.TryToGuiPoint(new Rect(0f, 0f, area.width, area.height), _visibleUv, viewport, out Vector2 point))
            {
                // 화면 좌표는 위가 0이고 영역 가운데가 기준인 캔버스 좌표는 위로 갈수록 커서 세로를 뒤집는다.
                _marker.anchoredPosition = new Vector2(point.x - area.width * 0.5f, area.height * 0.5f - point.y);
                SetActive(_marker.gameObject, true);
                return;
            }

            SetActive(_marker.gameObject, false);
        }

        public void ShowStatus(string text, Color dotColor, Color textColor)
        {
            _status.Show(text, dotColor, textColor);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
