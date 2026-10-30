using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Inventory;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    /// <summary>박스 공급과 슬롯 위치를 가짜로 대신한다. PlaceBox를 부르면 박스가 생기고 RemoveBox를 부르면 사라진다.</summary>
    internal sealed class FakeBox : IBoxSupply, IKitBoxLocator
    {
        internal bool NeverAppears;

        internal bool HasBox { get; private set; }

        internal int PlaceCount { get; private set; }

        internal int RemoveCount { get; private set; }

        public void PlaceBox(KitData kit, BoxSize size)
        {
            PlaceCount++;
            HasBox = !NeverAppears;
        }

        public void RemoveBox()
        {
            RemoveCount++;
            HasBox = false;
        }

        public bool TryGetSlotTarget(Vector3 slotLocalPosition, out BoxSlotTarget target)
        {
            target = HasBox ? new BoxSlotTarget(default, null) : default;
            return HasBox;
        }
    }

    /// <summary>선반 화면을 가짜로 대신한다. 비워 둔 칸은 집을 물건이 없다고 답한다.</summary>
    internal sealed class FakeShelf : IShelfItemLocator
    {
        internal readonly HashSet<string> EmptySlots = new HashSet<string>();

        public bool TryGetPickItem(string shelfSlotId, out ShelfPickItem item)
        {
            item = default;
            return !EmptySlots.Contains(shelfSlotId);
        }
    }

    /// <summary>로봇 이송을 가짜로 대신한다. 기록만 하고 즉시 끝나며, 실패 지점을 정해 둘 수 있다.</summary>
    internal sealed class FakeTransfer : IItemTransfer
    {
        internal readonly List<string> Events = new List<string>();

        /// <summary>몇 번째(1부터) 집기에서 이동 실패를 낼지. 0이면 내지 않는다.</summary>
        internal int FailPickNumber;

        /// <summary>몇 번째(1부터) 놓기에서 이동 실패를 낼지. 0이면 내지 않는다.</summary>
        internal int FailPlaceNumber;

        /// <summary>쥐는 순간(TryCommit 직전)에 실행할 동작. 그 사이에 재고가 비는 경쟁 상황을 만들 때 쓴다.</summary>
        internal Action BeforeCommit;

        internal int PickCount { get; private set; }

        internal int PlaceCount { get; private set; }

        internal int DiscardCount { get; private set; }

        public IEnumerator Pick(ItemPickRequest request, TransferResult result)
        {
            PickCount++;
            if (PickCount == FailPickNumber)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            yield return null;
            BeforeCommit?.Invoke();
            if (!request.TryCommit())
            {
                result.Fail(TransferFailure.CommitRejected);
                yield break;
            }

            Events.Add("pick");
            result.Succeed();
        }

        public IEnumerator Place(ItemPlaceRequest request, TransferResult result)
        {
            PlaceCount++;
            if (PlaceCount == FailPlaceNumber)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            yield return null;
            Events.Add("place");
            result.Succeed();
        }

        public void Discard()
        {
            DiscardCount++;
        }
    }

    /// <summary>재고가 알린 신호를 모아 둔다.</summary>
    internal sealed class RecordingStockSink : IStockEventSink
    {
        internal List<(string slot, int order)> Empty { get; } = new List<(string, int)>();

        public void StockEmpty(string shelfSlotId, int orderSequence) => Empty.Add((shelfSlotId, orderSequence));

        public void StockRefilled(string shelfSlotId)
        {
        }
    }

    /// <summary>중첩된 코루틴(yield return IEnumerator)까지 한 단계씩 끝까지 돌린다. 단계마다 시각이 1 흐른다.</summary>
    internal static class CoroutineRunner
    {
        internal static void Drain(IEnumerator routine, Action onStep, int maxSteps = 10000)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);

            int steps = 0;
            while (stack.Count > 0)
            {
                if (++steps > maxSteps)
                {
                    throw new InvalidOperationException("코루틴이 끝나지 않는다.");
                }

                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator inner)
                {
                    stack.Push(inner);
                }

                onStep?.Invoke();
            }
        }
    }
}
