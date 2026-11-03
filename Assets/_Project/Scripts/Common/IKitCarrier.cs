namespace MediKitTwin.Common
{
    /// <summary>
    /// 키트 하나를 담은 박스를 싣고 다니는 것(컨베이어 위의 받침)이 알려 주는 접점. (FR-05, FR-11)
    /// 설비들은 서로를 직접 참조하지 않으므로, 박스에 닿은 설비(예: 라벨)가 "이 박스가 어느 키트의 것인지"를 구체 클래스 없이 읽도록 둔다.
    /// </summary>
    public interface IKitCarrier
    {
        /// <summary>이 박스에 담긴 키트.</summary>
        KitData Kit { get; }
    }
}
