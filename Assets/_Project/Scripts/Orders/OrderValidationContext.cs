using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 한 번의 파싱 동안 검증기들이 공유하는 상태. 행 하나만 봐서는 판단할 수 없는 규칙(순번 중복)이
    /// 필요로 하며, 파싱마다 새로 만들어 검증기 자체는 상태 없이 재사용한다.
    /// </summary>
    public sealed class OrderValidationContext
    {
        private readonly HashSet<int> _acceptedSequences = new HashSet<int>();

        /// <summary>이미 받아들인 순번이면 false이다. 아니면 기록하고 true를 돌려준다.</summary>
        public bool TryRegisterSequence(int sequence)
        {
            return _acceptedSequences.Add(sequence);
        }
    }
}
