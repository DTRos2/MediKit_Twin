namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 파일의 글자를 읽어 오는 곳. 파일, StreamingAssets, 테스트용 문자열을 같은 방식으로 다루기 위해 파서와 분리했다.
    /// </summary>
    public interface IOrderTextSource
    {
        /// <summary>사람이 읽을 수 있는 위치(경고 메시지에 쓴다).</summary>
        string Description { get; }

        /// <summary>내용을 읽는다. 없거나 읽을 수 없으면 false이다.</summary>
        bool TryReadAll(out string text);
    }
}
