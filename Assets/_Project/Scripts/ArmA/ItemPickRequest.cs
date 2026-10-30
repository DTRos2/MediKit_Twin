using System;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 선반에서 구성품 하나를 집어 달라는 요청. TryCommit은 로봇암이 물건을 쥐는 순간에 부른다.
    /// 재고를 줄이는 일을 쥐는 순간으로 미뤄서, 선반의 물건이 사라지는 때와 로봇이 집는 때가 맞게 한다.
    /// false를 돌려주면 이송을 멈춘다(재고가 이미 비었다).
    /// </summary>
    public readonly struct ItemPickRequest
    {
        public ItemPickRequest(ShelfPickItem item, Func<bool> tryCommit)
        {
            Item = item;
            TryCommit = tryCommit;
        }

        public ShelfPickItem Item { get; }

        public Func<bool> TryCommit { get; }
    }
}
