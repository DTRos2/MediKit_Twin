using System.Collections.Generic;

namespace MediKitTwin.ArmA
{
    /// <summary>알려 준 집는 자세를 저장하고 지우고 나열하는 쪽. 자세를 알려 주는 화면(패널)이 쓴다.</summary>
    public interface IPickPoseRecorder
    {
        /// <summary>저장된 칸 ID들(이름순).</summary>
        IReadOnlyList<string> SlotIds();

        /// <summary>그 칸의 자세를 저장한다. 이미 있으면 바꾼다.</summary>
        void Save(TaughtPickPose pose);

        /// <summary>그 칸의 자세를 지운다. 없었으면 false.</summary>
        bool Remove(string slotId);
    }
}
