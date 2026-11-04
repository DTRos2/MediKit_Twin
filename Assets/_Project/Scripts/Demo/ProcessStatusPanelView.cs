using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 공정 현황 패널의 화면. 단계 글, 설비 칩, 숫자 칸, 경고 글은 모두 계층에 놓인 오브젝트이고, 이 클래스는 그 연결 고리만 쥐고 값을 채운다.
    /// 코드로 위젯을 만들거나 그리지 않으며, 값이 어디서 오는지는 모른다(IProcessStatus를 받아 보여 주기만 한다).
    /// 설비 칩은 이 오브젝트 아래에서 찾으므로, 계층에서 칩을 더하거나 빼면 그대로 따라온다.
    /// </summary>
    public sealed class ProcessStatusPanelView : MonoBehaviour
    {
        [Header("지금 키트")]
        [Tooltip("지금 단계를 보여 주는 큰 글자. 색은 단계에 따라 바뀐다.")]
        [SerializeField] private Text _headline;
        [SerializeField] private Text _kitSummary;

        [Header("숫자 칸")]
        [SerializeField] private UiStatTile _completed;
        [SerializeField] private UiStatTile _rejected;
        [SerializeField] private UiStatTile _stockShortage;
        [SerializeField] private UiStatTile _fault;

        [Header("마지막 경고")]
        [Tooltip("경고가 있을 때만 켜지는 글.")]
        [SerializeField] private Text _warning;

        private ProcessStationChip[] _stations = System.Array.Empty<ProcessStationChip>();

        private void Awake()
        {
            _stations = GetComponentsInChildren<ProcessStationChip>(true);
        }

        public void Show(IProcessStatus status)
        {
            DashboardPalette palette = DashboardPalette.Default;

            ChipColors.Resolve(ProcessChipTones.Headline(status), palette, out _, out Color stageColor);
            _headline.text = ProcessStatusText.Headline(status);
            _headline.color = stageColor;
            _kitSummary.text = ProcessStatusText.KitSummary(status.CurrentKit);

            foreach (ProcessStationChip chip in _stations)
            {
                chip.Show(status);
            }

            _completed.Show(status.CompletedCount);
            _rejected.Show(status.RejectedCount);
            _stockShortage.Show(status.StockShortageCount);
            _fault.Show(status.FaultCount);

            bool hasWarning = status.LastWarning.Length > 0;
            _warning.gameObject.SetActive(hasWarning);
            if (hasWarning)
            {
                _warning.text = status.LastWarning;
            }
        }
    }
}
