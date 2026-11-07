using System;
using MediKitTwin.Common;

namespace MediKitTwin.Orders
{
    /// <summary>주문이 보류된 이유의 종류.</summary>
    public enum HoldReasonKind
    {
        /// <summary>선반 칸 재고가 없다. 그 칸이 보충되면 풀린다. (FR-13)</summary>
        StockShortage,

        /// <summary>설비가 Fault이다. 그 설비가 Fault에서 벗어나면 풀린다. (FR-16)</summary>
        StationFault,

        /// <summary>키트 코드에 맞는 레시피가 없다. 스스로 풀리지 않는다. (FR-02)</summary>
        RecipeMissing
    }

    /// <summary>
    /// 주문이 보류된 이유 하나. 한 주문이 여러 이유로 동시에 보류될 수 있고(예: 서로 다른 두 칸 부족),
    /// 이유가 모두 풀려야 재개하므로 이유를 값으로 구분해 모아 둔다.
    /// </summary>
    public readonly struct HoldReason : IEquatable<HoldReason>
    {
        private HoldReason(HoldReasonKind kind, string key)
        {
            Kind = kind;
            Key = key;
        }

        public HoldReasonKind Kind { get; }

        /// <summary>이유를 구분하는 값. 재고 부족은 선반 칸 ID, 설비 오류는 설비 이름이다.</summary>
        public string Key { get; }

        public static HoldReason StockShortage(string shelfSlotId) => new HoldReason(HoldReasonKind.StockShortage, shelfSlotId);

        public static HoldReason StationFault(StationId station) => new HoldReason(HoldReasonKind.StationFault, station.ToString());

        public static HoldReason RecipeMissing() => new HoldReason(HoldReasonKind.RecipeMissing, string.Empty);

        public bool Equals(HoldReason other) => Kind == other.Kind && Key == other.Key;

        public override bool Equals(object obj) => obj is HoldReason other && Equals(other);

        public override int GetHashCode() => (Kind.GetHashCode() * 397) ^ (Key?.GetHashCode() ?? 0);
    }
}
