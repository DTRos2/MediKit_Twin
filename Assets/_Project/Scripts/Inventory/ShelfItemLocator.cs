using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 선반 칸 ID로 그 칸 화면(ShelfCellView)을 찾아, 집을 물건의 자리를 알려 준다. (IShelfItemLocator)
    /// 칸 화면을 어디서 모으는지는 모르고 조회 함수만 받으므로, 선반 구성이 바뀌어도 이 클래스는 그대로다.
    /// </summary>
    public sealed class ShelfItemLocator : IShelfItemLocator
    {
        private readonly Func<string, ShelfCellView> _findCell;

        public ShelfItemLocator(Func<string, ShelfCellView> findCell)
        {
            _findCell = findCell ?? throw new ArgumentNullException(nameof(findCell));
        }

        public bool TryGetPickItem(string shelfSlotId, out ShelfPickItem item)
        {
            item = default;

            ShelfCellView cell = shelfSlotId != null ? _findCell(shelfSlotId) : null;
            if (cell == null || !cell.TryGetFrontItem(out Transform front))
            {
                return false;
            }

            // 칸은 +Z 쪽이 열려 있다(에디터 빌더의 OpenBox). 칸 오브젝트의 forward가 곧 칸 밖으로 나가는 방향이다.
            item = new ShelfPickItem(new Pose(front.position, front.rotation), cell.transform.forward, front.gameObject);
            return true;
        }
    }
}
