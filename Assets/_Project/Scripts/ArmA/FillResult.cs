namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 구성품 하나를 선반에서 집어 박스에 놓은 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 넘기고 끝난 뒤 읽는다.
    /// 쥐는 순간 재고가 줄었으면 이후에 실패해도 IsTaken은 true로 남아, 호출하는 쪽이 재고를 되돌릴 수 있다.
    /// </summary>
    public sealed class FillResult
    {
        public bool Succeeded { get; private set; }

        public FillFailure Failure { get; private set; }

        /// <summary>실패 이유를 사람이 읽는 문장. 성공하면 빈 문자열이다.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>재고에서 이미 꺼냈으면 true.</summary>
        public bool IsTaken { get; private set; }

        public void MarkTaken()
        {
            IsTaken = true;
        }

        public void Succeed()
        {
            Succeeded = true;
            Failure = FillFailure.None;
            Message = string.Empty;
        }

        public void Fail(FillFailure failure, string message)
        {
            Succeeded = false;
            Failure = failure;
            Message = message ?? string.Empty;
        }
    }
}
