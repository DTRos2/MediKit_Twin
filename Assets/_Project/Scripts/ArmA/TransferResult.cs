namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 이송 단계(집기, 놓기)의 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 이 객체를 넘기고 끝난 뒤 읽는다.
    /// </summary>
    public sealed class TransferResult
    {
        public bool Succeeded { get; private set; }

        public TransferFailure Failure { get; private set; }

        /// <summary>어느 단계에서 왜 실패했는지 사람이 읽는 설명. 성공했거나 설명이 없으면 빈 문자열이다.</summary>
        public string Detail { get; private set; } = string.Empty;

        public void Succeed()
        {
            Succeeded = true;
            Failure = TransferFailure.None;
            Detail = string.Empty;
        }

        public void Fail(TransferFailure failure, string detail = "")
        {
            Succeeded = false;
            Failure = failure;
            Detail = detail ?? string.Empty;
        }
    }
}
