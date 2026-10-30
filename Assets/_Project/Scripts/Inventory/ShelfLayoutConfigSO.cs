using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 선반의 크기, 칸 안 물건 배치, 로봇암과의 거리를 담은 설정 에셋. 에디터 빌더와 정렬 도구가 읽으므로
    /// 치수와 배치 규칙을 코드가 아니라 여기서 고친다. 선반은 3행 4열(SRS)이고 칸 ID는 레시피가 정하므로
    /// 행과 열의 개수는 이 에셋에 두지 않는다.
    /// 로봇암은 바퀴 달린 베이스를 타고 물건 앞까지 이동하므로 선반은 제자리에 둔다. 다만 팔은 높이를 바꿀 수 없으므로
    /// 세 줄이 모두 어깨 높이 근처(작업 반경 안)에 오도록 선반 높이를 정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Shelf Layout Config", fileName = "ShelfLayoutConfig")]
    public sealed class ShelfLayoutConfigSO : ScriptableObject
    {
        [Header("칸")]
        [Tooltip("칸 사이의 가로 간격(m). 열 4개가 이 간격으로 놓이므로 선반 폭은 이 값의 4배이다.")]
        [SerializeField, Min(0.1f)] private float _cellWidth = 0.30f;
        [Tooltip("칸 사이의 세로 간격(m). 세 줄이 어깨 높이 근처에 모이도록 작게 둔다.")]
        [SerializeField, Min(0.1f)] private float _cellHeight = 0.25f;
        [Tooltip("맨 아래 칸 중심의 높이(m). 선반 원점(바닥) 기준이며, 가운데 줄이 로봇암 어깨 높이에 오도록 \"Fit Shelf To Arm\" 도구가 맞춰 준다.")]
        [SerializeField, Min(0f)] private float _firstRowHeight = 0.80f;
        [Tooltip("칸 하나의 바깥 크기(m). 높이는 그리퍼 손이 들어갈 수 있게 정한다.")]
        [SerializeField] private Vector3 _cellSize = new Vector3(0.30f, 0.20f, 0.24f);
        [Tooltip("칸 벽 두께(m). 얇게 두어야 같은 칸 크기에서 안쪽이 넓다.")]
        [SerializeField, Min(0.005f)] private float _wallThickness = 0.01f;
        [Tooltip("선반 받침대 크기(m).")]
        [SerializeField] private Vector3 _baseSize = new Vector3(1.3f, 0.04f, 0.30f);

        [Header("칸 안 물건 배치")]
        [Tooltip("한 줄에 놓는 최대 개수. 칸이 좁아 많이 놓으면 겹치므로 넘으면 줄을 바꾼다. 손가락이 물건 양옆으로 들어가려면 물건 사이에 틈이 있어야 한다.")]
        [SerializeField, Min(1)] private int _itemsPerRow = 3;
        [Tooltip("물건이 차지할 수 있는 격자 칸의 비율(0~1). 나머지는 물건 사이의 여유이다.")]
        [SerializeField, Range(0.1f, 1f)] private float _itemFillRatio = 0.9f;

        [Header("그리퍼가 집을 수 있는 크기")]
        [Tooltip("그리퍼가 완전히 열렸을 때 손가락 사이 간격(m). FR5 그리퍼는 손가락 원점이 x=+0.0345, x=-0.0425(fr5_withgripper.urdf의 fj1, fj2)라 0.077m이다. 로봇을 바꾸면 이 값도 바꾼다.")]
        [SerializeField, Min(0.01f)] private float _gripperOpeningWidthM = 0.077f;
        [Tooltip("열린 폭 중 물건이 차지해도 되는 비율(0~1). 나머지는 손가락이 들어갈 여유이다. 집는 방향을 알 수 없어 가장 긴 변을 기준으로 한다.")]
        [SerializeField, Range(0.1f, 1f)] private float _graspClearanceRatio = 0.8f;

        [Header("로봇암 (로봇을 바꾸면 함께 바꾼다)")]
        [Tooltip("로봇암이 선반과 지그에 안정적으로 닿는 반경(m). FR5의 공식 작업 반경은 0.922m이지만 도구를 수평으로 들이밀면 줄어서 여유를 두고 정한다.")]
        [SerializeField, Min(0.1f)] private float _armReachM = 0.75f;
        [Tooltip("로봇암 베이스에서 어깨 관절까지의 높이(m). FR5는 0.155m(URDF의 j2 간격)이다. 도달 거리는 어깨에서 잰다.")]
        [SerializeField, Min(0f)] private float _armShoulderHeightM = 0.155f;

        [Header("선반 위치")]
        [Tooltip("지그 중심에서 선반 원점까지의 월드 오프셋(m). 선반은 이 자리에 둔다(처음 배치와 같은 값).")]
        [SerializeField] private Vector3 _shelfOffsetFromJigM = new Vector3(0f, 0f, 2.6f);
        [Tooltip("선반의 방향(Y축 회전, 도). 칸이 열린 쪽이 지그와 팔 쪽을 향하게 한다.")]
        [SerializeField] private float _shelfYawDegrees = 180f;

        public float CellWidth => _cellWidth;

        public float CellHeight => _cellHeight;

        public float FirstRowHeight => _firstRowHeight;

        public Vector3 CellSize => _cellSize;

        public float WallThickness => _wallThickness;

        public Vector3 BaseSize => _baseSize;

        public int ItemsPerRow => _itemsPerRow;

        public float ItemFillRatio => _itemFillRatio;

        public float GripperOpeningWidthM => _gripperOpeningWidthM;

        public float GraspClearanceRatio => _graspClearanceRatio;

        public float ArmReachM => _armReachM;

        public float ArmShoulderHeightM => _armShoulderHeightM;

        public Vector3 ShelfOffsetFromJigM => _shelfOffsetFromJigM;

        public float ShelfYawDegrees => _shelfYawDegrees;
    }
}
