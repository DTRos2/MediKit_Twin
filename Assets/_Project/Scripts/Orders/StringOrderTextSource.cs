namespace MediKitTwin.Orders
{
    /// <summary>이미 가진 문자열을 주문 파일 내용으로 쓴다. 테스트와 인스펙터에 직접 적은 주문에 쓴다.</summary>
    public sealed class StringOrderTextSource : IOrderTextSource
    {
        private readonly string _text;

        public StringOrderTextSource(string text, string description = "문자열")
        {
            _text = text;
            Description = description;
        }

        public string Description { get; }

        public bool TryReadAll(out string text)
        {
            text = _text;
            return text != null;
        }
    }
}
