namespace MediKitTwin.ArmA
{
    /// <summary>알려 준 자세 모음의 글자를 어딘가에 읽고 쓰는 쪽. 파일이든 메모리든 이것만 맞으면 바꿔 끼운다.</summary>
    public interface IPickPoseStorage
    {
        /// <summary>저장된 글자. 아직 저장한 적이 없으면 빈 글자다.</summary>
        string Read();

        /// <summary>글자를 저장한다. 이전 내용은 바뀐다.</summary>
        void Write(string text);
    }
}
