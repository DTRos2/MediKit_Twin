using System;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 배속 패널의 화면. 계층에 놓인 현재 배속 글과 배속 버튼들을 다루기만 하고, 배속이 무엇이고 어떻게 바꾸는지는 모른다.
    /// 버튼은 이 오브젝트 아래에서 찾으므로, 계층에서 버튼을 더하거나 빼면 그대로 따라온다.
    /// </summary>
    public sealed class SimSpeedPanelView : MonoBehaviour
    {
        [SerializeField] private Text _current;

        private SimSpeedButton[] _buttons = Array.Empty<SimSpeedButton>();

        /// <summary>어떤 배속 버튼이 눌렸는지 알린다.</summary>
        public event Action<float> SpeedRequested;

        private void Awake()
        {
            _buttons = GetComponentsInChildren<SimSpeedButton>(true);
        }

        private void OnEnable()
        {
            foreach (SimSpeedButton button in _buttons)
            {
                button.Clicked += OnButtonClicked;
            }
        }

        private void OnDisable()
        {
            foreach (SimSpeedButton button in _buttons)
            {
                button.Clicked -= OnButtonClicked;
            }
        }

        public void Show(float speed, bool isLocked)
        {
            _current.text = isLocked ? SimSpeedPanelTexts.Locked(speed) : SimSpeedPanelTexts.Current(speed);

            foreach (SimSpeedButton button in _buttons)
            {
                button.Show(speed, isLocked);
            }
        }

        private void OnButtonClicked(float speed)
        {
            SpeedRequested?.Invoke(speed);
        }
    }
}
