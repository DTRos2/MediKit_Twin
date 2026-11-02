using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇 도구 끝에 달린 카메라가 보여 주는 것. 화면에 그리는 쪽과 물건을 찾는 쪽이 카메라 구현(Unity 카메라)을 모르고 읽는다.
    /// </summary>
    public interface IToolCameraView
    {
        /// <summary>카메라가 달렸으면 true. 화면을 그리지 않는 설정이어도 자세와 시야를 쓸 수 있으면 true이다.</summary>
        bool IsReady { get; }

        /// <summary>카메라가 지금 찍은 화면. 화면을 그리지 않는 설정이면 null이다.</summary>
        Texture Image { get; }

        /// <summary>카메라의 월드 자세. +Z가 보는 방향이다.</summary>
        Pose CameraPose { get; }

        /// <summary>카메라의 시야 모델(시야각, 비율, 최소·최대 거리).</summary>
        PinholeCamera Model { get; }

        /// <summary>가장 최근에 물건을 찾아본 결과. 화면에 표시를 겹쳐 그리는 데 쓴다.</summary>
        ItemObservation LastObservation { get; }

        /// <summary>물건을 찾아본 횟수. 찾아볼 때마다 늘어나서, 같은 결과가 다시 나와도 새로 찾아봤는지 알 수 있다.</summary>
        int ObservationCount { get; }
    }
}
