using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇암의 운동학 정보. IK가 필요한 것(순기구학 체인, 베이스 좌표계, IK 설정, 현재 상태)만 묶었다.
    /// IkArmTarget은 이 인터페이스만 알아서 ArmController가 아닌 다른 구현으로 바꿔 끼울 수 있다.
    /// </summary>
    public interface IArmModel : IArmState
    {
        /// <summary>준비가 끝나기 전에는 null이다. IsReady를 확인한 뒤 쓴다.</summary>
        KinematicChain Chain { get; }

        /// <summary>체인의 기준(베이스 링크). IK 목표를 월드에서 이 좌표계로 바꿀 때 쓴다.</summary>
        Transform BaseLink { get; }

        DlsIkSettings IkSettings { get; }
    }
}
