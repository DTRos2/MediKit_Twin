using System;
using System.Collections;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 구성품 하나를 선반에서 집어 박스 슬롯에 놓는 한 번의 일: 재고 확인, 물건 위치 확인, 집기, 슬롯 위치 확인, 놓기.
    /// 자동 키팅(KittingProcess)과 수동 키팅(ManualKitter)이 같은 순서를 쓰도록 한 곳에 둔다.
    /// 실패해도 재고를 되돌리거나 박스를 치우지 않는다. 그 뒤처리는 호출하는 쪽의 몫이다(자동과 수동이 서로 다르다).
    /// 모두 인터페이스로만 다루므로 Unity 씬 없이 시험할 수 있다.
    /// </summary>
    public sealed class ItemFiller
    {
        private readonly IStock _stock;
        private readonly IKitBoxLocator _boxLocator;
        private readonly IShelfItemLocator _shelf;
        private readonly IItemTransfer _transfer;

        public ItemFiller(IStock stock, IKitBoxLocator boxLocator, IShelfItemLocator shelf, IItemTransfer transfer)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _boxLocator = boxLocator ?? throw new ArgumentNullException(nameof(boxLocator));
            _shelf = shelf ?? throw new ArgumentNullException(nameof(shelf));
            _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
        }

        /// <param name="onPicking">집으러 가기 직전에 부른다. 필요 없으면 null.</param>
        /// <param name="onPlacing">놓으러 가기 직전에 부른다. 필요 없으면 null.</param>
        public IEnumerator Fill(FillRequest request, Action onPicking, Action onPlacing, FillResult result)
        {
            string shelfSlotId = request.ShelfSlotId;

            // 재고가 0이면 꺼내기를 시도해 StockEmpty 신호를 내보낸다. 빈 선반까지 로봇이 가는 헛걸음을 막는다.
            if (_stock.GetQuantity(shelfSlotId) <= 0)
            {
                _stock.TryTake(shelfSlotId, request.OrderSequence);
                result.Fail(FillFailure.StockEmpty, $"선반 칸 {shelfSlotId}의 재고가 없다.");
                yield break;
            }

            if (!_shelf.TryGetPickItem(shelfSlotId, out ShelfPickItem pickItem))
            {
                result.Fail(FillFailure.ItemNotVisible, $"선반 칸 {shelfSlotId}에 집을 물건이 보이지 않는다.");
                yield break;
            }

            onPicking?.Invoke();
            var pick = new TransferResult();
            yield return _transfer.Pick(new ItemPickRequest(pickItem, () =>
            {
                bool isTaken = _stock.TryTake(shelfSlotId, request.OrderSequence);
                if (isTaken)
                {
                    result.MarkTaken();
                }

                return isTaken;
            }), pick);

            if (!pick.Succeeded)
            {
                bool isStockProblem = pick.Failure == TransferFailure.CommitRejected;
                result.Fail(
                    isStockProblem ? FillFailure.StockEmpty : FillFailure.PickFailed,
                    isStockProblem ? $"선반 칸 {shelfSlotId}의 재고가 없다." : $"선반 칸 {shelfSlotId}에서 집지 못했다. {pick.Detail}");
                yield break;
            }

            if (!_boxLocator.TryGetSlotTarget(request.SlotLocalPosition, out BoxSlotTarget target))
            {
                result.Fail(FillFailure.BoxMissing, "박스가 사라져 구성품을 놓을 수 없다.");
                yield break;
            }

            onPlacing?.Invoke();
            var place = new TransferResult();
            yield return _transfer.Place(new ItemPlaceRequest(target.WorldPose, target.Parent), place);
            if (!place.Succeeded)
            {
                result.Fail(FillFailure.PlaceFailed, $"슬롯 {request.SlotNumber}에 놓지 못했다. {place.Detail}");
                yield break;
            }

            result.Succeed();
        }
    }
}
