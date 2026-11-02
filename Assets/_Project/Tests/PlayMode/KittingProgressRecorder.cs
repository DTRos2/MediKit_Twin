using System.Collections.Generic;
using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 키팅 현황(IKittingStatus)을 프레임마다 읽어 구성품의 단계가 바뀔 때마다 시각과 함께 적어 둔다.
    /// 끝난 뒤 "어느 구성품이 어디까지 갔는지"를 한눈에 보려는 것이다. 현황을 읽기만 하고 키팅에는 손대지 않는다.
    /// </summary>
    public sealed class KittingProgressRecorder
    {
        private readonly IKittingStation _station;
        private readonly StringBuilder _timeline = new StringBuilder();
        private readonly List<KittingItemState> _last = new List<KittingItemState>();
        private string _lastKit = string.Empty;

        public KittingProgressRecorder(IKittingStation station)
        {
            _station = station;
        }

        /// <summary>단계가 바뀔 때마다 (시각, 그 줄의 글)로 알린다. 그 순간의 다른 상태를 같이 적고 싶은 쪽이 듣는다.</summary>
        public event System.Action<float, string> StageChanged;

        /// <summary>단계가 바뀐 순서대로 한 줄씩 적은 글.</summary>
        public string Timeline => _timeline.ToString();

        /// <summary>지금까지 어떤 구성품이든 쥐고 있었던 적이 있으면(옮기는 중이 되었으면) true.</summary>
        public bool WasEverCarrying { get; private set; }

        /// <summary>지금까지 구성품이 박스에 놓였으면(완료가 되었으면) true.</summary>
        public bool WasEverPlaced { get; private set; }

        /// <summary>지금까지 본 키트 계획의 구성품 수 중 가장 큰 값. 끝난 뒤 박스에 몇 개가 있어야 하는지 가늠하는 데 쓴다.</summary>
        public int LargestItemCount { get; private set; }

        /// <summary>지금까지 새로 시작한 키트 수.</summary>
        public int KitsStarted { get; private set; }

        public void Sample(float simSeconds)
        {
            IKittingStatus status = _station.Status;
            if (status.CurrentPlan == null)
            {
                return;
            }

            LargestItemCount = System.Math.Max(LargestItemCount, status.CurrentPlan.Items.Count);
            string kit = status.CurrentPlan.Kit.KitCode + " #" + status.CurrentPlan.Kit.KitSequence;
            if (kit != _lastKit)
            {
                _lastKit = kit;
                _last.Clear();
                KitsStarted++;
                _timeline.AppendLine($"[{simSeconds:0.0}s] 키트 시작: {kit}");
                StageChanged?.Invoke(simSeconds, $"키트 시작 {kit}");
            }

            for (int i = 0; i < status.ItemStates.Count; i++)
            {
                KittingItemState state = status.ItemStates[i];
                if (i < _last.Count && _last[i] == state)
                {
                    continue;
                }

                if (i >= _last.Count)
                {
                    _last.Add(state);
                }
                else
                {
                    _last[i] = state;
                }

                KittingItem item = status.CurrentPlan.Items[i];
                _timeline.AppendLine($"[{simSeconds:0.0}s] 슬롯 {item.SlotNumber} {item.ComponentId} ({item.ShelfSlotId}): {state}");
                StageChanged?.Invoke(simSeconds, $"슬롯 {item.SlotNumber} {item.ComponentId} {state}");
                WasEverCarrying |= state == KittingItemState.Placing;
                WasEverPlaced |= state == KittingItemState.Placed;
            }
        }
    }
}
