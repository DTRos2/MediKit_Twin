using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 패널 안의 수동 키팅 조작: 수동 모드 스위치, 키트 종류 고르기, 새 박스. 로봇 동작은 모르고 IManualKitting만 부른다.
    /// 상태를 바꾸는 일은 직접 하지 않고 runAtNextLayout으로 맡긴다(눌린 이벤트 안에서 위젯 수가 바뀌면 IMGUI 배치가 어긋난다).
    /// </summary>
    public sealed class ManualKittingControls
    {
        private const string SwitchTitle = "수동 키팅 모드";
        private const string SwitchDescription = "칸을 누르면 로봇이 집어 박스에 담음";
        private const string NewBoxLabel = "새 박스";

        private readonly IManualKitting _manual;
        private readonly Action<Action> _runAtNextLayout;

        public ManualKittingControls(IManualKitting manual, Action<Action> runAtNextLayout)
        {
            _manual = manual ?? throw new ArgumentNullException(nameof(manual));
            _runAtNextLayout = runAtNextLayout ?? throw new ArgumentNullException(nameof(runAtNextLayout));
        }

        public bool IsManualMode => _manual.IsManualMode;

        /// <summary>수동 모드이고 자동 키팅이 멈춰 있어서 지금 칸을 지시할 수 있으면 true.</summary>
        public bool CanRequest => _manual.CanRequest;

        public string Message => _manual.Message;

        /// <summary>선반 칸을 눌러 로봇에게 집어 담게 한다. 거절 이유는 Message에 남는다.</summary>
        public void RequestPick(string shelfSlotId)
        {
            _manual.TryRequestPick(shelfSlotId);
        }

        public void DrawSwitch()
        {
            bool wantManual = SwitchRow.Draw(_manual.IsManualMode, SwitchTitle, SwitchDescription);
            if (wantManual != _manual.IsManualMode)
            {
                _runAtNextLayout(() => _manual.SetManualMode(wantManual));
            }
        }

        /// <summary>키트 종류 고르기와 새 박스. 수동 모드일 때만 부른다. 로봇이 칸을 집는 중이면 누를 수 없다.</summary>
        public void DrawOperations()
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && !_manual.IsBusy;

            using (DashboardRow.Begin())
            {
                IReadOnlyList<string> codes = _manual.KitCodes;
                foreach (string code in codes)
                {
                    ButtonKind kind = code == _manual.SelectedKitCode ? ButtonKind.Selected : ButtonKind.Secondary;
                    if (DashboardButton.Draw(code, kind, 0f, DashboardMetrics.SmallControlHeight))
                    {
                        string chosen = code;
                        _runAtNextLayout(() => _manual.SelectedKitCode = chosen);
                    }

                    DashboardSpacing.Block();
                }

                if (DashboardButton.Draw(NewBoxLabel, ButtonKind.Secondary, 0f, DashboardMetrics.SmallControlHeight))
                {
                    _runAtNextLayout(_manual.StartNewBox);
                }
            }

            GUI.enabled = wasEnabled;
        }
    }
}
