namespace MediKitTwin.Orders
{
    /// <summary>키트마다 실행 안에서 유일한 로트번호를 만든다. 형식을 바꿀 때 이 인터페이스만 새로 구현한다.</summary>
    public interface ILotNumberGenerator
    {
        string Next();
    }
}
