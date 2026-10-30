using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키트 1개를 만드는 순서: 박스를 놓고, 계획의 항목마다 선반에서 집어 박스 슬롯에 놓는다. (FR-03)
    /// 항목 하나를 집어 놓는 일은 ItemFiller, 박스를 기다리는 일은 BoxAwaiter가 맡고, 이 클래스는 항목 순서와 중단 처리를 맡는다.
    /// 재고, 박스, 위치 조회, 이송은 모두 인터페이스로만 다루므로 Unity 씬 없이도 시험할 수 있다.
    /// 중간에 멈추면 이미 꺼낸 구성품을 재고로 되돌리고 박스를 치운다(주문 보류와 재개의 전제, FR-13).
    /// 시간은 호출하는 쪽이 주는 시각 함수로만 알아 시뮬레이션 시간(배속)을 따른다.
    /// 진행 상황은 IKittingObserver로만 알리므로 화면이 키팅 순서를 알 필요가 없다.
    /// </summary>
    public sealed class KittingProcess
    {
        private readonly IStock _stock;
        private readonly IBoxSupply _boxSupply;
        private readonly IItemTransfer _transfer;
        private readonly BoxAwaiter _boxAwaiter;
        private readonly ItemFiller _filler;
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
            _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
            _boxAwaiter = new BoxAwaiter(boxSupply, boxLocator, now, boxWaitTimeoutSeconds);
            _filler = new ItemFiller(stock, boxLocator, shelf, transfer);
            _observer = observer;
        }

        public IEnumerator Run(KittingPlan plan, KittingOutcome outcome)
        {
            // 재고에서 꺼냈지만 아직 키트가 완성되지 않은 구성품의 선반 칸. 중간에 멈추면 이 칸들로 되돌린다.
            var taken = new List<string>();

            _observer?.PlanStarted(plan);

            bool isBoxReady = false;
            yield return _boxAwaiter.Ensure(plan.Kit, plan.Recipe.BoxSize, ready => isBoxReady = ready);
            if (!isBoxReady)
            {
                Abort(plan, taken, outcome, KittingResult.Fault, null, "박스가 지그에 놓이지 않았다.");
                yield break;
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

                int itemIndex = index;
                var fill = new FillResult();
                yield return _filler.Fill(
                    new FillRequest(item.ShelfSlotId, item.SlotLocalPosition, item.SlotNumber, orderSequence),
                    () => _observer?.ItemStateChanged(itemIndex, KittingItemState.Picking),
                    () => _observer?.ItemStateChanged(itemIndex, KittingItemState.Placing),
                    fill);

                // 쥔 뒤에 실패해도 이미 꺼낸 재고이므로 되돌릴 목록에 넣는다.
                if (fill.IsTaken)
                {
                    taken.Add(item.ShelfSlotId);
                }

                if (!fill.Succeeded)
                {
                    KittingResult result = fill.Failure == FillFailure.StockEmpty ? KittingResult.StockShortage : KittingResult.Fault;
                    Abort(plan, taken, outcome, result, item.ShelfSlotId, fill.Message);
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
