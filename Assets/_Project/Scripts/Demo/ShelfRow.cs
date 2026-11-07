using System.Collections.Generic;

namespace MediKitTwin.Demo
{
    /// <summary>선반 한 행: 행 글자(A, B, C)와 그 행의 칸 ID들(A-1 ~ A-4).</summary>
    public sealed class ShelfRow
    {
        public ShelfRow(char letter, IReadOnlyList<string> slotIds)
        {
            Letter = letter;
            SlotIds = slotIds;
        }

        public char Letter { get; }

        public IReadOnlyList<string> SlotIds { get; }
    }
}
