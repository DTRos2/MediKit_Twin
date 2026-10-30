using System;
using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 등록된 키트 코드만 받아들인다. 코드 목록은 레시피에서 받아 주입하므로 키트 종류를 늘려도 이 클래스는 고치지 않는다. (NFR-07)
    /// </summary>
    public sealed class KnownKitCodeValidator : IOrderRowValidator
    {
        private readonly HashSet<string> _knownKitCodes = new HashSet<string>(StringComparer.Ordinal);

        public KnownKitCodeValidator(IEnumerable<string> knownKitCodes)
        {
            if (knownKitCodes == null)
            {
                throw new ArgumentNullException(nameof(knownKitCodes));
            }

            foreach (string code in knownKitCodes)
            {
                if (!string.IsNullOrWhiteSpace(code))
                {
                    _knownKitCodes.Add(code.Trim().ToUpperInvariant());
                }
            }
        }

        /// <summary>등록된 키트 코드(대문자). 기본 주문 세트가 코드마다 1건씩 만드는 데 같은 목록을 쓴다.</summary>
        public IReadOnlyCollection<string> KnownKitCodes => _knownKitCodes;

        public bool TryValidate(OrderRecord order, int lineNumber, OrderValidationContext context, out OrderIssue issue)
        {
            if (_knownKitCodes.Contains(order.KitCode ?? string.Empty))
            {
                issue = default;
                return true;
            }

            issue = new OrderIssue(OrderIssueCodes.UnknownKitCode,
                $"{lineNumber}행을 건너뛴다: 알 수 없는 키트 코드 '{order.KitCode}'");
            return false;
        }
    }
}
