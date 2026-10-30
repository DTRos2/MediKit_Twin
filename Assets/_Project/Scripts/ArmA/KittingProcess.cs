using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키트 1개를 만드는 순서: 박스를 놓고, 계획의 항목마다 선반에서 집어 박스 슬롯에 놓는다. (FR-03)
    /// 재고, 박스, 위치 조회, 이송은 모두 인터페이스로만 다루므로 Unity 씬 없이도 시험할 수 있다.
    /// 중간에 멈추면 이미 꺼낸 구성품을 재고로 되돌리고 박스를 치운다(주문 보류와 재개의 전제, FR-13).
    /// 시간은 호출하는 쪽이 주는 시각 함수로만 알아 시뮬레이션 시간(배속)을 따른다.
    /// 진행 상황은 IKittingObserver로만 알리므로 화면이 키팅 순서를 알 필요가 없다.
    /// </summary>
    public sealed class KittingProcess
    {
        private readonly IStock _stock;
        private readonly IBoxSupply _boxSupply;
        private readonly IKitBoxLocator _boxLocator;
        private readonly IShelfItemLocator _shelf;
        private readonly IItemTransfer _transfer;
        private readonly Func<float> _now;
        private readonly float _boxWaitTimeoutSeconds;
        private readonly IKittingObserver _observer;

        /// <param name="observer">진행을 알릴 쪽. 필요 없으면 null.</param>
        public KittingProcess(
            IStock stock,
            IBoxSupply boxSupply,
            IKitBoxLocator boxLocator,
            IShelfItemLocator shelf,
            IItemTransfer transfer,
            Func<float> now,
            float boxWaitTimeoutSeconds,
            IKittingObserver observer = null)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _boxSupply = boxSupply ?? throw new ArgumentNullException(nameof(boxSupply));
            _boxLocator = boxLocator ?? throw new ArgumentNullException(nameof(boxLocator));
            _shelf = shelf ?? throw new ArgumentNullException(nameof(shelf));
            _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _boxWaitTimeoutSeconds = boxWaitTimeoutSeconds;
            _observer = observer;
        }

        public IEnumerator Run(KittingPlan plan, KittingOutcome outcome)
        {
            // 재고에서 꺼냈지만 아직 키트가 완성되지 않은 구성품의 선반 칸. 중간에 멈추면 이 칸들로 되돌린다.
            var taken = new List<string>();

            _observer?.PlanStarted(plan);
            _boxSupply.PlaceBox(plan.Kit, plan.Recipe.BoxSize);

            float deadline = _now() + _boxWaitTimeoutSeconds;
            while (!_boxLocator.TryGetSlotTarget(Vector3.zero, out _))
            {
                if (_now() > deadline)
                {
                    Abort(plan, taken, outcome, KittingResult.Fault, null, "박스가 지그에 놓이지 않았다.");
                    yield break;
                }

                yield return null;
            }

            int orderSequence = plan.Kit.OrderSequence;
            for (int index = 0; index < plan.Items.Count; index++)
            {
                KittingItem item = plan.Items[index];

                // 누락 규칙이 빠뜨리기로 한 구성품은 집지 않으므로 재고도 줄지 않는다.
                if (item.IsSkipped)
                {
                    continue;
                }

                // 재고가 0이면 꺼내기를 시도해 StockEmpty 신호를 내보낸다. 빈 선반까지 로봇이 가는 헛걸음을 막는다.
                if (_stock.GetQuantity(item.ShelfSlotId) <= 0)
                {
                    _stock.TryTake(item.ShelfSlotId, orderSequence);
                    Abort(plan, taken, outcome, KittingResult.StockShortage, item.ShelfSlotId, $"선반 칸 {item.ShelfSlotId}의 재고가 없다.");
                    yield break;
                }

                if (!_shelf.TryGetPickItem(item.ShelfSlotId, out ShelfPickItem pickItem))
                {
                    Abort(plan, taken, outcome, KittingResult.Fault, item.ShelfSlotId, $"선반 칸 {item.ShelfSlotId}에 집을 물건이 보이지 않는다.");
                    yield break;
                }

                string shelfSlotId = item.ShelfSlotId;
                bool isTaken = false;
                var pick = new TransferResult();
                _observer?.ItemStateChanged(index, KittingItemState.Picking);
                yield return _transfer.Pick(new ItemPickRequest(pickItem, () =>
                {
                    isTaken = _stock.TryTake(shelfSlotId, orderSequence);
                    return isTaken;
                }), pick);

                if (isTaken)
                {
                    taken.Add(shelfSlotId);
                }

                if (!pick.Succeeded)
                {
                    bool isStockProblem = pick.Failure == TransferFailure.CommitRejected;
                    Abort(plan, taken, outcome,
                        isStockProblem ? KittingResult.StockShortage : KittingResult.Fault,
                        shelfSlotId,
                        isStockProblem ? $"선반 칸 {shelfSlotId}의 재고가 없다." : $"선반 칸 {shelfSlotId}에서 집지 못했다. 로봇이 닿지 못하는 위치이거나 동작 시간이 초과되었다.");
                    yield break;
                }

                if (!_boxLocator.TryGetSlotTarget(item.SlotLocalPosition, out BoxSlotTarget target))
                {
                    Abort(plan, taken, outcome, KittingResult.Fault, shelfSlotId, "박스가 사라져 구성품을 놓을 수 없다.");
                    yield break;
                }

                _observer?.ItemStateChanged(index, KittingItemState.Placing);
                var place = new TransferResult();
                yield return _transfer.Place(new ItemPlaceRequest(target.WorldPose, target.Parent), place);
                if (!place.Succeeded)
                {
                    Abort(plan, taken, outcome, KittingResult.Fault, shelfSlotId, $"슬롯 {item.SlotNumber}에 놓지 못했다.");
                    yield break;
                }

                _observer?.ItemStateChanged(index, KittingItemState.Placed);
            }

            outcome.Complete();
            _observer?.Finished(plan, outcome);
        }

        private void Abort(KittingPlan plan, List<string> taken, KittingOutcome outcome, KittingResult result, string shelfSlotId, string message)
        {
            _transfer.Discard();

            foreach (string shelfSlot in taken)
            {
                _stock.Return(shelfSlot);
            }

            _boxSupply.RemoveBox();
            outcome.Fail(result, shelfSlotId, message);
            _observer?.Finished(plan, outcome);
        }
    }
}
