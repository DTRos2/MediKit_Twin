using System;
using System.Collections.Generic;
using MediKitTwin.Common;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 키팅이 끝난 키트의 박스를 지그에서 떼어 컨베이어에 올리는 순서를 맡는다.
    /// 입구가 차 있거나 지그에 박스가 아직 없으면 키트를 줄에 세워 두었다가 Pump로 다시 시도하므로, 키트가 연달아 끝나도 순서대로 보낸다.
    /// 박스는 입구가 받을 수 있다고 확인한 뒤에만 지그에서 떼므로, 올리지 못한 박스가 지그 밖에서 갈 곳을 잃는 일이 없다.
    /// 컨베이어와 박스 공급부는 인터페이스로만 다루고 경고는 호출한 쪽이 정한 곳으로 보내므로 Unity 씬 없이 시험할 수 있다.
    /// </summary>
    public sealed class BoxHandoff
    {
        private readonly IBoxSupply _supply;
        private readonly IBoxEntry _entry;
        private readonly Action<string, string> _warn;
        private readonly Queue<KitData> _pending = new Queue<KitData>();
        private bool _blockedWarned;

        /// <param name="warn">경고를 알리는 곳(경고 코드, 메시지). 보통 CellEvents.RaiseWarning이다.</param>
        public BoxHandoff(IBoxSupply supply, IBoxEntry entry, Action<string, string> warn)
        {
            _supply = supply ?? throw new ArgumentNullException(nameof(supply));
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _warn = warn ?? throw new ArgumentNullException(nameof(warn));
        }

        /// <summary>아직 컨베이어에 올리지 못하고 기다리는 키트 수.</summary>
        public int PendingCount => _pending.Count;

        public bool HasPending => _pending.Count > 0;

        /// <summary>키트를 다 만들었다. 줄에 세우고 바로 보내 본다.</summary>
        public void Enqueue(KitData kit)
        {
            _pending.Enqueue(kit);
            Pump();
        }

        /// <summary>기다리는 키트를 앞에서부터 보낸다. 입구가 막히거나 지그에 박스가 없으면 거기서 멈추고 다음 호출에 다시 시도한다.</summary>
        public void Pump()
        {
            while (_pending.Count > 0)
            {
                if (!_entry.CanEnter)
                {
                    WarnBlockedOnce();
                    return;
                }

                _blockedWarned = false;

                var box = _supply.TakeBox();
                if (box == null)
                {
                    // 새 박스가 내려오는 중이거나 아직 놓이지 않았다. 박스가 생기면 다음 호출에서 보낸다.
                    return;
                }

                KitData kit = _pending.Dequeue();
                if (!_entry.TryEnter(box, kit))
                {
                    // 입구가 받는다고 확인했는데도 올리지 못한 경우다. 박스는 이미 지그를 떠났으므로 알려서 사람이 보게 한다.
                    _warn(ConveyorWarningCodes.EntryFailed, $"키트 {kit?.KitSequence}의 박스를 컨베이어에 올리지 못했습니다.");
                }
            }
        }

        // 막힌 동안 매 프레임 같은 경고가 쏟아지지 않도록, 막히기 시작할 때 한 번만 알린다.
        private void WarnBlockedOnce()
        {
            if (_blockedWarned)
            {
                return;
            }

            _blockedWarned = true;
            _warn(ConveyorWarningCodes.EntryBlocked, $"컨베이어 입구가 차 있어 키트 {_pending.Peek()?.KitSequence}의 박스를 보내지 못하고 기다립니다.");
        }
    }
}
