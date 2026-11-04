using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 팩킹 구역에서 뚜껑 덮기가 실패했을 때만 뜨는 경고. 컨베이어와 팔 A는 이미 멈춰 있고,
    /// 사용자가 "확인하고 다시 덮기"를 누르면 팔 B가 같은 박스에 뚜껑을 다시 덮는다. 또 실패하면 다시 뜬다. (FR-10)
    /// </summary>
    public sealed class PackingFailureAlertPanel : DemoPanel
    {
        private string _message = string.Empty;
        private KitData _kit;
        private bool _isShown;

        public PackingFailureAlertPanel()
            : base("팩킹 실패", "PACKING · 확인 필요", DemoPanelSlots.PackingAlert)
        {
        }

        protected override bool ShouldDraw => _isShown;

        private void OnEnable()
        {
            CellEvents.PackingFailed += OnPackingFailed;
            CellEvents.LidClosed += OnLidClosed;
        }

        private void OnDisable()
        {
            CellEvents.PackingFailed -= OnPackingFailed;
            CellEvents.LidClosed -= OnLidClosed;
        }

        private void OnPackingFailed(KitData kit, string reason)
        {
            _kit = kit;
            _message = reason;
            _isShown = true;
        }

        private void OnLidClosed(KitData kit)
        {
            _isShown = false;
        }

        protected override void DrawContents()
        {
            int sequence = _kit != null ? _kit.KitSequence : 0;
            DashboardText.Line($"키트 {sequence}번 뚜껑을 덮지 못했습니다.", TextRole.BodyBold, DashboardPalette.Default.Ink);
            DashboardText.Paragraph(_message, TextRole.Body, DashboardPalette.Default.Muted);
            DashboardText.Paragraph("컨베이어와 팔 A를 멈췄습니다. 확인하면 팔 B가 다시 덮고, 덮이면 이어서 움직입니다.", TextRole.Body, DashboardPalette.Default.Muted);
            DashboardSpacing.Block();
            if (DashboardButton.Draw("확인하고 다시 덮기", ButtonKind.Primary))
            {
                KitData kit = _kit;
                RunAtNextLayout(() =>
                {
                    _isShown = false;
                    CellEvents.RaisePackingRetryRequested(kit);
                });
            }
        }
    }
}
