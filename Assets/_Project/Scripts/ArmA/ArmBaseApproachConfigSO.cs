using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇암 베이스가 물건 앞에 서는 거리와 이동 정밀도를 담은 설정 에셋. 로봇이나 선반 치수가 바뀌면 코드가 아니라 여기서 고친다.
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Arm Base Approach Config", fileName = "ArmBaseApproachConfig")]
    public sealed class ArmBaseApproachConfigSO : ScriptableObject
    {
        [Header("서는 자리")]
        [Tooltip("선반 물건에서 칸 밖 방향으로 떨어져 서는 거리(m). 팔이 물건에 닿고 베이스가 선반에 부딪히지 않는 사이로 정한다.")]
        [SerializeField, Min(0f)] private float _pickStandoffM = 0.65f;
        [Tooltip("선반 앞 첫 자리에서 팔이 닿지 못하면, 기본 거리에 이 값들을 차례로 더한 거리에 다시 서서 시도한다(m). 첫 값은 보통 0(기본 거리). 비우면 한 번만 시도한다.")]
        [SerializeField] private float[] _pickStandoffOffsetsM = { 0f, 0.1f, 0.2f };
        [Tooltip("박스 슬롯 곁 첫 자리에서 팔이 닿지 못하면, 기본 거리에 이 값들을 차례로 더한 거리에 다시 서서 시도한다(m). 비우면 한 번만 시도한다.")]
        [SerializeField] private float[] _placeStandoffOffsetsM = { 0f, 0.1f, 0.2f };
        [Tooltip("서는 자리로 들어가기 전에 같은 선 위에서 더 물러나 거치는 거리(m). 칸 정면에서 수직으로 들어가게 한다.")]
        [SerializeField, Min(0f)] private float _approachExtraM = 0.3f;
        [Tooltip("박스 슬롯에서 떨어져 서는 거리(m).")]
        [SerializeField, Min(0f)] private float _placeStandoffM = 0.55f;
        [Tooltip("박스 슬롯에서 베이스가 서는 쪽의 수평 방향(월드). 선반에서 지그로 돌아올 때 지그의 어느 쪽에 설지 정한다.")]
        [SerializeField] private Vector3 _boxStandDirection = new Vector3(0f, 0f, 1f);

        [Header("이동 정밀도")]
        [Tooltip("팔 베이스가 목표에서 이 거리 안이면 도착으로 본다(m).")]
        [SerializeField, Min(0.001f)] private float _arriveToleranceM = 0.03f;
        [Tooltip("팔 베이스가 이 거리(m)보다 적게 움직이면 방향을 바꾸지 않고 지금 방향으로 선다. 작은 보정에 제자리 회전을 하지 않게 한다.")]
        [SerializeField, Min(0f)] private float _minHeadingTravelM = 0.05f;
        [Tooltip("도착 뒤 남은 오차(미끄러짐 등)를 다시 보내 고치는 최대 횟수.")]
        [SerializeField, Min(0)] private int _maxCorrections = 4;
        [Tooltip("이동을 시작하기 전과 도착한 뒤에 기다리는 프레임 수. 관절 로봇의 위치가 베이스를 따라와 자리를 잡은 값으로 재기 위해서다.")]
        [SerializeField, Min(0)] private int _settleFrames = 3;
        [Tooltip("한 번의 이동이 끝나기를 기다리는 최대 시간(초). 넘으면 이송 실패로 본다.")]
        [SerializeField, Min(1f)] private float _timeoutSeconds = 30f;

        [Header("이동 중 팔 접기")]
        [Tooltip("모든 관절이 접힌 자세에서 이 각도(rad) 안이면 접힌 것으로 본다. 중력이나 접촉으로 끝까지 못 접혀 \"팔을 접지 못했다\"로 실패하면 키운다.")]
        [SerializeField, Min(0.001f)] private float _stowToleranceRad = 0.1f;
        [Tooltip("팔을 접기를 기다리는 최대 시간(초). 넘으면 이송 실패로 본다.")]
        [SerializeField, Min(1f)] private float _stowTimeoutSeconds = 15f;

        [Header("이동 중 충돌")]
        [Tooltip("로봇암 링크가 속한 레이어 이름. 이동하는 동안 선반 레이어와의 충돌을 끈다. 둘 중 하나라도 비우면 끄지 않는다. " +
                 "기본은 비어 있다(꺼짐): 레이어를 지정한 뒤 플레이 직후 Unity가 물리 단계에서 종료되는 경우가 있어, 확인한 뒤에만 켠다.")]
        [SerializeField] private string _armLayerName = "";
        [Tooltip("선반이 속한 레이어 이름.")]
        [SerializeField] private string _shelfLayerName = "";

        public string ArmLayerName => _armLayerName;

        public string ShelfLayerName => _shelfLayerName;

        public float PickStandoffM => _pickStandoffM;

        public float PlaceStandoffM => _placeStandoffM;

        public float ApproachExtraM => _approachExtraM;

        /// <summary>선반 앞에서 차례로 시도할 거리 보정값. 비었으면 보정 없이(0) 한 번만 시도한다.</summary>
        public IReadOnlyList<float> PickStandoffOffsetsM => OrSingleZero(_pickStandoffOffsetsM);

        /// <summary>박스 곁에서 차례로 시도할 거리 보정값. 비었으면 보정 없이(0) 한 번만 시도한다.</summary>
        public IReadOnlyList<float> PlaceStandoffOffsetsM => OrSingleZero(_placeStandoffOffsetsM);

        private static IReadOnlyList<float> OrSingleZero(float[] offsets)
        {
            return offsets != null && offsets.Length > 0 ? offsets : new[] { 0f };
        }

        public Vector3 BoxStandDirection => _boxStandDirection;

        public float ArriveToleranceM => _arriveToleranceM;

        public float MinHeadingTravelM => _minHeadingTravelM;

        public int SettleFrames => _settleFrames;

        public int MaxCorrections => _maxCorrections;

        public float TimeoutSeconds => _timeoutSeconds;

        public float StowToleranceRad => _stowToleranceRad;

        public float StowTimeoutSeconds => _stowTimeoutSeconds;
    }
}
