using System;
using System.Collections.Generic;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 집는 자세를 저장소(IPickPoseStorage)에 두고 찾고 바꾼다. 읽을 때마다 저장소에서 다시 읽으므로
    /// 자세를 알려 주는 쪽(패널)이 저장한 값을 집으러 가는 쪽(ArmItemTransfer)이 같은 파일을 통해 바로 본다(둘이 같은 객체를 공유할 필요가 없다).
    /// 저장소의 글자 형식이 틀리면 덮어써서 기존 자세를 잃지 않도록 예외를 그대로 낸다.
    /// </summary>
    public sealed class TaughtPickPoseRepository : IPickPoseLibrary
    {
        private readonly IPickPoseStorage _storage;

        public TaughtPickPoseRepository(IPickPoseStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public bool TryGet(string slotId, out TaughtPickPose pose)
        {
            return Load().TryGet(slotId, out pose);
        }

        public IReadOnlyList<string> SlotIds()
        {
            return Load().SlotIds;
        }

        public void Save(TaughtPickPose pose)
        {
            PickPoseBook book = Load();
            book.Set(pose);
            _storage.Write(book.ToJson());
        }

        public bool Remove(string slotId)
        {
            PickPoseBook book = Load();
            if (!book.Remove(slotId))
            {
                return false;
            }

            _storage.Write(book.ToJson());
            return true;
        }

        private PickPoseBook Load()
        {
            return PickPoseBook.FromJson(_storage.Read());
        }
    }
}
