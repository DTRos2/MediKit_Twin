using MediKitTwin.ArmA;
using MediKitTwin.Common;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키팅 현황을 화면에 적을 글로 바꾼다. 어떤 상태를 어떤 말로 부르는지를 패널 그리기 코드에서 떼어 한곳에 둔다.
    /// 설비 상태의 한글 표시는 대시보드 규칙(INTERFACES.md 3장): 대기, 가동, 정지. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class KittingStatusText
    {
        private const string NoKit = "지금 만드는 키트 없음";
        private const string NoResultYet = "아직 없음";

        /// <summary>상태 고리 가운데에 적는 영문.</summary>
        public static string StationRingLabel(StationState state)
        {
            switch (state)
            {
                case StationState.Running: return "RUN";
                case StationState.Fault: return "FAULT";
                default: return "IDLE";
            }
        }

        public static string StationText(StationState state)
        {
            switch (state)
            {
                case StationState.Running: return "가동";
                case StationState.Fault: return "정지";
                default: return "대기";
            }
        }

        public static string ItemText(KittingItemState state)
        {
            switch (state)
            {
                case KittingItemState.Picking: return "집는 중";
                case KittingItemState.Placing: return "옮기는 중";
                case KittingItemState.Placed: return "완료";
                case KittingItemState.Skipped: return "누락";
                default: return "대기";
            }
        }

        /// <summary>만드는 중인 키트를 한 줄로(종류, 순번, 주문, 병원). 쉬는 중이면 안내 글이다.</summary>
        public static string KitSummary(KittingPlan plan)
        {
            if (plan == null)
            {
                return NoKit;
            }

            KitData kit = plan.Kit;
            return $"{kit.KitCode} 키트 #{kit.KitSequence} · 주문 #{kit.OrderSequence} · {kit.HospitalName}";
        }

        /// <summary>로트와 박스 크기. 쉬는 중이면 빈 글자다.</summary>
        public static string KitDetail(KittingPlan plan)
        {
            return plan == null ? string.Empty : $"로트 {plan.Kit.LotNumber} · 박스 {plan.Recipe.BoxSize}";
        }

        /// <summary>"마지막 결과" 줄 오른쪽에 적는 짧은 글.</summary>
        public static string LastResultSummary(IKittingStatus status)
        {
            switch (status.LastResult)
            {
                case KittingResult.Completed: return $"{status.LastLotNumber} 완료";
                case KittingResult.StockShortage: return $"{status.LastLotNumber} 재고 부족";
                case KittingResult.Fault: return $"{status.LastLotNumber} 오류";
                default: return NoResultYet;
            }
        }

        /// <summary>실패했을 때만 이유를 알려 주는 긴 글. 성공했거나 아직 없으면 빈 글자다.</summary>
        public static string LastResultDetail(IKittingStatus status)
        {
            bool isFailure = status.LastResult == KittingResult.StockShortage || status.LastResult == KittingResult.Fault;
            return isFailure ? status.LastMessage ?? string.Empty : string.Empty;
        }
    }
}
