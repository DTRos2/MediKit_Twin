using System;
using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 에셋의 첫 구간을 박스의 입구로 쓴다. 박스를 받침에 얹어 그 구간에 올리면, 이후 구간 사이의 이동과 코너 회전은 에셋이 맡는다.
    /// </summary>
    public sealed class ConveyorBoxEntry : IBoxEntry
    {
        private readonly ConveyorBase _entry;
        private readonly KitBoxCarrierFactory _factory;

        public ConveyorBoxEntry(ConveyorBase entry, KitBoxCarrierFactory factory)
        {
            _entry = entry != null ? entry : throw new ArgumentNullException(nameof(entry));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool CanEnter => _entry.CanAcceptParcel();

        public bool TryEnter(Transform box, KitData kit)
        {
            // 받침을 만들면 박스가 지그에서 떨어지므로, 받을 수 있는지를 먼저 확인해 박스를 건드리지 않고 돌려보낼 수 있게 한다.
            if (!_entry.CanAcceptParcel())
            {
                return false;
            }

            // 구간의 출구 방향 회전을 받침의 회전으로 삼는다. 코너는 물건을 출구의 회전에 맞춰 돌리므로,
            // 처음부터 같은 규약으로 놓아야 코너를 지날 때 박스가 진행 방향에 맞춰 90도씩만 돈다.
            Quaternion rotation = _entry.OutputPoints != null && _entry.OutputPoints.Length > 0 && _entry.OutputPoints[0] != null
                ? _entry.OutputPoints[0].rotation
                : _entry.transform.rotation;

            KitBoxCarrier carrier = _factory.Create(box, kit, rotation);
            carrier.transform.SetParent(_entry.transform, true);
            return _entry.AcceptParcel(carrier.Parcel);
        }
    }
}
