using System.Collections.Generic;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 사람이 선반 칸을 골라 로봇에게 키트를 직접 만들게 하는 접점. 화면(패널)은 이 인터페이스만 알고 로봇 동작은 모른다.
    /// 수동 모드를 켜면 자동 키팅이 키트 사이에서 멈추고, 그동안 선반 칸을 하나씩 지시한다.
    /// </summary>
    public interface IManualKitting
    {
        bool IsManualMode { get; }

        /// <summary>수동 모드이고 자동 키팅이 멈춰 있어서 지금 칸을 지시할 수 있으면 true.</summary>
        bool CanRequest { get; }

        /// <summary>로봇이 한 칸을 집어 놓는 중이면 true.</summary>
        bool IsBusy { get; }

        /// <summary>마지막 지시의 결과나 지금 상태를 알리는 문장.</summary>
        string Message { get; }

        IReadOnlyList<string> KitCodes { get; }

        /// <summary>지금 고른 키트 종류 코드. 없으면 빈 문자열이다.</summary>
        string SelectedKitCode { get; set; }

        void SetManualMode(bool isOn);

        /// <summary>선반 칸의 구성품을 집어 박스에 놓도록 지시한다. 지시를 받아들였으면 true, 거절하면 false(이유는 Message).</summary>
        bool TryRequestPick(string shelfSlotId);

        /// <summary>지그의 박스를 치우고 새 박스로 시작한다.</summary>
        void StartNewBox();
    }
}
