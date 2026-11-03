using MediKitTwin.Common;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 공정 전체의 현황을 읽는 쪽이 보는 모양. 패널은 이것만 알면 되고, 숫자를 어떤 신호로 세는지는 모른다.
    /// 설비 하나(로봇암 A)가 아니라 키팅부터 컨베이어 끝까지 모든 설비를 합친 값이다.
    /// </summary>
    public interface IProcessStatus
    {
        /// <summary>공정을 끝까지 마친 키트 수(컨베이어 끝에 닿았거나 출하한 키트).</summary>
        int CompletedCount { get; }

        /// <summary>검사에서 불합격해 불량으로 가려진 키트 수.</summary>
        int RejectedCount { get; }

        /// <summary>선반 칸 재고가 바닥나 알린 횟수.</summary>
        int StockShortageCount { get; }

        /// <summary>어떤 설비든 정지(Fault) 상태로 들어간 횟수.</summary>
        int FaultCount { get; }

        /// <summary>지금 라인에서 가장 뒤(투입 순번이 가장 큰)에 있는 키트. 아직 없으면 null.</summary>
        KitData CurrentKit { get; }

        /// <summary>그 키트가 어디까지 왔는지. 키트가 없으면 None.</summary>
        ProcessStage CurrentStage { get; }

        /// <summary>마지막으로 올라온 경고 한 줄. 없으면 빈 글자.</summary>
        string LastWarning { get; }

        StationState StateOf(StationId station);
    }
}
