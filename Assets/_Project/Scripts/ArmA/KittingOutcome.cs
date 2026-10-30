namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키팅 진행의 결과. KittingProcess.Run은 코루틴이라 값을 돌려줄 수 없어서, 호출하는 쪽이 이 객체를 넘기고 끝난 뒤 읽는다.
    /// </summary>
    public sealed class KittingOutcome
    {
        public KittingResult Result { get; private set; }

        /// <summary>멈추게 한 선반 칸 ID. 재고 부족이나 집기 실패일 때만 있다.</summary>
        public string FailedShelfSlotId { get; private set; }

        public string Message { get; private set; }

        public bool IsFinished => Result != KittingResult.None;

        public void Complete()
        {
            Result = KittingResult.Completed;
        }

        public void Fail(KittingResult result, string shelfSlotId, string message)
        {
            Result = result;
            FailedShelfSlotId = shelfSlotId;
            Message = message;
        }
    }
}
