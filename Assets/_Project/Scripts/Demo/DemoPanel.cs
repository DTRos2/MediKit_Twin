using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 화면에 그리는 시험 패널(IMGUI)의 공통 뼈대. 표시 여부, 제목, 화면 모서리 기준 자리, 둥근 카드 틀, 접기를 맡고,
    /// 파생 패널은 머리줄 오른쪽(DrawHeaderRight)과 안에 들어갈 내용(DrawContents)만 그린다.
    /// 패널마다 같은 코드를 복사하지 않고, 모양을 바꾸는 방법도 한 곳이다(모양은 Dashboard 어셈블리).
    /// 제목줄을 누르면 접히고 다시 누르면 펼쳐진다. 기본 자리는 DemoPanelSlots가 정하고, Inspector 값이 있으면 그 값이 우선한다.
    /// 화면 크기가 기준(1920×1080)과 다르면 모든 패널이 같은 배율로 커지고 줄어 서로 겹치지 않는다.
    /// </summary>
    public abstract class DemoPanel : MonoBehaviour
    {
        // 안쪽 스크롤 막대와 내용 사이 여유. 내용 폭을 계산할 때 막대 자리를 미리 빼 둔다.
        private const float ScrollbarReserve = 12f;

        // 예전에는 _anchor, _margin, _size, _title이라는 이름이었다. 이름이 같으면 씬에 저장된 옛 값(옛 크기, 옛 제목)이
        // 새 기본값을 덮어쓰므로, 새 배치가 적용되도록 일부러 이름을 바꿨다.
        [Tooltip("패널이 붙는 화면 모서리. 왼쪽과 오른쪽으로 나눠 놓으면 패널이 가운데에 모이지 않는다.")]
        [SerializeField] private DemoPanelAnchor _panelAnchor;
        [Tooltip("모서리에서 떨어진 가로, 세로 거리(px, 기준 화면 1920×1080 기준).")]
        [SerializeField] private Vector2 _panelMargin;
        [SerializeField] private Vector2 _panelSize;
        [Tooltip("켜면 처음에는 제목줄만 보인다. 제목줄을 누르면 펼쳐진다.")]
        [SerializeField] private bool _startCollapsed;
        [Tooltip("끄면 패널을 화면에 그리지 않는다.")]
        [SerializeField] private bool _isVisible = true;

        private readonly string _title;
        private readonly string _subtitle;

        // 내용이 창보다 길면 잘리는 대신 스크롤한다.
        private Vector2 _contentScroll;
        private bool _isStateReady;
        private bool _isCollapsed;

        // 클릭으로 일어난 상태 변경은 다음 레이아웃 이벤트 맨 앞에서 실행한다(RunAtNextLayout).
        private readonly Queue<Action> _deferredActions = new Queue<Action>();

        protected DemoPanel(string title, string subtitle, DemoPanelSlot slot)
        {
            _title = title;
            _subtitle = subtitle;
            _panelAnchor = slot.Anchor;
            _panelMargin = slot.Margin;
            _panelSize = slot.Size;
            _startCollapsed = slot.StartsCollapsed;
        }

        /// <summary>패널 안쪽 내용의 가로 폭(px). 줄바꿈 높이를 미리 계산해야 하는 줄이 쓴다.</summary>
        protected float ContentWidth => Mathf.Max(0f, _panelSize.x - DashboardMetrics.CardPadding * 2f - ScrollbarReserve);

        /// <summary>패널 안쪽 내용이 쓸 수 있는 세로 높이(px). 안에 따로 스크롤 영역을 두는 패널(로그)이 쓴다.</summary>
        protected float ContentHeight => Mathf.Max(0f, _panelSize.y - DashboardMetrics.HeaderHeight - DashboardMetrics.CardPadding);

        private void OnGUI()
        {
            if (!_isVisible || !ShouldDraw)
            {
                return;
            }

            PrepareState();

            using (new DashboardGuiScope(out Vector2 screen))
            {
                Vector2 size = new Vector2(_panelSize.x, _isCollapsed ? DashboardMetrics.HeaderHeight : _panelSize.y);
                Rect rect = DemoPanelLayout.Place(_panelAnchor, _panelMargin, size, screen.x, screen.y);

                DashboardCard.DrawFrame(rect);
                CardHeader header = DashboardCard.DrawHeader(rect, _title, _subtitle, _isCollapsed);
                if (header.IsToggleClicked)
                {
                    RunAtNextLayout(ToggleCollapsed);
                }

                DrawHeaderRightArea(header.RightArea);

                if (_isCollapsed)
                {
                    return;
                }

                GUILayout.BeginArea(DashboardCard.ContentRect(rect));

                // 가로 막대는 숨긴다. 패널 내용은 창 너비에 맞춰 그려지므로 세로로만 스크롤하면 된다.
                _contentScroll = GUILayout.BeginScrollView(_contentScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                DrawContents();
                GUILayout.EndScrollView();

                GUILayout.EndArea();
            }
        }

        /// <summary>
        /// 지금 패널을 그릴지. 항상 보이는 패널은 그대로 두고, 어떤 일이 있을 때만 보이는 패널이 덮어쓴다.
        /// 한 프레임의 입력과 그리기 이벤트에서 같은 값을 돌려줘야 한다(IMGUI는 같은 프레임에 같은 모양을 기대한다).
        /// </summary>
        protected virtual bool ShouldDraw => true;

        /// <summary>머리줄 오른쪽에 그릴 것(칩, 버튼, 요약 글). 오른쪽 끝에 붙어 세로 가운데에 놓인다. 기본은 아무것도 그리지 않는다.</summary>
        protected virtual void DrawHeaderRight()
        {
        }

        /// <summary>카드 안쪽에 들어갈 내용을 그린다. 카드와 머리줄은 이미 그려져 있다.</summary>
        protected abstract void DrawContents();

        /// <summary>
        /// 그릴 위젯의 수나 모양이 바뀌는 상태 변경(수동 모드 켜기, 로봇 바꾸기, 접기)은 클릭을 받은 이벤트 안에서 바로 하지 말고 이것으로 맡긴다.
        /// 한 번의 OnGUI 안에서 위젯 수가 달라지면 IMGUI의 배치와 그리기가 어긋나므로, 다음 레이아웃 이벤트 맨 앞에서 실행한다.
        /// </summary>
        protected void RunAtNextLayout(Action action)
        {
            _deferredActions.Enqueue(action);
        }

        private void ToggleCollapsed()
        {
            _isCollapsed = !_isCollapsed;
        }

        private void PrepareState()
        {
            if (!_isStateReady)
            {
                _isCollapsed = _startCollapsed;
                _isStateReady = true;
            }

            if (Event.current.type != EventType.Layout)
            {
                return;
            }

            while (_deferredActions.Count > 0)
            {
                _deferredActions.Dequeue().Invoke();
            }
        }

        private void DrawHeaderRightArea(Rect area)
        {
            GUILayout.BeginArea(area);
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawHeaderRight();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
