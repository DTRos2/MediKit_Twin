namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 집는 자세를 찾고(IPickPoseSource) 저장하고 지우는(IPickPoseRecorder) 일을 한 곳에서 맡는 저장 목록.
    /// 로봇암 패널은 저장·삭제·불러오기를 모두 하므로 이 하나만 알면 된다.
    /// </summary>
    public interface IPickPoseLibrary : IPickPoseSource, IPickPoseRecorder
    {
    }
}
