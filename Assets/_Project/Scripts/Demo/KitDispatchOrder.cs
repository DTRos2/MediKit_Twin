using System;
using MediKitTwin.Common;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 두 키트 중 어느 쪽이 먼저 투입됐는지 가른다.
    /// 순번(KitSequence)은 주문을 다시 불러올 때마다 1부터 다시 세므로 실행 안에서 겹칠 수 있다. 그래서 둘 다 키팅 시작 시각(StartTime)이
    /// 있으면 그것으로 가른다. 시작 시각은 어느 주문에서 온 키트든 투입 순서대로 늘어난다.
    /// 시작 시각이 없는 키트(수동 키팅, 시험용 키트)는 순번으로 가른다.
    /// </summary>
    public static class KitDispatchOrder
    {
        /// <summary>kit가 other보다 먼저 투입됐으면 true. 같은 때에 투입됐거나 같은 키트이면 false다.</summary>
        public static bool IsEarlier(KitData kit, KitData other)
        {
            if (kit == null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (kit.StartTime.HasValue && other.StartTime.HasValue)
            {
                return kit.StartTime.Value < other.StartTime.Value;
            }

            return kit.KitSequence < other.KitSequence;
        }
    }
}
