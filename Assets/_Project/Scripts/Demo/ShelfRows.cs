using System.Collections.Generic;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 칸 ID 목록을 행 글자별로 묶는다. 칸 ID의 첫 글자(행)가 바뀔 때마다 새 행으로 나눠, 화면이 선반과 같은 3행 모양으로 그려지게 한다.
    /// 칸이 몇 개이고 행이 몇 개인지는 재고가 알려 주는 대로 따른다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ShelfRows
    {
        public static IReadOnlyList<ShelfRow> Group(IReadOnlyList<string> slotIds)
        {
            var rows = new List<ShelfRow>();
            if (slotIds == null)
            {
                return rows;
            }

            List<string> current = null;
            char currentLetter = '\0';
            foreach (string slotId in slotIds)
            {
                // 비어 있는 ID는 행을 알 수 없으므로 건너뛴다.
                if (string.IsNullOrEmpty(slotId))
                {
                    continue;
                }

                if (current == null || slotId[0] != currentLetter)
                {
                    currentLetter = slotId[0];
                    current = new List<string>();
                    rows.Add(new ShelfRow(currentLetter, current));
                }

                current.Add(slotId);
            }

            return rows;
        }
    }
}
