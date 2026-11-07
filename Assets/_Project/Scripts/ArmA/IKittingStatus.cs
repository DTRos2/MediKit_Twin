using System.Collections.Generic;

namespace MediKitTwin.ArmA
{
    /// <summary>키팅 현황을 읽는 쪽. 화면과 대시보드가 이 인터페이스만 보고 키팅 구현은 모른다.</summary>
    public interface IKittingStatus
    {
        /// <summary>지금 만들고 있는 키트의 계획. 쉬는 중이면 null이다.</summary>
        KittingPlan CurrentPlan { get; }

        /// <summary>CurrentPlan의 항목별 단계. CurrentPlan.Items와 같은 순서이며, 쉬는 중이면 비어 있다.</summary>
        IReadOnlyList<KittingItemState> ItemStates { get; }

        /// <summary>가장 최근에 끝난 키팅의 결과. 아직 없으면 None이다.</summary>
        KittingResult LastResult { get; }

        string LastMessage { get; }

        /// <summary>가장 최근에 끝난 키트의 로트번호. 아직 없으면 null이다.</summary>
        string LastLotNumber { get; }

        int CompletedKitCount { get; }

        /// <summary>재고 부족으로 멈춘 횟수. 주문이 보류되고 나중에 다시 만든다.</summary>
        int StockShortageCount { get; }

        int FaultCount { get; }
    }
}
