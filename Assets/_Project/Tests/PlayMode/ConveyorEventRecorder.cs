#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 컨베이어 시험 동안 박스 흐름의 알림(AtPacking, LidClosed, AtConveyorEnd)과 뚜껑 덮기 로그를 모아 둔다.
    /// 시험이 끝나면(Dispose) 구독을 모두 풀어서, 다음 시험이 이전 시험의 알림을 받지 않게 한다.
    /// </summary>
    internal sealed class ConveyorEventRecorder : IDisposable
    {
        // 뚜껑 덮기 작업(팔 B)이 남기는 로그의 머리말. 실패 이유가 여기에 담긴다.
        private const string LidTaskLogTag = "[LidCoverTask]";

        private readonly List<string> _lidTaskLines = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public ConveyorEventRecorder()
        {
            CellEvents.AtPacking += OnAtPacking;
            CellEvents.LidClosed += OnLidClosed;
            CellEvents.AtConveyorEnd += OnAtConveyorEnd;
            CellEvents.WarningRaised += OnWarning;
            Application.logMessageReceived += OnLog;
        }

        public KitData AtPacking { get; private set; }
        public KitData LidClosed { get; private set; }
        public KitData AtConveyorEnd { get; private set; }

        /// <summary>뚜껑 덮기 작업이 남긴 로그 줄들(시도 기록, 실패 이유).</summary>
        public IReadOnlyList<string> LidTaskLines => _lidTaskLines;

        /// <summary>CellEvents.WarningRaised로 올라온 경고들("코드: 내용").</summary>
        public IReadOnlyList<string> Warnings => _warnings;

        public string Describe()
        {
            return $"뚜껑 덮기 로그:\n{string.Join("\n", _lidTaskLines)}\n경고:\n{string.Join("\n", _warnings)}";
        }

        public void Dispose()
        {
            CellEvents.AtPacking -= OnAtPacking;
            CellEvents.LidClosed -= OnLidClosed;
            CellEvents.AtConveyorEnd -= OnAtConveyorEnd;
            CellEvents.WarningRaised -= OnWarning;
            Application.logMessageReceived -= OnLog;
        }

        private void OnAtPacking(KitData kit) => AtPacking = kit;

        private void OnLidClosed(KitData kit) => LidClosed = kit;

        private void OnAtConveyorEnd(KitData kit) => AtConveyorEnd = kit;

        private void OnWarning(string code, string message) => _warnings.Add($"{code}: {message}");

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.Contains(LidTaskLogTag))
            {
                _lidTaskLines.Add(condition);
            }
        }
    }
}
#endif
