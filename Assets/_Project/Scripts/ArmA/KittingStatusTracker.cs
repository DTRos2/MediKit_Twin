using System;
using System.Collections.Generic;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키팅 진행을 모아 현황으로 보여 준다. IKittingObserver로 알림을 받고 IKittingStatus로 읽힌다.
    /// 상태만 갖고 화면은 모르므로 IMGUI 패널이든 UI Toolkit이든 같은 현황을 읽는다.
    /// </summary>
    public sealed class KittingStatusTracker : IKittingObserver, IKittingStatus
    {
        private KittingItemState[] _states = Array.Empty<KittingItemState>();

        public KittingPlan CurrentPlan { get; private set; }

        public IReadOnlyList<KittingItemState> ItemStates => _states;

        public KittingResult LastResult { get; private set; }

        public string LastMessage { get; private set; }

        public string LastLotNumber { get; private set; }

        public int CompletedKitCount { get; private set; }

        public int StockShortageCount { get; private set; }

        public int FaultCount { get; private set; }

        public void PlanStarted(KittingPlan plan)
        {
            CurrentPlan = plan;
            _states = new KittingItemState[plan.Items.Count];
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i] = plan.Items[i].IsSkipped ? KittingItemState.Skipped : KittingItemState.Waiting;
            }
        }

        public void ItemStateChanged(int itemIndex, KittingItemState state)
        {
            if (itemIndex >= 0 && itemIndex < _states.Length)
            {
                _states[itemIndex] = state;
            }
        }

        public void Finished(KittingPlan plan, KittingOutcome outcome)
        {
            LastResult = outcome.Result;
            LastMessage = outcome.Message;
            LastLotNumber = plan.Kit.LotNumber;

            switch (outcome.Result)
            {
                case KittingResult.Completed:
                    CompletedKitCount++;
                    break;
                case KittingResult.StockShortage:
                    StockShortageCount++;
                    break;
                case KittingResult.Fault:
                    FaultCount++;
                    break;
            }

            CurrentPlan = null;
            _states = Array.Empty<KittingItemState>();
        }
    }
}
