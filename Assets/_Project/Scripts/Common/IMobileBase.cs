using System;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 굴러가는 베이스(바퀴나 궤도 위에 올라간 로봇암)의 이동 명령. 하역 로봇, 로봇암 A, 강화학습 환경이
    /// 구체 클래스를 모르고 같은 방식으로 이동을 지시한다. 어떻게 굴러가는지(궤도 애니메이션 등)는 구현체가 정한다.
    /// </summary>
    public interface IMobileBase
    {
        bool IsMoving { get; }

        /// <summary>월드 기준 목표 위치(바닥 평면)로 이동한다. 높이(y)는 무시한다.</summary>
        void DriveTo(Vector3 worldPosition);

        /// <summary>그 자리에서 멈춘다.</summary>
        void Stop();

        /// <summary>목표에 도착해서 멈췄을 때 발생한다. Stop으로 멈춘 경우에는 발생하지 않는다.</summary>
        event Action Arrived;
    }
}
