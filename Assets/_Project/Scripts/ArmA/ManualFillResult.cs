namespace MediKitTwin.ArmA
{
    /// <summary>수동으로 구성품 하나를 채운 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 넘기고 끝난 뒤 읽는다.</summary>
    public sealed class ManualFillResult
    {
        public bool Succeeded { get; private set; }

        /// <summary>화면에 보여 줄 문장. 성공이면 무엇을 채웠는지, 실패면 이유다.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>이번으로 키트의 모든 슬롯이 찼으면 true.</summary>
        public bool CompletedKit { get; private set; }

        public void Succeed(string message, bool completedKit)
        {
            Succeeded = true;
            Message = message;
            CompletedKit = completedKit;
        }

        public void Fail(string message)
        {
            Succeeded = false;
            Message = message;
            CompletedKit = false;
        }
    }
}
