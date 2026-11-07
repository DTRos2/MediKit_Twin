using System;
using System.Collections.Generic;
using MediKitTwin.Common;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 관리의 정책 값. 경고 기준 횟수와 Fault로 주문을 보류할 설비는 코드에 쓰지 않고 호출하는 쪽(인스펙터, SO)이 정한다.
    /// </summary>
    public sealed class OrderManagerSettings
    {
        private readonly HashSet<StationId> _faultHoldStations;

        public OrderManagerSettings(int repeatedHoldWarningCount, IEnumerable<StationId> faultHoldStations)
        {
            if (repeatedHoldWarningCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatedHoldWarningCount), "경고 기준 횟수는 1 이상이어야 한다.");
            }

            RepeatedHoldWarningCount = repeatedHoldWarningCount;
            _faultHoldStations = new HashSet<StationId>(faultHoldStations ?? throw new ArgumentNullException(nameof(faultHoldStations)));
        }

        /// <summary>같은 주문이 이 횟수만큼 보류되면 경고한다. (FR-13: 3번)</summary>
        public int RepeatedHoldWarningCount { get; }

        /// <summary>이 설비가 Fault이면 지금 만드는 주문을 보류한다. (FR-16)</summary>
        public bool HoldsOrderOnFault(StationId station) => _faultHoldStations.Contains(station);
    }
}
