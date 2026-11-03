using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;
using Warcool.WarehouseRobot;

namespace MediKitTwin.Unloader
{
    /// <summary>
    /// 하역 로봇(UnloaderRobot)의 한 번의 배달: 컨베이어 끝 받침대에서 박스를 집어 AGVPath를 따라 하역장까지 가서 키트 종류의 구역에 내려놓고 돌아온다. (FR-12)
    /// 순서: 박스 등록(AgvPickPlatform) → 받침대 자리로 이동·정렬 → 집기(Load) → 뒤로 물러남 → 경로를 구역 가까이까지 이동
    /// → 구역 자리로 이동·정렬 → 내려놓기(Unload) → CompleteTime을 채우고 KitShipped → 뒤로 물러남 → 같은 경로를 되짚어 받침대 자리로 복귀.
    /// 서야 할 자리(DockPoint)는 받침대나 단 가장자리에서 팔이 닿는 거리밖에 안 떨어져 있어 제자리에서 돌면 로봇과 박스가 물체를 파고든다.
    /// 그래서 자리에 들어갈 때는 자리에서 StagingDistance만큼 뒤(스테이징)에서 방향을 돌린 뒤 똑바로 전진하고, 나올 때는 똑바로 후진해 스테이징까지 물러난 뒤 돈다.
    /// 경로는 끝까지 가지 않고 목표 자리에 가장 가까운 지점까지만 따라간다(AgvRoute). 구역이 여럿이고 경로는 하나이기 때문이다.
    /// 집기와 내려놓기는 에셋의 고정 애니메이션(RobotCargoInteractor.RequestInteract)이 하고, 이 컴포넌트는 로봇이 구역에 들어와 정렬될 때까지
    /// 요청을 되풀이한다. 이동은 IReversibleMobileBase(MobileBaseDriver)로만 지시한다. 시간은 Time.deltaTime이라 SimClock 배속이 적용된다.
    /// 박스가 등록된 순서대로 하나씩 처리한다. 실패하면 경고를 알리고 그 박스는 건너뛴다(KitShipped를 보내지 않는다).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AgvUnloadSequence : MonoBehaviour
    {
        [Tooltip("박스를 집고 내려놓는 에셋의 컴포넌트. 로봇의 트리거 콜라이더와 같은 오브젝트에 있다.")]
        [SerializeField] private RobotCargoInteractor _interactor;

        [Tooltip("IReversibleMobileBase를 구현한 이동 컴포넌트(MobileBaseDriver). 집기 컴포넌트와 같은 오브젝트에 두어야 정렬 방향이 맞는다.")]
        [SerializeField] private MonoBehaviour _mobileBase;

        [Tooltip("컨베이어 끝 받침대. 박스가 칸에 등록되면 알려 준다.")]
        [SerializeField] private AgvPickPlatform _pickPlatform;

        [Tooltip("컨베이어 끝 받침대의 감지 구역. 로봇이 서야 할 자리(dockPoint)를 여기서 읽는다.")]
        [SerializeField] private ShelfInteractionZone _pickZone;

        [Tooltip("하역장의 키트별 구역. 박스의 키트 코드로 구역과 로봇이 서야 할 자리를 정한다.")]
        [SerializeField] private UnloadingDockStock _dock;

        [Tooltip("AGVPath. 자식 웨이포인트를 순서대로 지나 하역장으로 가고, 같은 길로 되짚어 돌아온다. 비우거나 자식이 없으면 곧장 간다.")]
        [SerializeField] private Transform _path;

        [Tooltip("서야 할 자리에서 뒤로 이만큼(m) 떨어진 곳에서 방향을 돌린다. 로봇 몸과 든 박스가 제자리에서 돌 때 휘두르는 반경(약 0.35m)보다 " +
                 "자리와 물체 사이 거리(약 0.21m)와 이 값의 합이 커야 한다.")]
        [SerializeField, Min(0.1f)] private float _stagingDistanceM = 0.5f;

        [Tooltip("한 지점으로 이동하는 데 허용하는 시뮬레이션 시간(초). 넘으면 경고하고 그 배달을 포기한다.")]
        [SerializeField, Min(1f)] private float _driveTimeoutSeconds = 60f;

        [Tooltip("집기나 내려놓기를 시작할 때까지 요청을 되풀이하는 시뮬레이션 시간(초). 로봇이 구역에 들어와 정렬되고, 내려놓을 칸이 비기를 기다리는 시간이다.")]
        [SerializeField, Min(0.1f)] private float _interactTimeoutSeconds = 10f;

        [Tooltip("웨이포인트로 읽을 AGVPath 자식의 이름 앞부분. AGVPath에는 노란 레인(AGV_Strip_n)도 자식으로 있어 이름으로 가려낸다.")]
        [SerializeField] private string _waypointNamePrefix = "AGV_WP";

        [Tooltip("로봇 몸의 크기를 알려 주는 박스 콜라이더(집기 컴포넌트와 같은 오브젝트). 경로 위에서 물체와 겹치는 점을 밀어낼 때 쓴다. 비우면 경로를 그대로 따른다.")]
        [SerializeField] private BoxCollider _footprint;

        [Tooltip("로봇 몸과 물체 사이에 남길 여유(m).")]
        [SerializeField, Min(0f)] private float _clearanceMarginM = 0.03f;

        [Tooltip("경로를 이 간격(m)으로 훑어 물체와 겹치는 곳을 찾는다.")]
        [SerializeField, Min(0.05f)] private float _clearanceSampleStepM = 0.15f;

        [Tooltip("윗면이 이 높이(m)보다 낮은 것은 바닥이나 레인으로 보고 피하지 않는다.")]
        [SerializeField, Min(0f)] private float _ignoreBelowM = 0.05f;

        // 이 이상 옮겨야 겹침을 피할 수 있으면 사람에게 알린다(m). 그보다 작은 조정은 경로를 따른 것으로 본다.
        private const float AdjustmentReportM = 0.01f;
        private const int MaxPushIterations = 4;
        private const int MaxOverlaps = 32;

        private readonly Queue<KitData> _waiting = new Queue<KitData>();
        private readonly List<Vector3> _waypoints = new List<Vector3>();
        private readonly List<Vector3> _route = new List<Vector3>();
        private readonly List<Bounds> _obstacles = new List<Bounds>();
        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private IReversibleMobileBase _base;
        private Vector3 _routeStart;
        private bool _arrived;
        private bool _stepOk;
        private bool _reportedAdjustment;

        private void Awake()
        {
            _base = _mobileBase as IReversibleMobileBase;
            if (_interactor == null || _base == null || _pickPlatform == null || _pickZone == null || _dock == null)
            {
                Debug.LogError($"{name}: Interactor, Mobile Base(IReversibleMobileBase), Pick Platform, Pick Zone, Dock을 지정해야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _pickPlatform.BoxRegistered += OnBoxRegistered;
            _base.Arrived += OnArrived;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            _pickPlatform.BoxRegistered -= OnBoxRegistered;
            _base.Arrived -= OnArrived;
        }

        private void OnBoxRegistered(KitData kit)
        {
            _waiting.Enqueue(kit);
        }

        private void OnArrived()
        {
            _arrived = true;
        }

        private IEnumerator Run()
        {
            while (true)
            {
                while (_waiting.Count == 0)
                {
                    yield return null;
                }

                yield return Deliver(_waiting.Dequeue());
            }
        }

        private IEnumerator Deliver(KitData kit)
        {
            // 박스를 집기 전에 내려놓을 구역을 확인한다. 구역이 없는데 박스부터 집으면 로봇이 박스를 든 채 멈춘다.
            ShelfInteractionZone zone = ChooseDockZone(kit);
            if (zone == null || zone.dockPoint == null)
            {
                CellEvents.RaiseWarning(UnloaderWarningCodes.NoDockZone, $"키트 {kit.KitSequence}를 내려놓을 하역장 구역의 로봇 자리가 연결되어 있지 않다.");
                yield break;
            }

            ShelfInteractionZone pickZone = _pickZone;

            BuildRoute(pickZone, zone);

            yield return EnterPose(pickZone);
            if (!_stepOk)
            {
                yield break;
            }

            yield return Interact(wantsBox: true);
            if (!_stepOk)
            {
                CellEvents.RaiseWarning(UnloaderWarningCodes.LoadFailed, $"키트 {kit.KitSequence}의 박스를 받침대에서 집지 못했다.");
                yield break;
            }

            yield return LeavePose(pickZone);
            if (_stepOk)
            {
                yield return FollowRoute(reverse: false);
            }

            if (_stepOk)
            {
                yield return EnterPose(zone);
            }

            if (!_stepOk)
            {
                yield break;
            }

            yield return Interact(wantsBox: false);
            if (!_stepOk)
            {
                CellEvents.RaiseWarning(UnloaderWarningCodes.UnloadFailed, $"키트 {kit.KitSequence}의 박스를 하역장에 내려놓지 못했다.");
                yield break;
            }

            kit.CompleteTime = SimClock.Now;
            CellEvents.RaiseKitShipped(kit);

            yield return LeavePose(zone);
            if (_stepOk)
            {
                yield return FollowRoute(reverse: true);
            }

            if (_stepOk)
            {
                yield return EnterPose(pickZone);
            }
        }

        // 키트 코드에 맞는 구역. 없으면 경고하고 첫 구역을 쓴다(배달을 멈추지 않는다).
        private ShelfInteractionZone ChooseDockZone(KitData kit)
        {
            int column = _dock.ColumnFor(kit.KitCode);
            if (column < 0)
            {
                CellEvents.RaiseWarning(UnloaderWarningCodes.NoDockZone, $"키트 코드 {kit.KitCode}에 맞는 하역장 구역이 없어 첫 구역에 내려놓는다.");
                column = 0;
            }

            return _dock.ZoneFor(column);
        }

        // AGVPath의 자식 중 이름이 웨이포인트 접두사로 시작하는 것만 순서대로 읽는다. 레인(AGV_Strip_n)은 경로가 아니다.
        private void ReadWaypoints()
        {
            _waypoints.Clear();
            if (_path == null)
            {
                return;
            }

            for (int i = 0; i < _path.childCount; i++)
            {
                Transform child = _path.GetChild(i);
                if (child.name.StartsWith(_waypointNamePrefix, System.StringComparison.Ordinal))
                {
                    _waypoints.Add(child.position);
                }
            }
        }

        // 받침대 스테이징에서 하역장 스테이징까지 지날 점들을 만든다. 경로(웨이포인트)는 목표 구역에 가장 가까운 지점까지만 쓰고,
        // 로봇 몸이 물체와 겹치는 지점은 물체 밖으로 최소한만 옮긴다. 스테이징은 비어 있는 자리라 옮기지 않는다.
        private void BuildRoute(ShelfInteractionZone pickZone, ShelfInteractionZone dockZone)
        {
            ReadWaypoints();
            int viaCount = AgvRoute.WaypointsBefore(_waypoints, dockZone.dockPoint.position);

            _routeStart = AgvRoute.StagePoint(pickZone.dockPoint.position, pickZone.dockPoint.eulerAngles.y, _stagingDistanceM);
            Vector3 routeEnd = AgvRoute.StagePoint(dockZone.dockPoint.position, dockZone.dockPoint.eulerAngles.y, _stagingDistanceM);

            _route.Clear();
            Vector3 previous = _routeStart;
            float largestShift = 0f;
            for (int i = 0; i <= viaCount; i++)
            {
                bool isEnd = i == viaCount;
                Vector3 target = isEnd ? routeEnd : _waypoints[i];
                largestShift = Mathf.Max(largestShift, AppendLeg(previous, target, clearEnd: !isEnd));
                previous = _route[_route.Count - 1];
            }

            if (largestShift > AdjustmentReportM && !_reportedAdjustment)
            {
                _reportedAdjustment = true;
                Debug.LogWarning($"{name}: AGVPath가 물체에 너무 가까워 로봇이 지나는 점을 최대 {largestShift * 100f:0}cm 옮겼다. 경로를 물체에서 로봇 반폭 이상 띄우면 경로 그대로 간다.", this);
            }
        }

        // previous에서 target까지 가는 길을 일정 간격으로 훑어, 로봇 몸이 물체와 겹치는 점은 밀어낸 점을 경로에 넣는다. 밀어낸 가장 큰 거리를 돌려준다.
        private float AppendLeg(Vector3 previous, Vector3 target, bool clearEnd)
        {
            float largestShift = 0f;
            Vector3 toTarget = target - previous;
            toTarget.y = 0f;
            float length = toTarget.magnitude;
            float yaw = length > 0f ? Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg : 0f;

            int samples = Mathf.Max(1, Mathf.CeilToInt(length / _clearanceSampleStepM));
            for (int s = 1; s < samples; s++)
            {
                Vector3 point = Vector3.Lerp(previous, target, (float)s / samples);
                Vector3 cleared = ClearOfObstacles(point, yaw);
                float shift = HorizontalDistance(point, cleared);
                if (shift > AdjustmentReportM)
                {
                    _route.Add(cleared);
                    largestShift = Mathf.Max(largestShift, shift);
                }
            }

            Vector3 end = clearEnd ? ClearOfObstacles(target, yaw) : target;
            largestShift = Mathf.Max(largestShift, HorizontalDistance(target, end));
            _route.Add(end);
            return largestShift;
        }

        // 로봇 몸(footprint)을 yawDeg 방향으로 point에 놓았을 때 겹치는 물체 밖으로 민 점. 몸 크기를 모르면 그대로 둔다.
        private Vector3 ClearOfObstacles(Vector3 point, float yawDeg)
        {
            if (_footprint == null)
            {
                return point;
            }

            Vector3 scale = _footprint.transform.lossyScale;
            float halfWidth = 0.5f * _footprint.size.x * Mathf.Abs(scale.x);
            float halfLength = 0.5f * _footprint.size.z * Mathf.Abs(scale.z);
            float halfHeight = 0.5f * _footprint.size.y * Mathf.Abs(scale.y);
            Vector2 half = AgvClearance.FootprintHalf(halfWidth, halfLength, yawDeg);

            for (int iteration = 0; iteration < MaxPushIterations; iteration++)
            {
                GatherObstacles(point, half, halfHeight);
                Vector3 pushed = AgvClearance.PushOut(point, half, _obstacles, _clearanceMarginM);
                if (pushed == point)
                {
                    break;
                }

                point = pushed;
            }

            return point;
        }

        // 로봇 몸이 닿을 수 있는 범위의 물체(트리거, 로봇 자신, 바닥, 레인은 뺀다)의 경계 상자를 모은다.
        private void GatherObstacles(Vector3 point, Vector2 half, float halfHeight)
        {
            _obstacles.Clear();
            Vector3 center = new Vector3(point.x, _footprint.bounds.center.y, point.z);
            var extents = new Vector3(half.x + _clearanceMarginM, halfHeight, half.y + _clearanceMarginM);
            int count = Physics.OverlapBoxNonAlloc(center, extents, _overlaps, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = _overlaps[i];
                if (other.transform.IsChildOf(transform) || other.bounds.max.y < _ignoreBelowM)
                {
                    continue;
                }

                _obstacles.Add(other.bounds);
            }
        }

        // 만든 경로를 차례로 지난다. 되짚을 때는 거꾸로 지나 받침대 스테이징에서 끝난다(하역장 스테이징에서 출발하므로 마지막 점은 건너뛴다).
        private IEnumerator FollowRoute(bool reverse)
        {
            _stepOk = true;
            if (!reverse)
            {
                for (int i = 0; i < _route.Count && _stepOk; i++)
                {
                    yield return Move(_route[i], null, backwards: false);
                }

                yield break;
            }

            for (int i = _route.Count - 2; i >= 0 && _stepOk; i--)
            {
                yield return Move(_route[i], null, backwards: false);
            }

            if (_stepOk)
            {
                yield return Move(_routeStart, null, backwards: false);
            }
        }

        // 서야 할 자리에 들어간다: 스테이징에서 방향을 돌린 뒤 똑바로 전진한다. 이미 자리에 서 있으면 그대로 둔다.
        // 서야 할 자리는 구역의 dockPoint이고, 이미 자리에 서 있는지는 구역이 인정하는 위치 허용 오차로 본다.
        private IEnumerator EnterPose(ShelfInteractionZone zone)
        {
            Transform pose = zone.dockPoint;
            float yaw = pose.eulerAngles.y;
            _stepOk = true;

            if (HorizontalDistance(_base.Position, pose.position) > zone.positionTolerance)
            {
                Vector3 stage = AgvRoute.StagePoint(pose.position, yaw, _stagingDistanceM);
                yield return Move(stage, yaw, backwards: false);
                if (!_stepOk)
                {
                    yield break;
                }
            }

            yield return Move(pose.position, yaw, backwards: false);
        }

        // 서 있던 자리에서 똑바로 후진해 스테이징까지 물러난다. 돌려면 여기서 돈다.
        private IEnumerator LeavePose(ShelfInteractionZone zone)
        {
            Transform pose = zone.dockPoint;
            Vector3 stage = AgvRoute.StagePoint(pose.position, pose.eulerAngles.y, _stagingDistanceM);
            yield return Move(stage, null, backwards: true);
        }

        // 도착(IMobileBase.Arrived)을 기다린다. 정해진 시간 안에 못 오면 멈추고 실패로 알린다.
        private IEnumerator Move(Vector3 position, float? finalYawDeg, bool backwards)
        {
            _arrived = false;
            if (backwards)
            {
                _base.ReverseTo(position);
            }
            else if (finalYawDeg.HasValue)
            {
                _base.DriveTo(position, finalYawDeg.Value);
            }
            else
            {
                _base.DriveTo(position);
            }

            float waited = 0f;
            while (!_arrived && waited < _driveTimeoutSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            _stepOk = _arrived;
            if (!_arrived)
            {
                _base.Stop();
                CellEvents.RaiseWarning(UnloaderWarningCodes.DriveTimeout, $"하역 로봇이 {_driveTimeoutSeconds:0}초 안에 {position}에 도착하지 못했다.");
            }
        }

        // 집기(박스가 없을 때)나 내려놓기(박스를 들고 있을 때)를 시작하고 끝날 때까지 기다린다.
        // 구역에 들어오지 않았거나 정렬이 어긋났거나 칸이 차 있으면 시작되지 않으므로 시간 안에서 되풀이한다.
        private IEnumerator Interact(bool wantsBox)
        {
            float waited = 0f;
            bool started = false;
            while (!started && waited < _interactTimeoutSeconds)
            {
                started = _interactor.RequestInteract();
                if (!started)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }

            if (!started)
            {
                _stepOk = false;
                yield break;
            }

            while (_interactor.IsBusy)
            {
                yield return null;
            }

            _stepOk = _interactor.HasBox == wantsBox;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
