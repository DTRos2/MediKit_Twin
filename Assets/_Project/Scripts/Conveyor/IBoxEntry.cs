using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 키트 박스를 컨베이어에 올리는 입구. BoxHandoff는 컨베이어 에셋을 모르고 이 접점으로만 박스를 올리므로,
    /// 컨베이어 없이도 보내는 순서와 막혔을 때의 동작을 시험할 수 있다.
    /// </summary>
    public interface IBoxEntry
    {
        /// <summary>지금 박스를 올릴 수 있으면 true. 앞 박스가 입구를 차지하고 있으면 false이다.</summary>
        bool CanEnter { get; }

        /// <summary>박스를 컨베이어에 올린다. 올리지 못하면 박스를 건드리지 않고 false를 돌려준다.</summary>
        bool TryEnter(Transform box, KitData kit);
    }
}
