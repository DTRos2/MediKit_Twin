using System;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키트 하나를 만든 결과에 따라 알리고 설비 상태를 바꾼다. 완료면 KitReady를 알리고, 재고 부족은 그대로 넘어가며
    /// (주문 보류는 StockEmpty를 받은 주문 관리가 이미 했다), 오류면 설비를 정지로 바꾸고 경고한 뒤 쉴 시간을 알려 준다.
    /// 알림과 상태는 인터페이스로만 다루므로 Unity와 이벤트 버스 없이 시험할 수 있다.
    /// </summary>
    public sealed class KittingOutcomeHandler
    {
        private readonly IKittingEventSink _events;
        private readonly IStationStateReporter _station;
        private readonly float _faultRecoverySeconds;

        public KittingOutcomeHandler(IKittingEventSink events, IStationStateReporter station, float faultRecoverySeconds)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _station = station ?? throw new ArgumentNullException(nameof(station));
            _faultRecoverySeconds = faultRecoverySeconds;
        }

        /// <summary>결과를 처리한다. 호출한 쪽이 기다려야 하는 시간(초)을 돌려준다. 기다릴 필요가 없으면 0이다.</summary>
        public float Handle(KitData kit, KittingOutcome outcome)
        {
            switch (outcome.Result)
            {
                case KittingResult.Completed:
                    _events.KitReady(kit);
                    return 0f;

                case KittingResult.Fault:
                    _station.Set(StationState.Fault);
                    _events.WarningRaised(KittingIssueCodes.ArmFault, outcome.Message);
                    return _faultRecoverySeconds;

                default:
                    return 0f;
            }
        }
    }
}
