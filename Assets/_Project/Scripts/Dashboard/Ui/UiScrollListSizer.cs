using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 스크롤 목록의 높이를 내용에 맞추되 정한 최대 높이를 넘지 않게 한다.
    /// 줄이 적으면 목록이 그만큼만 차지해 빈 칸이 남지 않고, 많으면 최대 높이에서 멈춰 안에서 스크롤한다.
    /// 목록의 내용이 바뀐 직후에 Refresh를 부른다.
    /// </summary>
    public sealed class UiScrollListSizer : MonoBehaviour
    {
        [Tooltip("줄들이 쌓이는 스크롤 내용 영역.")]
        [SerializeField] private RectTransform _content;
        [Tooltip("높이를 정해 줄 목록 틀의 LayoutElement.")]
        [SerializeField] private LayoutElement _frame;
        [Tooltip("목록이 늘어날 수 있는 최대 높이(px). 넘치면 안에서 스크롤한다.")]
        [SerializeField, Min(0f)] private float _maxHeight;

        public void Refresh()
        {
            // 줄을 켜고 끈 직후에는 내용의 크기가 아직 예전 값이므로 먼저 다시 계산시킨다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _frame.preferredHeight = Mathf.Min(LayoutUtility.GetPreferredHeight(_content), _maxHeight);
        }
    }
}
