using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 카메라로 물건을 알아보고 집는 자리를 고치는 방식의 설정.
    /// 이 값들은 시뮬레이션 추정기의 성질(센서 잡음)과 보정 한계다. 실제 영상 인식기로 바꿔도 보정 한계는 그대로 쓴다.
    /// </summary>
    [Serializable]
    public sealed class RecognitionSettings
    {
        [Tooltip("끄면 카메라가 있어도 집기 전에 물건을 찾아보지 않는다.")]
        [SerializeField] private bool _enabled = true;
        [Tooltip("켜면 물건을 카메라로 찾지 못했을 때 집지 않고 실패로 끝낸다. 끄면 경고만 남기고 계획한 자리 그대로 집는다.")]
        [SerializeField] private bool _requireRecognition;
        [Tooltip("센서 잡음의 표준편차(m). 카메라로 추정한 물건 위치에 섞는다. 0이면 잡음이 없다.")]
        [SerializeField, Min(0f)] private float _noiseSigmaM = 0.002f;
        [Tooltip("잡음을 만드는 난수 씨앗. 같은 값이면 같은 잡음이 나온다.")]
        [SerializeField] private int _noiseSeed = 7;
        [Tooltip("쥐는 자리가 본 물건 위치와 이보다 적게 다르면 보정하지 않는다(m).")]
        [SerializeField, Min(0f)] private float _minCorrectionM = 0.004f;
        [Tooltip("한 번에 고치는 최대 거리(m). 이보다 크게 다르면 이 거리까지만 고친다. 잘못 인식해서 팔이 크게 움직이는 일을 막는다. 알려 준 자세가 물건에서 이보다 멀면 그 자세는 쓰지 않고 스스로 찾아 집는다. 그리퍼가 쥘 수 있는 반경(6cm)보다는 커야 알려 준 자세의 작은 어긋남을 고친다.")]
        [SerializeField, Min(0.005f)] private float _maxShiftM = 0.08f;
        [Tooltip("그리퍼 손가락 끝이 도구 끝점보다 아래로 내려오는 깊이(m). 얇은 물건은 중심이 선반 판에 가까워, 중심에 도구 끝점을 맞추면 손가락 끝이 판에 파고들어 팔이 멈춘다. " +
                 "그래서 물건 중심보다 (이 값 - 물건 높이의 절반)만큼 높게 겨냥한다. 0이면 물건 중심에 맞춘다. 팔이 판에 눌려 멈추면 키우고, 물건을 못 쥐면 줄인다.")]
        [SerializeField, Range(0f, 0.06f)] private float _fingerDropBelowToolM = 0.025f;
        [Tooltip("쥘 때 도구 끝점은 그대로 두고 손가락 끝을 아래로 숙이는 각도(도). 손가락 뿌리의 받침 블록이 패드보다 뒤에서 아래로 내려와 있어서, " +
                 "끝을 숙이면 뿌리는 올라가고 패드 끝이 판 가까이 내려가 얇은 물건도 패드 사이에 들어온다. 0이면 알려 준 방향 그대로 쥔다. " +
                 "숙이면 뿌리가 올라가므로 위 '손가락 끝이 내려오는 깊이'도 그만큼 줄여 잡는다.")]
        [SerializeField, Range(0f, 30f)] private float _graspTiltDeg;

        [Header("쥐는 자리 마무리")]
        [Tooltip("쥐는 자리에 닿은 뒤 도구 끝점이 겨냥한 자리와 어긋난 만큼 한 번 더 움직여 메우는 최대 횟수. 관절 각도가 허용 오차 안에 들면 \"닿았다\"고 끝나는데, " +
                 "팔이 자체 무게로 처져 도구 끝점이 몇 cm 덜 들어가 있을 수 있어서다. 0이면 메우지 않는다.")]
        [SerializeField, Range(0, 3)] private int _settleAttempts = 2;
        [Tooltip("닿은 뒤 팔이 처짐까지 가라앉기를 기다리는 시간(초). 오차는 이 시간이 지난 뒤에 잰다.")]
        [SerializeField, Min(0f)] private float _settleWaitSeconds = 0.3f;
        [Tooltip("도구 끝점이 겨냥한 자리와 이보다 적게 다르면 더 움직이지 않는다(m). 센서 잡음(위 값)보다 커야 공연히 움직이지 않는다.")]
        [SerializeField, Min(0.001f)] private float _settleToleranceM = 0.005f;
        [Tooltip("마무리로 한 번에 메우는 최대 거리(m). 잘못 재서 팔이 크게 움직이는 일을 막는다.")]
        [SerializeField, Min(0.005f)] private float _maxSettleM = 0.04f;

        public bool Enabled => _enabled;

        public bool RequireRecognition => _requireRecognition;

        public float NoiseSigmaM => _noiseSigmaM;

        public int NoiseSeed => _noiseSeed;

        public float MinCorrectionM => _minCorrectionM;

        public float MaxCorrectionM => Mathf.Max(_minCorrectionM + 0.001f, _maxShiftM);

        public float FingerDropBelowToolM => _fingerDropBelowToolM;

        public float GraspTiltDeg => _graspTiltDeg;

        public int SettleAttempts => _settleAttempts;

        public float SettleWaitSeconds => _settleWaitSeconds;

        public float SettleToleranceM => _settleToleranceM;

        public float MaxSettleM => Mathf.Max(_settleToleranceM + 0.001f, _maxSettleM);
    }
}
