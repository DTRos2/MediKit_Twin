using System.Collections.Generic;
using System.Linq;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 투입은 되었지만 아직 만들기를 끝내지 못한(KitReady 전) 키트를 KitSequence로 추적한다.
    /// 설비가 여러 대여도 키트마다 따로 기록하므로, 어느 주문이 보류되든 그 주문의 만들던 키트를 정확히 되돌릴 수 있다.
    /// </summary>
    public sealed class InFlightKits
    {
        private readonly SortedDictionary<int, KitRequest> _byKitSequence = new SortedDictionary<int, KitRequest>();

        public int Count => _byKitSequence.Count;

        public void Add(int kitSequence, KitRequest request)
        {
            _byKitSequence[kitSequence] = request;
        }

        /// <summary>키트를 다 만들었다(KitReady). 기록에 없으면 무시한다. 보류로 이미 되돌려진 키트일 수 있다.</summary>
        public void Complete(int kitSequence)
        {
            _byKitSequence.Remove(kitSequence);
        }

        /// <summary>주문의 만들던 키트를 기록에서 빼서 투입 순서대로 돌려준다.</summary>
        public IReadOnlyList<KitRequest> TakeByOrder(int orderSequence)
        {
            List<int> sequences = _byKitSequence
                .Where(pair => pair.Value.OrderSequence == orderSequence)
                .Select(pair => pair.Key)
                .ToList();

            var taken = new List<KitRequest>(sequences.Count);
            foreach (int sequence in sequences)
            {
                taken.Add(_byKitSequence[sequence]);
                _byKitSequence.Remove(sequence);
            }

            return taken;
        }

        /// <summary>만들던 키트가 있는 주문 순번(중복 없음).</summary>
        public IReadOnlyList<int> OrderSequences()
        {
            return _byKitSequence.Values.Select(request => request.OrderSequence).Distinct().ToList();
        }
    }
}
