using System;

namespace MediKitTwin.Common
{
    /// <summary>그리퍼. 로봇 종류와 무관하게 열고 닫고, 물체를 쥐었는지 묻는다.</summary>
    public interface IGripper
    {
        /// <summary>현재 명령 개폐 정도. 0이 닫힘, 1이 열림.</summary>
        float Opening { get; }

        /// <summary>물체를 쥐고 있으면 true.</summary>
        bool IsHolding { get; }

        event Action Grasped;
        event Action Released;

        void SetOpening(float opening);
        void Open();
        void Close();
    }
}
