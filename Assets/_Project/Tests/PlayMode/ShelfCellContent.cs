namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 선반 칸 하나에 놓여 있는 구성품. 칸 안의 물건은 씬에 이미 놓여 있어 바꿀 수 없으므로, 랜덤 레시피는 이 짝(칸, 구성품)만 골라 쓴다.
    /// </summary>
    public readonly struct ShelfCellContent
    {
        public ShelfCellContent(string shelfSlotId, string componentId)
        {
            ShelfSlotId = shelfSlotId;
            ComponentId = componentId;
        }

        /// <summary>선반 칸 ID(예: B-2).</summary>
        public string ShelfSlotId { get; }

        /// <summary>그 칸에 놓인 구성품 종류 ID(예: needle).</summary>
        public string ComponentId { get; }
    }
}
