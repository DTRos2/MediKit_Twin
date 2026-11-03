using System;
using MediKitTwin.Common;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 공정 현황을 화면에 적을 글로 바꾼다. 어떤 단계를 어떤 말로 부르는지를 패널 그리기 코드에서 떼어 한곳에 둔다.
    /// 설비 상태의 한글 표시는 대시보드 규칙(INTERFACES.md 3장)을 따른다: 대기, 가동, 정지. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ProcessStatusText
    {
        private const string Idle = "대기 중";
        private const string Kitting = "키팅 중";

        public static string StationName(StationId station)
        {
            switch (station)
            {
                case StationId.ArmA: return "로봇암 A";
                case StationId.ArmB: return "로봇암 B";
                case StationId.Conveyor: return "컨베이어";
                case StationId.Inspection: return "검사";
                case StationId.Pusher: return "푸셔";
                case StationId.Unloader: return "하역";
                default: throw new ArgumentOutOfRangeException(nameof(station), station, "알 수 없는 설비다.");
            }
        }

        public static string StateText(StationState state) => KittingStatusText.StationText(state);

        /// <summary>"로봇암 A 가동"처럼 칩에 적는 글.</summary>
        public static string StationChip(StationId station, StationState state) => $"{StationName(station)} {StateText(state)}";

        public static string Stage(ProcessStage stage)
        {
            switch (stage)
            {
                case ProcessStage.ToInspection: return "검사로 이송 중";
                case ProcessStage.Inspecting: return "검사 중";
                case ProcessStage.Rejecting: return "불량 — 밀어내는 중";
                case ProcessStage.Rejected: return "불량 박스에 넣음";
                case ProcessStage.ToPacking: return "팩킹으로 이송 중";
                case ProcessStage.Packing: return "팩킹 중";
                case ProcessStage.ToEnd: return "끝으로 이송 중";
                case ProcessStage.Done: return "공정 완료";
                default: return Idle;
            }
        }

        /// <summary>
        /// 큰 글자로 보일 지금 단계. 아직 컨베이어에 넘어온 키트가 없을 때는 로봇암 A가 일하는지로 "키팅 중"과 "대기 중"을 가른다.
        /// 키트가 있으면 그 키트의 단계가 우선이다(앞 키트가 끝났어도 새 키트를 만드는 중이면 단계 글은 마지막 키트의 것이다).
        /// </summary>
        public static string Headline(IProcessStatus status)
        {
            if (status.CurrentKit == null)
            {
                return status.StateOf(StationId.ArmA) == StationState.Running ? Kitting : Idle;
            }

            return Stage(status.CurrentStage);
        }

        /// <summary>지금 키트를 한 줄로(종류, 순번, 주문, 병원, 로트). 아직 없으면 안내 글이다.</summary>
        public static string KitSummary(KitData kit)
        {
            if (kit == null)
            {
                return "라인 위의 키트 없음";
            }

            return $"{kit.KitCode} 키트 #{kit.KitSequence} · 주문 #{kit.OrderSequence} · {kit.HospitalName} · 로트 {kit.LotNumber}";
        }
    }
}
