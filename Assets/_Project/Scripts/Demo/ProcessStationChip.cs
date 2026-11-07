using MediKitTwin.Common;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 설비 하나의 상태 칩("로봇암 A 가동"). 어느 설비인지(_station)만 알고, 상태는 밖에서 받아 글과 색으로 바꿔 보여 준다.
    /// 계층에서 칩을 복제하고 _station만 고쳐도 새 설비 칩이 된다.
    /// </summary>
    public sealed class ProcessStationChip : MonoBehaviour
    {
        [SerializeField] private StationId _station;
        [SerializeField] private UiChip _chip;

        public StationId Station => _station;

        public void Show(IProcessStatus status)
        {
            StationState state = status.StateOf(_station);
            _chip.Show(ProcessStatusText.StationChip(_station, state), KittingChipTones.Station(state), withDot: true);
        }
    }
}
