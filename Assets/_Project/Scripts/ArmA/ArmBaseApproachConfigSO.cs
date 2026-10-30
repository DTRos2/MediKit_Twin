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
        [SerializeField, Min(0f)] private float _pickStandoffM = 0.42f;
        [Tooltip("박스 슬롯에서 떨어져 서는 거리(m).")]
        [SerializeField, Min(0f)] private float _placeStandoffM = 0.55f;
        [Tooltip("박스 슬롯에서 베이스가 서는 쪽의 수평 방향(월드). 선반에서 지그로 돌아올 때 지그의 어느 쪽에 설지 정한다.")]
        [SerializeField] private Vector3 _boxStandDirection = new Vector3(0f, 0f, 1f);

        [Header("이동 정밀도")]
        [Tooltip("팔 베이스가 목표에서 이 거리 안이면 도착으로 본다(m).")]
        [SerializeField, Min(0.001f)] private float _arriveToleranceM = 0.03f;
        [Tooltip("회전으로 팔 베이스가 어긋났을 때 다시 보내는 최대 횟수.")]
        [SerializeField, Min(0)] private int _maxCorrections = 4;
        [Tooltip("한 번의 이동이 끝나기를 기다리는 최대 시간(초). 넘으면 이송 실패로 본다.")]
        [SerializeField, Min(1f)] private float _timeoutSeconds = 30f;

        public float PickStandoffM => _pickStandoffM;

        public float PlaceStandoffM => _placeStandoffM;

        public Vector3 BoxStandDirection => _boxStandDirection;

        public float ArriveToleranceM => _arriveToleranceM;

        public int MaxCorrections => _maxCorrections;

        public float TimeoutSeconds => _timeoutSeconds;
    }
}
