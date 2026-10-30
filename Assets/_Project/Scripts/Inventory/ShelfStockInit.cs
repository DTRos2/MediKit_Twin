namespace MediKitTwin.Inventory
{
    /// <summary>선반 칸 하나의 시작 재고.</summary>
    public readonly struct ShelfStockInit
    {
        public ShelfStockInit(string shelfSlotId, int quantity)
        {
            ShelfSlotId = shelfSlotId;
            Quantity = quantity;
        }

        public string ShelfSlotId { get; }

        public int Quantity { get; }
    }
}
