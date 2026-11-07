using System;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 머리줄을 누르면 본문이 접히고 펼쳐지는 패널 틀. 접으면 본문 오브젝트를 끄고 패널 높이를 머리줄 높이로 줄이며,
    /// 펼치면 처음 높이로 되돌린다. 처음 높이는 시작할 때 패널이 가진 높이를 기억한다.
    /// 본문이 무엇인지는 모른다. 어떤 패널이든 머리줄 단추와 본문 오브젝트만 이으면 쓴다(IMGUI 패널의 접기와 같은 일을 한다).
    /// 접힌 동안 본문 안의 위젯은 꺼져 있으므로 값을 보여 주는 쪽은 그동안 갱신을 건너뛸 수 있다(IsCollapsed).
    /// </summary>
    public sealed class UiCollapsiblePanel : MonoBehaviour
    {
        private const string ExpandedMark = "−";
        private const string CollapsedMark = "+";

        [Tooltip("누르면 접거나 펼치는 머리줄 단추.")]
        [SerializeField] private Button _headerButton;
        [Tooltip("접을 때 꺼지는 본문 오브젝트.")]
        [SerializeField] private GameObject _body;
        [Tooltip("머리줄 끝에 적는 접힘 표시(펼침 −, 접힘 +).")]
        [SerializeField] private Text _mark;
        [Tooltip("켜면 처음에는 머리줄만 보인다.")]
        [SerializeField] private bool _startsCollapsed;

        private RectTransform _rect;
        private float _expandedHeight;

        /// <summary>접힘 또는 펼침으로 바뀌었다. 접혔으면 true.</summary>
        public event Action<bool> CollapsedChanged;

        public bool IsCollapsed { get; private set; }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _expandedHeight = _rect.rect.height;
            SetCollapsed(_startsCollapsed);
        }

        private void OnEnable()
        {
            _headerButton.onClick.AddListener(OnHeaderClicked);
        }

        private void OnDisable()
        {
            _headerButton.onClick.RemoveListener(OnHeaderClicked);
        }

        public void SetCollapsed(bool isCollapsed)
        {
            bool isChanged = IsCollapsed != isCollapsed;
            IsCollapsed = isCollapsed;

            _body.SetActive(!isCollapsed);
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, isCollapsed ? DashboardMetrics.HeaderHeight : _expandedHeight);
            _mark.text = isCollapsed ? CollapsedMark : ExpandedMark;

            if (isChanged)
            {
                CollapsedChanged?.Invoke(isCollapsed);
            }
        }

        private void OnHeaderClicked()
        {
            SetCollapsed(!IsCollapsed);
        }
    }
}
