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

        /// <summary>베이스 루트의 현재 월드 위치.</summary>
        Vector3 Position { get; }

        /// <summary>베이스 루트의 현재 수평 방향(도, 월드 Y축 기준).</summary>
        float YawDeg { get; }

        /// <summary>월드 기준 목표 위치(바닥 평면)로 이동한다. 높이(y)는 무시한다. 도착했을 때의 방향은 마지막 이동 방향이다.</summary>
        void DriveTo(Vector3 worldPosition);

        /// <summary>목표 위치로 이동한 뒤 제자리에서 finalYawDeg(월드 Y축 기준 도)로 돌아선다. 루트에서 떨어진 팔 베이스를 정확히 놓을 때 쓴다.</summary>
        void DriveTo(Vector3 worldPosition, float finalYawDeg);

        /// <summary>그 자리에서 멈춘다.</summary>
        void Stop();

        /// <summary>목표에 도착해서 멈췄을 때 발생한다. Stop으로 멈춘 경우에는 발생하지 않는다.</summary>
        event Action Arrived;
    }
}
