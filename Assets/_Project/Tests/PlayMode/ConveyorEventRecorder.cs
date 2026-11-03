#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 컨베이어·검사·푸셔 시험 동안 박스 흐름의 알림과 뚜껑 덮기 로그를 모아 둔다.
    /// 알림마다 마지막 키트(AtPacking, LidClosed, AtConveyorEnd)와, 키트가 여럿일 때를 위해 받은 순서대로의 기록(Timeline)을 함께 남긴다.
    /// 시험이 끝나면(Dispose) 구독을 모두 풀어서, 다음 시험이 이전 시험의 알림을 받지 않게 한다.
    /// </summary>
    internal sealed class ConveyorEventRecorder : IDisposable
    {
        // 뚜껑 덮기 작업(팔 B)이 남기는 로그의 머리말. 실패 이유가 여기에 담긴다.
        private const string LidTaskLogTag = "[LidCoverTask]";

        private readonly List<string> _lidTaskLines = new List<string>();
        private readonly List<string> _warnings = new List<string>();
        private readonly List<FlowEvent> _timeline = new List<FlowEvent>();

        public ConveyorEventRecorder()
        {
            CellEvents.KitReady += OnKitReady;
            CellEvents.AtInspection += OnAtInspection;
            CellEvents.InspectionResult += OnInspectionResult;
            CellEvents.KitRejected += OnKitRejected;
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

        /// <summary>받은 알림을 받은 순서대로. 키트가 여럿일 때 키트별 순서를 따진다.</summary>
        public IReadOnlyList<FlowEvent> Timeline => _timeline;

        /// <summary>이 알림을 받은 키트의 번호들(받은 순서).</summary>
        public List<int> KitsWith(FlowStep step)
        {
            var kits = new List<int>();
            foreach (FlowEvent flow in _timeline)
            {
                if (flow.Step == step)
                {
                    kits.Add(flow.Kit.KitSequence);
                }
            }

            return kits;
        }

        /// <summary>이 키트가 이 알림을 받은 차례(Timeline의 위치). 받지 않았으면 -1.</summary>
        public int IndexOf(int kitSequence, FlowStep step)
        {
            for (int i = 0; i < _timeline.Count; i++)
            {
                if (_timeline[i].Step == step && _timeline[i].Kit.KitSequence == kitSequence)
                {
                    return i;
                }
            }

            return -1;
        }

        public string Describe()
        {
            return $"뚜껑 덮기 로그:\n{string.Join("\n", _lidTaskLines)}\n경고:\n{string.Join("\n", _warnings)}";
        }

        /// <summary>시험 실패 때 어디까지 갔는지 보도록 알림을 시각과 함께 한 줄씩 적는다.</summary>
        public string DescribeTimeline()
        {
            var lines = new List<string>(_timeline.Count);
            foreach (FlowEvent flow in _timeline)
            {
                string verdict = flow.Step == FlowStep.InspectionResult ? DescribeVerdict(flow.Kit) : string.Empty;
                lines.Add($"  {flow.Time,7:0.0}초  키트 {flow.Kit.KitSequence}  {flow.Step}{verdict}");
            }

            return string.Join("\n", lines);
        }

        // 판정과 그 근거. 정상이 불량으로 나왔을 때 어느 슬롯에서 무엇을 읽었는지 바로 보이게 한다.
        private static string DescribeVerdict(KitData kit)
        {
            if (!kit.Verdict.HasValue)
            {
                return " (판정 없음)";
            }

            InspectionVerdict verdict = kit.Verdict.Value;
            string detected = kit.DetectedComponentIds == null
                ? "읽지 못함"
                : string.Join(",", Array.ConvertAll(kit.DetectedComponentIds, id => id ?? "-"));
            string missing = verdict.MissingSlots == null ? string.Empty : string.Join(",", verdict.MissingSlots);
            return $" ({(verdict.IsPass ? "정상" : "불량")}, {verdict.Method}, 빠진 슬롯 [{missing}], 슬롯별 감지 [{detected}], 누락 정답 {kit.GetMissingAnswerForEvaluation()?.ToString() ?? "없음"})";
        }

        public void Dispose()
        {
            CellEvents.KitReady -= OnKitReady;
            CellEvents.AtInspection -= OnAtInspection;
            CellEvents.InspectionResult -= OnInspectionResult;
            CellEvents.KitRejected -= OnKitRejected;
            CellEvents.AtPacking -= OnAtPacking;
            CellEvents.LidClosed -= OnLidClosed;
            CellEvents.AtConveyorEnd -= OnAtConveyorEnd;
            CellEvents.WarningRaised -= OnWarning;
            Application.logMessageReceived -= OnLog;
        }

        private void Record(FlowStep step, KitData kit)
        {
            _timeline.Add(new FlowEvent(step, kit, Time.time));
        }

        private void OnKitReady(KitData kit) => Record(FlowStep.KitReady, kit);

        private void OnAtInspection(KitData kit) => Record(FlowStep.AtInspection, kit);

        private void OnInspectionResult(KitData kit) => Record(FlowStep.InspectionResult, kit);

        private void OnKitRejected(KitData kit) => Record(FlowStep.KitRejected, kit);

        private void OnAtPacking(KitData kit)
        {
            AtPacking = kit;
            Record(FlowStep.AtPacking, kit);
        }

        private void OnLidClosed(KitData kit)
        {
            LidClosed = kit;
            Record(FlowStep.LidClosed, kit);
        }

        private void OnAtConveyorEnd(KitData kit)
        {
            AtConveyorEnd = kit;
            Record(FlowStep.AtConveyorEnd, kit);
        }

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
