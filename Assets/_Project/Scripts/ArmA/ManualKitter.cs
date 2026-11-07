using System;
using System.Collections;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 사람이 고른 선반 칸의 구성품 하나를 로봇이 집어 박스 슬롯에 놓게 한다. 순서는 박스 확인, 놓을 슬롯 정하기(ManualKittingSession),
    /// 집어 놓기(ItemFiller)이고, 실패하면 쥐고 있던 것을 버리고 꺼낸 재고를 되돌린다. 박스는 치우지 않는다(이미 채운 것을 지키기 위해서다).
    /// 자동 키팅과 같은 ItemFiller를 쓰므로 로봇 동작은 두 모드에서 똑같다. 씬 없이 시험할 수 있다.
    /// </summary>
    public sealed class ManualKitter
    {
        private const int NoSequence = 0;
        private const string ManualLotNumber = "MANUAL";

        private readonly ManualKittingSession _session;
        private readonly IStock _stock;
        private readonly IKitBoxLocator _boxLocator;
        private readonly BoxAwaiter _boxAwaiter;
        private readonly ItemFiller _filler;
        private readonly IItemTransfer _transfer;
        private readonly int _orderSequence;

        public ManualKitter(
            ManualKittingSession session,
            IStock stock,
            IKitBoxLocator boxLocator,
            BoxAwaiter boxAwaiter,
            ItemFiller filler,
            IItemTransfer transfer,
            int orderSequence)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _boxLocator = boxLocator ?? throw new ArgumentNullException(nameof(boxLocator));
            _boxAwaiter = boxAwaiter ?? throw new ArgumentNullException(nameof(boxAwaiter));
            _filler = filler ?? throw new ArgumentNullException(nameof(filler));
            _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
            _orderSequence = orderSequence;
        }

        public IEnumerator Fill(string shelfSlotId, ManualFillResult result)
        {
            RecipeSO recipe = _session.Recipe;
            if (recipe == null)
            {
                result.Fail("키트 종류를 먼저 고른다.");
                yield break;
            }

            // 박스가 없으면 새로 놓고 기다린다. 이미 있으면 바로 끝난다.
            bool isBoxReady = false;
            yield return _boxAwaiter.Ensure(NewKit(recipe), recipe.BoxSize, ready => isBoxReady = ready);
            if (!isBoxReady)
            {
                result.Fail("박스가 지그에 놓이지 않았다.");
                yield break;
            }

            // 박스가 바뀌었다면(예: 치워지고 새로 내려옴) 채운 슬롯 기록이 낡았으므로 먼저 비운다.
            if (_boxLocator.TryGetSlotTarget(Vector3.zero, out BoxSlotTarget box))
            {
                _session.NotifyBox(box.Parent);
            }

            if (!_session.TryPrepare(shelfSlotId, out RecipeItem item, out string reason))
            {
                result.Fail(reason);
                yield break;
            }

            var fill = new FillResult();
            yield return _filler.Fill(new FillRequest(shelfSlotId, item.SlotLocalPosition, item.SlotNumber, _orderSequence), null, null, fill);

            if (!fill.Succeeded)
            {
                // 쥐고 있던 것을 버리고, 이미 꺼낸 재고는 되돌린다.
                _transfer.Discard();
                if (fill.IsTaken)
                {
                    _stock.Return(shelfSlotId);
                }

                result.Fail(fill.Message);
                yield break;
            }

            _session.MarkFilled(item.SlotNumber);
            string progress = $"{shelfSlotId}({item.ComponentId})를 슬롯 {item.SlotNumber}에 놓았다. {_session.FilledCount}/{recipe.Items.Count}";
            result.Succeed(_session.IsComplete ? $"{progress} - {recipe.KitCode} 키트 완성" : progress, _session.IsComplete);
        }

        // 수동 키트는 주문과 무관하다. 박스 공급부는 키트 종류와 크기만 쓴다.
        private static KitData NewKit(RecipeSO recipe)
        {
            return new KitData(NoSequence, NoSequence, ManualLotNumber, string.Empty, recipe.KitCode);
        }
    }
}
