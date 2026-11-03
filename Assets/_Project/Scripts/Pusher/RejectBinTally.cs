using System;

namespace MediKitTwin.Pusher
{
    /// <summary>
    /// 불량 박스의 적재 수와 누적 불량 수 계산. 불량 키트가 들어올 때마다 적재 수를 올리고, 정원에 차면 비워야 한다고 알려 준다.
    /// 누적 불량 수는 비워도 유지된다. (FR-09) 장면과 무관한 계산이라 따로 두어 테스트한다.
    /// </summary>
    public sealed class RejectBinTally
    {
        public const int DefaultCapacity = 5;

        public RejectBinTally(int capacity = DefaultCapacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "정원은 1 이상이어야 한다.");
            }

            Capacity = capacity;
        }

        /// <summary>정원. 적재 수가 이 값에 도달하면 불량 박스를 비운다.</summary>
        public int Capacity { get; }

        /// <summary>지금 불량 박스에 들어 있는 키트 수. 비우면 0이 된다.</summary>
        public int Loaded { get; private set; }

        /// <summary>지금까지 불량으로 처리한 키트 수. 비워도 유지되며, 불량 박스 밖으로 떨어진 키트도 포함한다.</summary>
        public int Total { get; private set; }

        /// <summary>정원이 차서 비운 횟수.</summary>
        public int ClearCount { get; private set; }

        /// <summary>키트가 불량 박스에 들어왔다. 정원에 도달해 비워야 하면 true를 돌려주고 적재 수를 0으로 되돌린다.</summary>
        public bool AddLoaded()
        {
            Loaded++;
            Total++;
            if (Loaded < Capacity)
            {
                return false;
            }

            Loaded = 0;
            ClearCount++;
            return true;
        }

        /// <summary>키트가 불량 박스 밖으로 떨어졌다. 적재 수는 올리지 않고 누적 불량 수에만 포함한다.</summary>
        public void AddOutside()
        {
            Total++;
        }
    }
}
