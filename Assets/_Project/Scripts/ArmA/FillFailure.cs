namespace MediKitTwin.ArmA
{
    /// <summary>구성품 하나를 선반에서 집어 박스에 놓는 일이 실패한 이유.</summary>
    public enum FillFailure
    {
        None,

        /// <summary>선반 칸의 재고가 없다(처음부터 0이거나 쥐는 순간 꺼내지 못했다).</summary>
        StockEmpty,

        /// <summary>재고는 있지만 선반에 집을 물건이 보이지 않는다.</summary>
        ItemNotVisible,

        /// <summary>로봇이 물건을 집지 못했다.</summary>
        PickFailed,

        /// <summary>놓을 박스가 없다(사라졌거나 아직 내려오는 중이다).</summary>
        BoxMissing,

        /// <summary>로봇이 박스 슬롯에 놓지 못했다.</summary>
        PlaceFailed
    }
}
