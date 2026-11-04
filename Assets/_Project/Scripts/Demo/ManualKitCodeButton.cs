using System;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 수동 키팅에서 키트 종류 하나를 고르는 버튼. 어떤 종류인지(_kitCode)만 알고, 눌리면 그 종류를 알리며, 지금 고른 종류와 같으면 켜진 모양이 된다.
    /// 계층에서 버튼을 복제하고 _kitCode만 고쳐도 새 종류 버튼이 된다.
    /// </summary>
    public sealed class ManualKitCodeButton : MonoBehaviour
    {
        [Tooltip("이 버튼이 고르는 키트 종류 코드(EM, DX, BL …).")]
        [SerializeField] private string _kitCode;
        [SerializeField] private UiKindButton _button;

        /// <summary>눌렸을 때 이 버튼의 키트 종류를 알린다.</summary>
        public event Action<string> Clicked;

        public string KitCode => _kitCode;

        private void Awake()
        {
            _button.SetLabel(_kitCode);
        }

        private void OnEnable()
        {
            _button.Button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.Button.onClick.RemoveListener(OnClick);
        }

        public void Show(string selectedKitCode, bool isInteractable)
        {
            _button.Show(_kitCode == selectedKitCode ? ButtonKind.Selected : ButtonKind.Secondary, isInteractable);
        }

        private void OnClick()
        {
            Clicked?.Invoke(_kitCode);
        }
    }
}
