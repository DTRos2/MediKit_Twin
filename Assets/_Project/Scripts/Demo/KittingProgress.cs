using System.Collections.Generic;
using MediKitTwin.ArmA;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키트 하나에서 몇 칸을 담았고 몇 칸을 담아야 하는지. 누락으로 일부러 집지 않는 칸은 담아야 할 칸에 세지 않는다.
    /// 상태 목록이 항목보다 짧으면(막 시작했을 때) 모자란 칸은 아직 담지 않은 것으로 본다. Unity 없이 시험할 수 있다.
    /// </summary>
    public readonly struct KittingProgress
    {
        private KittingProgress(int placed, int toPlace)
        {
            Placed = placed;
            ToPlace = toPlace;
        }

        public int Placed { get; }

        public int ToPlace { get; }

        public static KittingProgress From(IReadOnlyList<KittingItem> items, IReadOnlyList<KittingItemState> states)
        {
            int placed = 0;
            int toPlace = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsSkipped)
                {
                    continue;
                }

                toPlace++;
                if (i < states.Count && states[i] == KittingItemState.Placed)
                {
                    placed++;
                }
            }

            return new KittingProgress(placed, toPlace);
        }
    }
}
