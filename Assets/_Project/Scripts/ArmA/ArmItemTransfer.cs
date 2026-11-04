using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// IItemTransfer의 실제 로봇암 구현. 씬에 놓이는 MonoBehaviour로서 설정(직렬화 필드)을 받아 협력 객체를 조립하고,
    /// 집기와 놓기를 맞는 순서 객체에 넘기는 일만 한다.
    ///  - 집기: 칸에 사람이 알려 준 자세가 있으면 TaughtPickRoutine, 없으면 SearchedPickRoutine(서는 자리와 도구 방향을 스스로 찾음)
    ///  - 놓기: BoxPlaceRoutine
    /// 순서마다 움직임 한 단계는 ArmMotionSteps, 서서 닿는 방향 찾기는 StandSearchRoutine, 서는 자리 정책은 StandPolicy,
    /// 실패 이유 설명은 TransferDiagnostics, 쥐고 놓는 방식은 IItemHolder, 베이스 이동은 IArmBasePositioner가 맡는다.
    /// 로봇이 준비되면 도구 끝에 카메라를 달고(ToolCameraFeed), 물건 앞에서 카메라로 물건을 찾아 집는 자리를 고치며(ObservationGraspRefiner),
    /// 방향과 서는 자리를 고를 때 팔이 장애물과 겹치지 않는 쪽을 우선한다(ArmClearanceProbe). 이 셋은 ArmPerception을 통해 순서 객체에 끼워진다.
    /// 베이스 이동 모듈을 비우면 팔이 제자리에서 닿는 곳만 집는 고정 팔로 동작한다.
    /// 실패하면 어느 단계에서 왜 실패했는지를 결과(TransferResult.Detail)에 남긴다.
    /// </summary>
    public sealed class ArmItemTransfer : MonoBehaviour, IItemTransfer, IManualHoldState
    {
        // 인스펙터는 cm로 받고 코드는 m로 다루므로 바꾸는 배율. 조정할 값이 아니라 단위 변환이다.
        private const float CentimetersPerMeter = 100f;

        [Tooltip("도구 끝점 목표를 받는 쪽(IToolPoseTarget 구현체, 보통 IkArmTarget).")]
        [SerializeField] private MonoBehaviour _toolTarget;
        [Tooltip("물건을 쥐고 놓는 방식(IItemHolder 구현체: ScriptAttachHolder 또는 PhysicsGraspHolder).")]
        [SerializeField] private MonoBehaviour _holder;

        [Header("베이스 이동 (비우면 고정 팔)")]
        [Tooltip("팔 베이스를 옮기는 쪽(IArmBasePositioner 구현체, 보통 ArmBasePositionerBehaviour). 비우면 이동하지 않는다.")]
        [SerializeField] private MonoBehaviour _positioner;
        [Tooltip("서는 거리와 방향을 담은 설정. 베이스 이동 모듈을 연결했다면 반드시 지정한다.")]
        [SerializeField] private ArmBaseApproachConfigSO _approach;

        [Header("선반에서 집기")]
        [Tooltip("칸마다 사람이 알려 준 자세(Assets/_Project/Config/TaughtPickPoses.json)가 있으면 탐색 없이 그 자세로 집는다. 끄면 항상 스스로 찾는다.")]
        [SerializeField] private bool _useTaughtPoses = true;
        [Tooltip("알려 준 쥐는 자리가 지금 물건과 수평으로 어긋나 있으면(알려 준 뒤 선반이 바뀜), 서는 자리와 도구 경로를 그만큼 옮겨 알려 준 자세 그대로 쥔다. " +
                 "옮길 수 있는 최대 거리(m)이며, 더 멀면 알려 준 자세를 쓰지 않고 스스로 찾는다. 0이면 옮기지 않고 저장값 그대로 간다.")]
        [SerializeField, Min(0f)] private float _taughtShiftLimitM = 0.6f;
        [Tooltip("선반 칸 앞에서 물건 쪽으로 들어가기 시작하는 거리(m).")]
        [SerializeField, Min(0.02f)] private float _shelfApproachDistance = 0.15f;
        [Tooltip("물건을 쥔 뒤 칸 밖으로 빼내는 거리(m).")]
        [SerializeField, Min(0.02f)] private float _shelfRetreatDistance = 0.15f;
        [Tooltip("물건에 닿는 도구 방향을 찾을 때 기본 방향(수평으로 칸 안쪽)에서 벗어날 수 있는 범위. 선반 칸은 위가 막혀 있어 위로는 조금만 든다.")]
        [SerializeField] private OrientationSearchSettings _shelfSearch = new OrientationSearchSettings(45f, 15f, 15f, 30f, 15f, 90f, 90f);
        [Tooltip("알려 준 자세로 가는 길이 선반과 겹칠 때, 알려 준 도구 방향에서 벗어나 볼 수 있는 범위. 이것으로도 안 되면 서는 자리를 바꿔 스스로 찾는다.")]
        [SerializeField] private OrientationSearchSettings _taughtSearch = new OrientationSearchSettings(20f, 10f, 5f, 20f, 5f, 0f, 0f);

        [Header("박스에 놓기")]
        [Tooltip("박스 슬롯 위에서 내려가기 시작하는 높이(m).")]
        [SerializeField, Min(0.02f)] private float _boxApproachHeight = 0.15f;
        [Tooltip("물건을 놓은 뒤 들어올리는 높이(m).")]
        [SerializeField, Min(0.02f)] private float _boxLiftHeight = 0.15f;
        [Tooltip("박스 위에서 손가락이 벌어지는 방향을 정하는 기준. 도구가 아래를 볼 때 위쪽이 되는 수평 방향이다.")]
        [SerializeField] private Vector3 _boxGripYawForward = Vector3.forward;
        [Tooltip("박스 슬롯에 닿는 도구 방향을 찾을 때 기본 방향(똑바로 아래)에서 벗어날 수 있는 범위. 비틀기는 위에서 보아 손을 돌리는 것이다.")]
        [SerializeField] private OrientationSearchSettings _boxSearch = new OrientationSearchSettings(30f, 30f, 15f, 30f, 15f, 180f, 90f);
        [Tooltip("물건을 쥔 방향 그대로 위에서 내려놓으면 선반에서 눕혀 있던 물건이 모서리로 선다. 켜면 물건이 선반에 놓여 있던 방향 그대로 놓이는 도구 방향을 먼저 찾고, " +
                 "닿지 못하면 위에서 내려놓는다. 쥔 자세를 알 수 있는 방식(PhysicsGraspHolder)에서만 쓴다.")]
        [SerializeField] private bool _placeInRestingOrientation = true;
        [Tooltip("물건이 놓여 있던 방향으로 놓이는 도구 방향에서 벗어날 수 있는 범위. 손목 비틀기는 물건을 가로로 눕히므로 180°씩만 허용(뒤집어 놓는 것은 괜찮다)한다. " +
                 "도구를 아래로 기울여도(약 60°까지) 눕혀 있던 물건은 떨어지면서 다시 눕는다. 서는 자리는 기본 자리 한 곳만 본다.")]
        [SerializeField] private OrientationSearchSettings _boxRestingSearch = new OrientationSearchSettings(60f, 30f, 30f, 60f, 30f, 180f, 180f);
        [Tooltip("슬롯 중심보다 이만큼 위에서 물건을 놓아 떨어뜨리는 높이 후보들(m). 낮은 것부터 시도해 팔이 닿는 가장 낮은 높이에서 놓는다. " +
                 "좁은 박스에서는 벌린 손가락이 벽과 이웃 물건 사이로 들어가지 못해, 가장 높은 값은 손가락이 벽 윗면보다 위에 오게 해야 한다: " +
                 "(벽 윗면 − 슬롯 중심) + 손가락이 도구 끝점 아래로 내려오는 길이(약 2.5cm) + 여유. 0은 슬롯 중심까지 내려가 놓는다. 후보가 많을수록 안 닿을 때 시간이 더 든다.")]
        [SerializeField] private float[] _boxReleaseHeights = { 0f, 0.04f, 0.07f, 0.11f };
        [Tooltip("슬롯마다 놓기에 성공한 방식(놓는 높이, 서는 자리, 도구 방향)을 기억해, 같은 슬롯에 같은 구성품을 놓을 때 처음부터 찾지 않고 그 방식을 먼저 쓴다. " +
                 "닿지 않거나 내려가지 못하면 잊고 처음부터 다시 찾는다. 끄면 매번 처음부터 찾는다.")]
        [SerializeField] private bool _rememberPlacements = true;
        [Tooltip("이 거리(m) 안이면 같은 슬롯으로 본다. 박스가 지그에 멈추는 자리가 조금씩 달라도 같은 슬롯을 알아보게 하되, 이웃 슬롯 사이 간격보다 작아야 한다.")]
        [SerializeField, Min(0.001f)] private float _placementMemoryRadiusM = 0.02f;

        [Header("카메라와 물건 인식")]
        [Tooltip("도구 끝에 다는 카메라. 화면은 'Tool Camera' 패널에 나온다.")]
        [SerializeField] private ToolCameraSettings _camera = new ToolCameraSettings();
        [Tooltip("물건 앞에서 카메라로 물건을 찾아보고 집는 자리를 고치는 방식.")]
        [SerializeField] private RecognitionSettings _recognition = new RecognitionSettings();

        [Header("장애물 회피")]
        [Tooltip("팔이 선반·박스에 닿지 않는 도구 방향과 서는 자리를 고르는 방식.")]
        [SerializeField] private CollisionAvoidanceSettings _collision = new CollisionAvoidanceSettings();

        [Header("방향·서는 자리 찾기")]
        [Tooltip("도구 방향과 서는 자리를 찾을 때 한 프레임에 쓰는 시간(밀리초). 크면 찾기가 빨리 끝나지만 그동안 화면이 덜 부드럽다. 0이면 프레임마다 후보 하나만 따진다.")]
        [SerializeField, Range(0f, 30f)] private float _searchBudgetMs = 8f;

        [Header("판정 — 쥐고 놓는 동작 (엄격)")]
        [Tooltip("물건을 쥐는 자리와 박스 슬롯에 놓는 자리에서 허용하는 위치 오차(m).")]
        [SerializeField, Min(0.001f)] private float _positionToleranceM = 0.01f;
        [Tooltip("물건을 쥐는 자리와 박스 슬롯에 놓는 자리에서 허용하는 회전 오차(도).")]
        [SerializeField, Min(0.5f)] private float _rotationToleranceDeg = 5f;

        [Header("판정 — 접근·후퇴·들어올리기 (느슨)")]
        [Tooltip("물건 앞, 박스 위처럼 지나가는 자리에서 허용하는 위치 오차(m). 시간 초과로 실패하면 키운다.")]
        [SerializeField, Min(0.001f)] private float _travelPositionToleranceM = 0.03f;
        [Tooltip("지나가는 자리에서 허용하는 회전 오차(도). 시간 초과로 실패하면 키운다.")]
        [SerializeField, Min(0.5f)] private float _travelRotationToleranceDeg = 10f;

        [Tooltip("지나가는 자리에서 IK가 수렴하지 못해도 이 위치 오차(m) 안이면 풀린 것으로 보고 움직인다. " +
                 "IK 수렴 기준(2mm)보다 느슨하게 닿아도 되는 자리를 아슬아슬하게 못 풀어 멈추는 일을 막는다. " +
                 "닿았다고 보는 오차(위 값)의 절반쯤으로 두어, 팔이 목표를 따라가며 생기는 오차가 더해져도 닿았다고 판정되게 한다. 0이면 엄격하게(수렴해야만) 푼다.")]
        [SerializeField, Min(0f)] private float _travelSolvePositionToleranceM = 0.015f;
        [Tooltip("지나가는 자리에서 IK가 수렴하지 못해도 이 회전 오차(도) 안이면 풀린 것으로 본다. 0이면 엄격하게 푼다.")]
        [SerializeField, Min(0f)] private float _travelSolveRotationToleranceDeg = 5f;

        [Header("판정 — 알려 준 관절 각도로 갈 때")]
        [Tooltip("알려 준 관절 각도로 쥐는 자리에서 허용하는 관절 각도 오차(도).")]
        [SerializeField, Min(0.1f)] private float _jointToleranceDeg = 1.5f;
        [Tooltip("알려 준 관절 각도로 지나가는 자리(접근, 후퇴)에서 허용하는 관절 각도 오차(도). 시간 초과로 실패하면 키운다.")]
        [SerializeField, Min(0.1f)] private float _travelJointToleranceDeg = 4f;

        [Header("판정 — 시간")]
        [Tooltip("한 동작이 목표에 닿기를 기다리는 최대 시간(초).")]
        [SerializeField, Min(1f)] private float _stepTimeoutSeconds = 10f;

        [Header("진단")]
        [Tooltip("켜면 박스에 놓는 동안 쥔 물건이 도구에서 떨어진 거리, 가늠한 도구 자리와 실제로 가는 도구 자리의 차이(IK 결과 포함), " +
                 "놓기 직전 물건의 미끄러짐과 어긋남을 로그로 남긴다. 슬롯마다 두세 줄이 늘어나 평소 화면 로그를 가득 채우므로 기본은 꺼 두고, " +
                 "놓기 문제를 따질 때만 켠다.")]
        [SerializeField] private bool _logPlacementTrace = false;
        [Tooltip("켜면 박스로 가며 팔 베이스를 옮길 때마다 물리 스텝마다 베이스의 속도와 쥔 물건의 밀림을 재서, 이동 한 번마다 요약 한두 줄을 로그로 남긴다. " +
                 "물건이 베이스가 움직이는 중에 밀리는지 멈춘 뒤에 밀리는지 가리는 데 쓴다. 위 박스 진단이 켜져 있어야 한다. 이동이 되풀이될수록 줄이 늘어나므로 기본은 꺼 둔다.")]
        [SerializeField] private bool _logBaseMotionTrace = false;
        [Tooltip("이동 중 쥔 물건이 이만큼(cm) 밀리면 '밀리기 시작했다'고 보고, 그때 베이스가 움직이는 중이었는지와 속도를 로그에 남긴다.")]
        [SerializeField, Min(0.1f)] private float _baseMotionSlipOnsetCm = 2f;
        [Tooltip("이 속도(m/s)를 넘으면 베이스가 움직이는 것으로 본다. 밀림이 움직이는 중에 생겼는지 멈춘 뒤에 생겼는지 가르는 기준이다.")]
        [SerializeField, Min(0.001f)] private float _baseMotionStillSpeedMps = BaseMotionTrace.DefaultStillSpeedMps;
        [Tooltip("이 회전 속도(도/초)를 넘으면 베이스가 도는 것으로 본다.")]
        [SerializeField, Min(0.1f)] private float _baseMotionStillYawRateDegPerSec = BaseMotionTrace.DefaultStillYawRateDegPerSec;
        [Tooltip("이동 한 번의 추이 줄에 적는 스텝의 최대 개수. 넘으면 일정한 간격으로 줄여 적는다.")]
        [SerializeField, Min(10)] private int _baseMotionTraceMaxEntries = BoxPlaceDiagnostics.DefaultMaxTraceEntries;
        [Tooltip("이만큼(도) 이상 돌았을 때만 추이에 회전 속도를 적는다. 돌지 않은 이동은 회전 속도가 전부 0이라 줄만 길어진다.")]
        [SerializeField, Min(0f)] private float _baseMotionTraceMinYawTravelDeg = BoxPlaceDiagnostics.DefaultMinYawTravelToTraceDeg;

        private IItemHolder _itemHolder;
        private TaughtPoseFinder _taughtFinder;
        private TaughtPickRoutine _taughtPick;
        private SearchedPickRoutine _searchedPick;
        private BoxPlaceRoutine _boxPlace;

        // 카메라 인식과 장애물 회피. 로봇이 준비된 뒤에 채워지고(Start), 채워지기 전에는 아무것도 하지 않는다.
        private readonly ArmPerception _perception = new ArmPerception();

        /// <summary>이 팔의 도구 목표를 사람이 직접 조작하느라 막아 두었으면 true. 키팅 반복이 이 값을 보고 쉰다.</summary>
        public bool IsHeld => _toolTarget is IManualHoldState hold && hold.IsHeld;

        private void Awake()
        {
            var tool = _toolTarget as IToolPoseTarget;
            _itemHolder = _holder as IItemHolder;
            if (tool == null || _itemHolder == null)
            {
                Debug.LogError($"{name}: Tool Target(IToolPoseTarget)과 Holder(IItemHolder)를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            var basePositioner = _positioner as IArmBasePositioner;
            ArmBaseStandPlanner planner = null;
            if (basePositioner != null)
            {
                if (_approach == null)
                {
                    Debug.LogError($"{name}: 베이스 이동 모듈을 연결했으면 Approach 설정도 지정해야 한다.", this);
                    enabled = false;
                    return;
                }

                planner = new ArmBaseStandPlanner(
                    _approach.PickStandoffM, _approach.PlaceStandoffM, _approach.ApproachExtraM, _approach.BoxStandDirection);
            }

            AssembleCollaborators(tool, basePositioner, new StandPolicy(planner, _approach));
        }

        private void Start()
        {
            if (_camera.Enabled || _collision.Enabled)
            {
                StartCoroutine(AttachPerceptionWhenReady());
            }
        }

        // 카메라와 충돌 상자는 팔의 순기구학과 링크 변환이 준비된 뒤에야 만들 수 있다.
        private IEnumerator AttachPerceptionWhenReady()
        {
            var model = (_toolTarget as IArmModelSource)?.Model;
            var host = model as IGripperHost;
            if (model == null || host == null)
            {
                Debug.LogWarning($"{name}: 도구 목표에서 팔 모델(IArmModelSource)을 찾지 못해 카메라 인식과 장애물 회피를 쓰지 않는다.", this);
                yield break;
            }

            while (!model.IsReady)
            {
                yield return null;
            }

            ArmPerceptionBuilder.Attach(
                _perception,
                model,
                host,
                _toolTarget as ISeededToolSolver,
                _positioner as IArmStowPoseSource,
                _camera,
                _recognition,
                _collision,
                message => Debug.Log($"{name}: {message}", this));
        }

        // 설정을 협력 객체에 나눠 주어 조립한다. 순서 객체들은 씬을 몰라도 시험할 수 있다.
        private void AssembleCollaborators(IToolPoseTarget tool, IArmBasePositioner basePositioner, StandPolicy policy)
        {
            // 알려 준 관절 각도로 가려면 관절 목표를 받는 쪽과 출발 자세를 고르는 풀이가 모두 있어야 한다. 없으면 도구 끝점으로만 간다.
            var seededSolver = _toolTarget as ISeededToolSolver;
            JointPoseMover jointMover = _toolTarget is IJointPoseTarget jointTarget && seededSolver != null
                ? new JointPoseMover(jointTarget, () => SimClock.Now, _stepTimeoutSeconds)
                : null;

            var diagnostics = new TransferDiagnostics(
                tool, _toolTarget as IToolTrackingDiagnostics, _toolTarget as IToolReachFrame, () => IsHeld, _positionToleranceM);
            var tolerances = new MotionTolerances(
                _travelPositionToleranceM, _travelRotationToleranceDeg, _positionToleranceM, _rotationToleranceDeg,
                _travelJointToleranceDeg, _jointToleranceDeg, _travelSolvePositionToleranceM, _travelSolveRotationToleranceDeg);
            var toolMover = new ToolPoseMover(tool, _travelPositionToleranceM, _travelRotationToleranceDeg, _stepTimeoutSeconds);
            var steps = new ArmMotionSteps(basePositioner, toolMover, jointMover, diagnostics, tolerances);
            var grasp = new ShelfGraspSequence(_itemHolder, steps, _perception);

            // 서 보기 전에 가늠할 때는 접힌 자세에서 출발했을 때를 기준으로 묻는다. 베이스가 움직이기 전에 팔이 접히기 때문이다.
            // 장애물과 겹치는지도 함께 물어, 닿는 방향 가운데 겹치지 않는 방향과 서는 자리를 우선한다.
            ClearancePolicy clearance = _collision.ToPolicy();
            FrameBudget searchBudget = CreateSearchBudget();
            var planningProbe = new StowedPoseReachProbe(_positioner as IArmStowPoseSource, seededSolver, pose => tool.Probe(pose).Converged);
            var standSearch = new StandSearchRoutine(
                steps,
                new GraspOrientationSearcher(
                    pose => tool.Probe(pose).Converged, _perception.IsClear, clearance, _perception.IsClearAlong, searchBudget,
                    _perception.DescribeClearAlong),
                policy.CreateCost(),
                planningProbe.CanReach,
                _positioner as IArmBaseStateSource,
                diagnostics,
                policy.SearchSettings,
                () => SimClock.Now,
                message => Debug.Log($"{name}: {message}", this),
                _perception.IsClearAtBase,
                clearance,
                searchBudget);

            // 알려 준 자세가 물건에서 카메라로 고칠 수 있는 거리보다 멀면 그 자세를 쓰지 않는다(고쳐도 쥘 수 없다).
            var pickSettings = new ShelfPickSettings(_shelfApproachDistance, _shelfRetreatDistance, _shelfSearch, _taughtShiftLimitM);
            _searchedPick = new SearchedPickRoutine(policy, standSearch, grasp, pickSettings);
            // 알려 준 자세로 가는 길이 겹치면 알려 준 방향 가까이에서 겹치지 않는 방향을 찾는다. 풀이가 없으면 겹침을 따질 수 없다.
            TaughtClearancePlanner taughtClearance = jointMover != null
                ? new TaughtClearancePlanner(
                    new SeededRouteBuilder(seededSolver), _perception.IsRouteClear, _perception.DescribeRouteCollision, clearance, _taughtSearch,
                    searchBudget)
                : null;
            _taughtPick = new TaughtPickRoutine(
                policy, steps, grasp, jointMover != null ? seededSolver : null, diagnostics, pickSettings,
                message => Debug.LogWarning($"{name}: {message}", this), taughtClearance);
            _boxPlace = new BoxPlaceRoutine(
                policy, standSearch, steps, _itemHolder,
                new BoxPlaceSettings(
                    _boxApproachHeight, _boxLiftHeight, _boxGripYawForward, _boxSearch,
                    _placeInRestingOrientation ? _boxRestingSearch : null, new ReleaseHeights(_boxReleaseHeights)),
                CurrentToolPose,
                message => Debug.Log($"{name}: {message}", this),
                CreateBaseClearance(policy),
                _logPlacementTrace
                    ? new BoxPlaceDiagnostics(tool.Probe, _baseMotionTraceMaxEntries, _baseMotionTraceMinYawTravelDeg)
                    : null,
                _positioner as IArmPhaseSource,
                CreateBaseMotionProbe(),
                _rememberPlacements ? new PlacementMemory(_placementMemoryRadiusM) : null);

            _taughtFinder = new TaughtPoseFinder(
                _useTaughtPoses ? new TaughtPickPoseRepository(FilePickPoseStorage.CreateDefault()) : null,
                message => Debug.LogError($"{name}: {message}", this));
        }

        // 베이스를 옮기는 동안 물리 스텝마다 재는 데 필요한 것. 끄거나 진단을 껐거나 굴러가는 베이스를 알 수 없으면 만들지 않는다.
        // 스텝 알림은 FixedUpdate에서 나오므로 이 오브젝트에 알림 컴포넌트를 하나 단다.
        private BaseMotionProbe CreateBaseMotionProbe()
        {
            if (!_logPlacementTrace || !_logBaseMotionTrace)
            {
                return null;
            }

            IMobileBase mobile = (_positioner as ArmBasePositionerBehaviour)?.MobileBase;
            if (mobile == null)
            {
                return null;
            }

            PhysicsStepTicker ticker = GetComponent<PhysicsStepTicker>();
            if (ticker == null)
            {
                ticker = gameObject.AddComponent<PhysicsStepTicker>();
            }

            // 시작과 끝은 코루틴(Update 쪽)에서, 그 사이는 FixedUpdate에서 재므로 둘에서 같은 뜻을 가진 fixedTime(마지막 물리 스텝의 시각)을 쓴다.
            // Time.time은 Update에서 프레임 시각이라 FixedUpdate의 스텝 시각보다 앞설 수 있어 시간이 거꾸로 가 보인다.
            return new BaseMotionProbe(
                ticker, mobile, () => Time.fixedTime, _baseMotionSlipOnsetCm / CentimetersPerMeter,
                _baseMotionStillSpeedMps, _baseMotionStillYawRateDegPerSec);
        }

        // 박스 곁에 서는 자리에서 팔 베이스가 레일 같은 장애물에 파고들지 않게 하는 쪽. 베이스를 못 옮기는 팔이거나 끄면 만들지 않는다.
        // 베이스의 충돌 상자는 로봇이 준비된 뒤에 채워지므로(ArmPerception), 그 전에는 겹침을 따지지 않고 정해진 자리에 선다.
        private BaseStandClearance CreateBaseClearance(StandPolicy policy)
        {
            if (!policy.CanMoveBase || !_collision.Enabled || !_collision.AvoidBaseOverlap)
            {
                return null;
            }

            return new BaseStandClearance(
                _positioner as IArmBaseStateSource,
                _perception.DescribeBaseCollision,
                _collision.BaseRetreatStepM,
                _collision.MaxBaseRetreatM,
                message => Debug.Log($"{name}: {message}", this));
        }

        // 도구 끝점의 지금 월드 자세. 팔 모델이 아직 준비되지 않았으면 null이다. 쥔 물건이 도구에 대해 어떤 자세인지 잴 때 쓴다.
        private Pose? CurrentToolPose()
        {
            IArmModel model = (_toolTarget as IArmModelSource)?.Model;
            return model != null && model.IsReady ? model.ToolPose : (Pose?)null;
        }

        // 찾기들이 한 프레임의 시간을 나눠 쓰도록 하나만 만든다. 0ms이면 없다(프레임마다 후보 하나씩).
        // 예산은 배속을 따라 늘어난다(FrameBudget 설명 참고).
        private FrameBudget CreateSearchBudget()
        {
            return _searchBudgetMs > 0f
                ? new FrameBudget(_searchBudgetMs / 1000.0, () => Time.realtimeSinceStartupAsDouble, () => SimClock.Speed)
                : null;
        }

        // 그 칸에 사람이 알려 준 자세가 있으면 그 자세로 집고, 없으면 서는 자리와 도구 방향을 스스로 찾아 집는다.
        // 알려 준 자세로 가는 길이 선반과 겹치고 방향을 바꿔도 피할 수 없으면, 서는 자리까지 새로 찾아 겹치지 않게 집는다.
        public IEnumerator Pick(ItemPickRequest request, TransferResult result)
        {
            string slotId = request.Item.SlotId;
            if (_taughtFinder.TryFind(slotId, out TaughtPickPose taught))
            {
                Debug.Log(
                    $"{name}: 칸 {taught.SlotId}은 알려 준 자세로 집는다 — ArmA x {taught.RootPosition.x:0.00} z {taught.RootPosition.z:0.00}, 방향 {taught.RootYawDeg:0.0}°",
                    this);
                yield return _taughtPick.Run(request, taught, result);
                if (!result.CanRetryBySearching)
                {
                    yield break;
                }

                // 알려 준 자세를 쓸 수 없다: 방향을 바꿔도 선반과 겹치거나, 물건 자리가 알려 준 자리에서 옮길 수 있는 한계보다 멀다.
                // 서는 자리(뒤로 물러나기, 옆으로 비키기)와 도구 방향을 새로 찾아 집는다.
                Debug.LogWarning($"{name}: {result.Detail} — 서는 자리와 도구 방향을 새로 찾아 집는다.", this);
            }
            else
            {
                // 어느 쪽으로 집는지 Console에서 알 수 있어야 저장한 자세가 안 쓰인 이유를 찾을 수 있다.
                Debug.Log($"{name}: {_taughtFinder.WhyNot(slotId)} 스스로 찾아 집는다.", this);
            }

            yield return _searchedPick.Run(request, result);
        }

        public IEnumerator Place(ItemPlaceRequest request, TransferResult result)
        {
            Debug.Log($"{name}: 쥔 물건을 박스 슬롯에 놓으러 간다.", this);
            return _boxPlace.Run(request, result);
        }

        public void Discard()
        {
            _itemHolder.Discard();
        }
    }
}
