namespace MediKitTwin.ArmA
{
    /// <summary>이동 중에만 켰다가 끝나면 되돌리는 상태(충돌 무시 등). Begin과 End는 짝으로 부른다.</summary>
    public interface IMotionScope
    {
        void Begin();

        void End();
    }
}
