using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 팩킹 실패 경고 패널의 동작(FR-10). 팩킹 구역에서 뚜껑 덮기가 실패하면(CellEvents.PackingFailed) 패널을 띄우고,
    /// 사용자가 "확인하고 다시 덮기"를 누르면 감추고 다시 덮기를 요청한다(CellEvents.RaisePackingRetryRequested).
    /// 뚜껑이 덮이면(CellEvents.LidClosed) 사용자가 누르지 않아도 감춘다. 컨베이어와 팔 A를 멈추는 일은 PackingFailureHandler가 한다.
    /// 시작할 때는 늘 감춘 상태로 둔다.
    /// </summary>
    public sealed class PackingFailureAlertPanelPresenter : MonoBehaviour
    {
        [SerializeField] private PackingFailureAlertPanelView _view;

        private KitData _kit;

        private void OnEnable()
        {
            _view.RetryRequested += OnRetryRequested;
            CellEvents.PackingFailed += OnPackingFailed;
            CellEvents.LidClosed += OnLidClosed;
            _view.Hide();
        }

        private void OnDisable()
        {
            _view.RetryRequested -= OnRetryRequested;
            CellEvents.PackingFailed -= OnPackingFailed;
            CellEvents.LidClosed -= OnLidClosed;
        }

        private void OnPackingFailed(KitData kit, string reason)
        {
            _kit = kit;
            int sequence = kit != null ? kit.KitSequence : 0;
            _view.Show(PackingFailureAlertPanelTexts.Headline(sequence), reason);
        }

        private void OnLidClosed(KitData kit)
        {
            _view.Hide();
        }

        private void OnRetryRequested()
        {
            KitData kit = _kit;
            _view.Hide();
            CellEvents.RaisePackingRetryRequested(kit);
        }
    }
}
