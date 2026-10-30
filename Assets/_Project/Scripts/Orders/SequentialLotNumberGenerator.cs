using System.Globalization;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 접두어와 0으로 채운 일련번호(예: LOT-0001)를 만든다.
    /// 실제 시간을 쓰지 않으므로 같은 주문 파일이면 같은 로트번호가 나와 NFR-06(재현성)을 해치지 않는다.
    /// </summary>
    public sealed class SequentialLotNumberGenerator : ILotNumberGenerator
    {
        private readonly string _prefix;
        private readonly int _digits;
        private int _next;

        public SequentialLotNumberGenerator(string prefix, int digits, int firstNumber = 1)
        {
            _prefix = prefix;
            _digits = digits;
            _next = firstNumber;
        }

        public string Next()
        {
            string number = (_next++).ToString(CultureInfo.InvariantCulture).PadLeft(_digits, '0');
            return _prefix + number;
        }
    }
}
