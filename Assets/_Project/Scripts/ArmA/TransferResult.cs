namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 이송 단계(집기, 놓기)의 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 이 객체를 넘기고 끝난 뒤 읽는다.
    /// </summary>
    public sealed class TransferResult
    {
        public bool Succeeded { get; private set; }

        public TransferFailure Failure { get; private set; }

        public void Succeed()
        {
            Succeeded = true;
            Failure = TransferFailure.None;
        }

        public void Fail(TransferFailure failure)
        {
            Succeeded = false;
            Failure = failure;
        }
    }
}
