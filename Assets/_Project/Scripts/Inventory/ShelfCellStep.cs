namespace MediKitTwin.Inventory
{
    /// <summary>칸 안 물건 한 개에 일어나는 변화의 종류.</summary>
    public enum ShelfCellStepKind
    {
        /// <summary>슬롯의 물건이 사라진다(집혔다).</summary>
        Vacate,

        /// <summary>물건이 한 슬롯에서 다른 슬롯으로 밀려 나온다.</summary>
        Move,

        /// <summary>빈 슬롯에 물건이 채워진다(보충, 되돌림).</summary>
        Fill
    }

    /// <summary>
    /// 칸 안 변화 한 단계. 변화는 순서가 의미를 가지므로(방금 비운 슬롯으로 다른 물건이 밀려 올 수 있다)
    /// 표시하는 쪽은 단계를 받은 순서대로 처리해야 한다.
    /// </summary>
    public readonly struct ShelfCellStep
    {
        private ShelfCellStep(ShelfCellStepKind kind, int fromSlot, int toSlot)
        {
            Kind = kind;
            FromSlot = fromSlot;
            ToSlot = toSlot;
        }

        public ShelfCellStepKind Kind { get; }

        /// <summary>Vacate와 Move의 출발 슬롯. Fill에서는 -1이다.</summary>
        public int FromSlot { get; }

        /// <summary>Move와 Fill의 도착 슬롯. Vacate에서는 -1이다.</summary>
        public int ToSlot { get; }

        public static ShelfCellStep Vacate(int slot) => new ShelfCellStep(ShelfCellStepKind.Vacate, slot, -1);

        public static ShelfCellStep Move(int fromSlot, int toSlot) => new ShelfCellStep(ShelfCellStepKind.Move, fromSlot, toSlot);

        public static ShelfCellStep Fill(int slot) => new ShelfCellStep(ShelfCellStepKind.Fill, -1, slot);
    }
}
