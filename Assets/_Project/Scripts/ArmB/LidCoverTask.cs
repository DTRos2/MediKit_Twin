using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmB
{
    /// <summary>
    /// 로봇암 B가 뚜껑 공급대의 맨 위 뚜껑을 집어 컨베이어 위 키트 박스에 덮는다. (FR-10)
    /// AtPacking 신호를 받으면 시작하고, 뚜껑이 박스에 안착하면 LidClosed를 보낸다.
    /// 로봇은 IToolPoseTarget(목표 자세)과 IGripper(열고 닫기)로만 다루므로 어느 팔에도 연결할 수 있다.
    /// 뚜껑에는 Rigidbody와 Graspable이 있어야 하고(Tools > MediKit > Setup ArmB Lid Grasp), 물리적으로 쥔다.
    /// 어느 단계든 목표에 stepTimeoutSeconds 안에 닿지 못하면 Fault로 바꾸고 멈춘다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LidCoverTask : MonoBehaviour
    {
        // 실패했을 때 팔 곁에 붙은 충돌체로 적을 거리(m). 물리 접촉이 닿기 직전부터 잡히도록 여유를 조금 둔다.
        private const float BlockerMarginM = 0.01f;

        // 목표에 닿았다고 보는 기준. Normal은 목표와의 거리 하나로 본다.
        // Grasp는 손가락이 닫히는 방향(뚜껑 폭 방향)만 엄격하게 보고(여유가 한쪽 5mm뿐이다), 나머지 방향은 Loose 오차까지 허용한다.
        // Cover는 뚜껑과 박스 사이 틈이 사방 5mm이므로 가로 두 방향을 엄격하게 보고, 높이는 Loose 오차까지 허용한다.
        // 팔 무게로 끝점이 몇 mm 처지는 것은 쥐거나 덮는 데 영향이 작기 때문이다.
        private enum Precision
        {
            Normal,
            Grasp,
            Cover
        }

        [Header("연결")]
        [Tooltip("도구 끝점의 목표 자세를 받는 쪽(IToolPoseTarget 구현체, 보통 IkArmTarget).")]
        [SerializeField] private MonoBehaviour toolTarget;
        [Tooltip("그리퍼(IGripper 구현체, 보통 JointGripper).")]
        [SerializeField] private MonoBehaviour gripper;
        [SerializeField] private LidDispenser dispenser;
        [Tooltip("덮을 키트 박스를 찾을 부모(Sensor_Packing). 이름에 KitBox가 들어간 첫 자손을 박스로 쓴다.")]
        [SerializeField] private Transform kitBoxSearchRoot;

        [Header("동작")]
        [Tooltip("위아래로 움직일 때 끊어서 이동하는 간격(m). 관절은 관절 공간에서 직선으로 움직이므로 한 번에 크게 내려가면 끝점이 옆으로 휘어 손가락이 뚜껑 윗판에 얹힌다.")]
        [SerializeField, Min(0.005f)] private float linearStepM = 0.01f;
        [Tooltip("쥐거나 덮기 전에 물체 위에서 멈추는 높이(m).")]
        [SerializeField, Min(0.02f)] private float approachHeight = 0.10f;
        [Tooltip("뚜껑을 쥔 뒤 들어 올리는 높이(m). 공급대 벽을 넘을 만큼이면 되지만, 박스 쪽으로 돌 때 뚜껑이 컨베이어 위를 스치지 않을 만큼도 올라야 한다. 박스가 벨트에 얹혀 낮아지면(0.06m) 덮을 자리도 낮아져서 0.10m로는 돌다가 컨베이어 모서리에 걸려 어깨가 멈췄다.")]
        [SerializeField, Min(0.02f)] private float liftHeight = 0.16f;

        [Header("판정")]
        [Tooltip("경유 지점에서 허용하는 위치 오차(m).")]
        [SerializeField, Min(0.001f)] private float looseToleranceM = 0.01f;
        [Tooltip("쥐고 덮는 지점에서 엄격하게 보는 위치 오차(m). 쥘 때는 손가락이 닫히는 방향, 덮을 때는 가로 두 방향에 쓴다. 그리퍼 여유는 한쪽 5.5mm, 뚜껑과 박스 사이 틈은 5mm이다.")]
        [SerializeField, Min(0.0005f)] private float preciseToleranceM = 0.002f;
        [SerializeField, Min(0.5f)] private float looseRotationToleranceDeg = 5f;
        [Tooltip("쥐고 덮는 지점의 회전 오차(도). 뚜껑 긴 변 0.27m에서 1도는 약 5mm이므로 작게 둔다.")]
        [SerializeField, Min(0.1f)] private float preciseRotationToleranceDeg = 2f;
        [Tooltip("박스 위에 뚜껑을 내려놓는 하강의 기울기 오차(도). 뚜껑은 놓으면 박스 위에 평평하게 내려앉으므로 쥘 때보다 넓게 둔다. 팔 처짐으로 3도쯤 어긋난다. 수평 방향(박스 긴 변과의 어긋남)은 Precise Rotation Tolerance Deg로 엄격하게 본다.")]
        [FormerlySerializedAs("coverRotationToleranceDeg")]
        [SerializeField, Min(0.5f)] private float coverTiltToleranceDeg = 5f;
        [Tooltip("박스 위에 뚜껑을 내려놓는 하강의 가로 오차(m). 뚜껑과 박스 사이 틈이 사방 5mm이므로 그보다 작게 둔다.")]
        [SerializeField, Min(0.0005f)] private float coverToleranceM = 0.003f;
        [Tooltip("한 목표에 닿기까지 기다리는 시간(초, 시뮬레이션 시간). 지나면 Fault. SRS FR-10은 3초이다.")]
        [SerializeField, Min(0.5f)] private float stepTimeoutSeconds = 3f;
        [SerializeField, Min(0.5f)] private float graspTimeoutSeconds = 3f;

        [Header("그리퍼")]
        [Tooltip("손가락 관절(knuckle, finger)의 구동 힘 한계(N·m). ArmDriveSetup 기본값 1000은 실제 그리퍼의 수십~백 배여서 얇은 뚜껑 벽을 뚫고 지나간다. 쥐기 직전에 이 값으로 낮춘다. 손가락 길이 약 6cm에서 5N·m는 약 80N이다. 0이면 건드리지 않는다.")]
        [SerializeField, Min(0f)] private float fingerForceLimit = 5f;

        [Header("끝점 보정")]
        [Tooltip("쥐고 덮는 동작에서 팔이 중력과 마찰로 목표에서 어긋나면, 어긋난 만큼 반대로 목표를 옮겨 바로잡는다. 0이면 끈다.")]
        [SerializeField, Range(0f, 1f)] private float correctionGain = 0.6f;
        [Tooltip("보정을 시작하기 전에 팔이 먼저 움직이도록 기다리는 시간과, 그 뒤 보정하는 간격(초).")]
        [SerializeField, Min(0.1f)] private float correctionIntervalSeconds = 0.3f;
        [Tooltip("보정으로 목표를 옮길 수 있는 최대 거리(m). 막혀 있을 때 계속 밀어 넣지 않도록 제한한다.")]
        [SerializeField, Min(0.001f)] private float maxCorrectionM = 0.02f;
        [Tooltip("그리퍼를 연 뒤 뚜껑이 내려앉기를 기다리는 시간(초).")]
        [SerializeField, Min(0f)] private float settleSeconds = 0.3f;

        [Header("뚜껑 모양 (CellSceneBuilder와 같은 값)")]
        [SerializeField, Min(0.01f)] private float lidHeight = 0.04f;
        [SerializeField, Min(0.001f)] private float lidWall = 0.005f;
        [Tooltip("덮은 뒤 뚜껑이 박스에서 벗어나도 안착으로 보는 오차(m). 뚜껑과 박스 사이 틈(5mm)보다 조금 크게 둔다.")]
        [SerializeField, Min(0.001f)] private float seatToleranceM = 0.008f;

        private IToolPoseTarget _tool;
        private IGripper _grip;
        private IArmState _state;
        private IStationStateReporter _station;
        private Coroutine _running;
        private bool _stepReached;
        private string _stepNote = string.Empty;
        private Transform _heldLid;

        public bool IsRunning => _running != null;

        /// <summary>작업이 끝났을 때 발생한다. 뚜껑을 덮었으면 true.</summary>
        public event Action<bool> Finished;

        private void Awake()
        {
            _tool = toolTarget as IToolPoseTarget;
            _grip = gripper as IGripper;
            if (_tool == null || _grip == null || dispenser == null)
            {
                Debug.LogError($"[LidCoverTask] {name}: Tool Target(IToolPoseTarget), Gripper(IGripper), Dispenser를 모두 지정해야 합니다.", this);
                enabled = false;
                return;
            }

            // 실패했을 때 팔이 실제로 어디까지 갔는지 알리기 위해 같은 오브젝트의 팔 상태를 찾는다. 없어도 동작한다.
            _state = toolTarget.GetComponent<IArmState>();
            _station = new StationStateReporter(StationId.ArmB);
        }

        private void OnEnable()
        {
            CellEvents.AtPacking += Begin;
        }

        private void OnDisable()
        {
            CellEvents.AtPacking -= Begin;
        }

        private void Update()
        {
            // 일시정지나 배속에 영향받지 않도록 실제 시간을 넘긴다.
            _station?.Tick(Time.unscaledTime);
        }

        /// <summary>양품 박스가 팩킹 구역에 도착했을 때 뚜껑 덮기를 시작한다. 이미 하고 있으면 무시한다.</summary>
        public void Begin(KitData kit)
        {
            if (!isActiveAndEnabled || IsRunning)
            {
                return;
            }

            _running = StartCoroutine(Run(kit));
        }

        // 로봇암 B와 컨베이어가 아직 연결되지 않은 지금은 인스펙터에서 직접 눌러 확인한다. (플레이 모드에서 컴포넌트 우클릭)
        [ContextMenu("Run Cover (Test)")]
        private void RunForTest()
        {
            Begin(null);
        }

        private IEnumerator Run(KitData kit)
        {
            _station.Set(StationState.Running);

            Transform kitBox = FindKitBox(kitBoxSearchRoot);
            if (kitBox == null)
            {
                Fail("키트 박스를 찾지 못했습니다. Kit Box Search Root 아래에 이름에 KitBox가 들어간 오브젝트가 있어야 합니다.");
                yield break;
            }

            // 앞서 가져간 뚜껑 자리를 채우며 올라오는 중이면 끝날 때까지 기다린다.
            float waited = 0f;
            while (dispenser.IsBusy && waited < stepTimeoutSeconds)
            {
                yield return null;
                waited += Time.deltaTime;
            }

            if (dispenser.IsBusy)
            {
                Fail("뚜껑 공급대가 뚜껑을 올리는 중이라 가져갈 수 없습니다.");
                yield break;
            }

            // ── 뚜껑 쥐기 ──
            Vector3 lidPosition = dispenser.TopLidWorldPosition;
            Vector3 lidLongAxis = dispenser.TopLidWorldRotation * Vector3.forward;

            if (!TryChooseRotation(lidPosition + Vector3.up * approachHeight, lidLongAxis, lidLongAxis, out Quaternion pickRotation))
            {
                Fail("뚜껑 위로 팔이 닿지 않습니다.");
                yield break;
            }

            ApplyFingerForceLimit();
            _grip.Open();

            // 내려가기 전에 뚜껑 바로 위에 정밀하게 맞춘다. 여유가 한쪽 6mm뿐이라 여기서 어긋나면 손가락이 뚜껑 윗판에 얹힌다.
            Vector3 aboveLid = lidPosition + Vector3.up * approachHeight;
            yield return MoveTool(new Pose(aboveLid, pickRotation), preciseToleranceM, preciseRotationToleranceDeg, Precision.Grasp);
            if (!_stepReached)
            {
                Fail("뚜껑 위로 이동하지 못했습니다.");
                yield break;
            }

            yield return MoveLinear(aboveLid, lidPosition, pickRotation, preciseToleranceM, preciseRotationToleranceDeg, Precision.Grasp);
            if (!_stepReached)
            {
                Fail("뚜껑을 쥘 높이까지 내려가지 못했습니다.");
                yield break;
            }

            // 뚜껑을 공급대에서 떼고 물리 몸으로 바꾼다. 쥘 때까지 떨어지지 않도록 중력은 끈다.
            // 새 뚜껑은 들어 올린 뒤에 올라오게 멈춰 둔다.
            if (!dispenser.TryTakeTopLid(out Transform lid, false))
            {
                Fail("뚜껑을 가져가지 못했습니다.");
                yield break;
            }

            Rigidbody body = lid.GetComponent<Rigidbody>();
            if (body == null)
            {
                dispenser.StartRise();
                Fail("뚜껑에 Rigidbody가 없습니다. Tools > MediKit > Setup ArmB Lid Grasp를 실행하세요.");
                yield break;
            }

            body.isKinematic = false;
            body.useGravity = false;
            _grip.Close();
            yield return WaitForGrasp();
            if (!_grip.IsHolding)
            {
                _grip.Open();
                body.useGravity = true;
                dispenser.StartRise();
                Fail("뚜껑을 쥐지 못했습니다. 뚜껑에 Graspable이 있는지 확인하세요.");
                yield break;
            }

            _heldLid = lid;

            // 들어 올리기는 목표 높이가 아니라 실제 끝점에서 시작한다. 쥐는 동안 끝점이 목표와 어긋나 있으면
            // 공칭 위치 기준 첫 걸음이 현재보다 아래가 되어 팔이 내려가려다 막힌다.
            Vector3 liftStart = CurrentToolPosition(lidPosition);
            yield return MoveLinear(liftStart, liftStart + Vector3.up * liftHeight, pickRotation, looseToleranceM, looseRotationToleranceDeg);
            dispenser.StartRise();
            if (!_stepReached)
            {
                DropLid(body);
                Fail("뚜껑을 들어 올리지 못했습니다.");
                yield break;
            }

            // ── 박스 위로 옮겨 덮기 ──
            float boxTopY = TopOf(kitBox);
            float coverY = LidCoverPlanner.CoverHeight(boxTopY, lidHeight, lidWall);
            Vector3 boxCenter = kitBox.position;
            var lidCenterTarget = new Vector3(boxCenter.x, coverY, boxCenter.z);

            // 쥔 뚜껑은 그리퍼에 대해 몇 mm 비껴서, 조금 기울어져 붙어 있다. 그 자세를 재서 반영해야 뚜껑 중심이 박스 중심에 온다.
            // 반영하지 않으면 끝점(TCP)을 박스 중심에 맞춰 뚜껑이 한쪽으로 쏠리고, 높이 오차만큼 박스를 누르게 된다.
            if (!TryPlanCover(lid, kitBox.forward, lidCenterTarget, out Quaternion coverRotation, out Vector3 coverPosition))
            {
                DropLid(body);
                Fail("키트 박스 위로 팔이 닿지 않습니다.");
                yield break;
            }

            Vector3 aboveBox = coverPosition + Vector3.up * approachHeight;
            yield return MoveTool(new Pose(aboveBox, coverRotation), coverToleranceM, coverTiltToleranceDeg, Precision.Cover);
            if (!_stepReached)
            {
                DropLid(body);
                Fail("키트 박스 위로 이동하지 못했습니다.");
                yield break;
            }

            yield return MoveLinear(aboveBox, coverPosition, coverRotation, coverToleranceM, coverTiltToleranceDeg, Precision.Cover);
            if (!_stepReached)
            {
                DropLid(body);
                Fail("뚜껑을 덮을 높이까지 내려가지 못했습니다.");
                yield break;
            }

            _grip.Open();
            body.useGravity = true;

            // 손가락이 완전히 벌어질 때까지 기다린다. 아직 뚜껑을 누르는 채로 올라가면 뚜껑이 딸려 올라가거나 걸린다.
            float openWait = 0f;
            while (_grip.Opening < 0.99f && openWait < graspTimeoutSeconds)
            {
                yield return new WaitForFixedUpdate();
                openWait += Time.fixedDeltaTime;
            }

            yield return new WaitForSeconds(settleSeconds);

            // 후퇴도 실제 끝점에서 같은 x, z로 곧게 올라간다. 옆으로 움직이면 손가락이 뚜껑을 건드린다.
            Vector3 retreatStart = CurrentToolPosition(coverPosition);
            yield return MoveLinear(retreatStart, new Vector3(retreatStart.x, aboveBox.y, retreatStart.z), coverRotation, looseToleranceM, looseRotationToleranceDeg);
            if (!_stepReached)
            {
                Fail("뚜껑을 덮은 뒤 물러나지 못했습니다.");
                yield break;
            }

            if (!LidCoverPlanner.IsSeated(lid.position, boxCenter, coverY, seatToleranceM))
            {
                // 가로와 높이를 나눠 적는다. 높이가 높으면 뚜껑이 무언가(박스 안 물건)에 얹힌 것이고, 가로가 크면 옆으로 밀린 것이다.
                Vector3 unseated = lid.position - new Vector3(boxCenter.x, coverY, boxCenter.z);
                Fail($"뚜껑이 박스에 제대로 덮이지 않았습니다. 중심 차이 {unseated.magnitude:0.000}m (가로 {new Vector2(unseated.x, unseated.z).magnitude * 1000f:0.0}mm, 높이 {unseated.y * 1000f:0.0}mm)");
                yield break;
            }

            // 덮인 뚜껑은 박스를 따라 움직인다(컨베이어가 박스를 옮긴다).
            lid.SetParent(kitBox, true);
            body.isKinematic = true;

            Vector3 seatError = lid.position - lidCenterTarget;
            Debug.Log($"[LidCoverTask] 뚜껑 덮기 완료: 뚜껑 중심이 박스 중심에서 가로 {new Vector2(seatError.x, seatError.z).magnitude * 1000f:0.0}mm, 높이 {seatError.y * 1000f:0.0}mm 차이. LidClosed 발행", this);
            CellEvents.RaiseLidClosed(kit);
            Finish(true);
        }

        // 쥔 뚜껑의 중심이 lidCenterTarget에 오고 긴 변이 박스 긴 변과 나란하도록 끝점(TCP) 목표를 만든다.
        // 지금 뚜껑이 끝점에 대해 어디에 어떤 자세로 붙어 있는지(위치 오차, 기울어짐)를 재서 그만큼 보정한다.
        // 팔 상태를 읽을 수 없으면 뚜껑이 끝점에 정확히 있다고 보고 계산한다.
        private bool TryPlanCover(Transform lid, Vector3 boxLongAxis, Vector3 lidCenterTarget, out Quaternion toolRotation, out Vector3 toolPosition)
        {
            bool measured = _state != null && _state.IsReady;
            Vector3 heldLocal = Vector3.zero;
            Quaternion heldRotation = Quaternion.identity;
            if (measured)
            {
                Pose tool = _state.ToolPose;
                Quaternion inverse = Quaternion.Inverse(tool.rotation);
                heldLocal = inverse * (lid.position - tool.position);
                heldRotation = inverse * lid.rotation;
            }

            // 뚜껑 긴 변은 앞뒤가 같은 모양이다. 쥔 뚜껑의 긴 변에 가까운 쪽을 먼저 써서 손목이 덜 돌게 한다.
            foreach (Vector3 axis in LidCoverPlanner.CandidateAxes(boxLongAxis, lid.forward))
            {
                Quaternion rotation = measured
                    ? Quaternion.LookRotation(axis, Vector3.up) * Quaternion.Inverse(heldRotation)
                    : LidCoverPlanner.ToolRotation(axis);
                Vector3 position = lidCenterTarget - rotation * heldLocal;

                if (_tool.Probe(new Pose(position + Vector3.up * approachHeight, rotation)).Converged)
                {
                    toolRotation = rotation;
                    toolPosition = position;
                    return true;
                }
            }

            toolRotation = Quaternion.identity;
            toolPosition = lidCenterTarget;
            return false;
        }

        // 뚜껑 위에서 방향 후보를 앞에서부터 IK로 확인해 닿는 첫 방향을 쓴다.
        private bool TryChooseRotation(Vector3 position, Vector3 longAxis, Vector3 preferred, out Quaternion rotation)
        {
            foreach (Vector3 axis in LidCoverPlanner.CandidateAxes(longAxis, preferred))
            {
                Quaternion candidate = LidCoverPlanner.ToolRotation(axis);
                if (_tool.Probe(new Pose(position, candidate)).Converged)
                {
                    rotation = candidate;
                    return true;
                }
            }

            rotation = Quaternion.identity;
            return false;
        }

        // 같은 방향을 유지한 채 from에서 to까지 linearStepM씩 끊어서 이동한다. 어느 한 걸음이라도 닿지 못하면 거기서 멈춘다.
        private IEnumerator MoveLinear(Vector3 from, Vector3 to, Quaternion rotation, float positionToleranceM, float rotationToleranceDeg, Precision precision = Precision.Normal)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / linearStepM));
            for (int i = 1; i <= steps; i++)
            {
                Vector3 waypoint = Vector3.Lerp(from, to, (float)i / steps);
                yield return MoveTool(new Pose(waypoint, rotation), positionToleranceM, rotationToleranceDeg, precision);
                if (!_stepReached)
                {
                    yield break;
                }
            }
        }

        // 손가락 관절의 구동 힘 한계를 낮춘다. 힘이 너무 크면 패드가 얇은 뚜껑 벽을 뚫고 들어가 그리퍼가 열리지 않는다.
        // 구동 설정이 적용된 뒤여야 하므로 Awake가 아니라 쥐기 직전에 한다. 팔 관절은 건드리지 않는다.
        private void ApplyFingerForceLimit()
        {
            if (fingerForceLimit <= 0f)
            {
                return;
            }

            foreach (ArticulationBody body in toolTarget.GetComponentsInChildren<ArticulationBody>())
            {
                bool isFinger = body.name.IndexOf("knuckle", StringComparison.OrdinalIgnoreCase) >= 0
                                || body.name.EndsWith("_finger", StringComparison.OrdinalIgnoreCase);
                if (!isFinger || body.jointType != ArticulationJointType.RevoluteJoint)
                {
                    continue;
                }

                ArticulationDrive drive = body.xDrive;
                drive.forceLimit = fingerForceLimit;
                body.xDrive = drive;
            }
        }

        // 팔의 실제 끝점 위치. 팔 상태를 읽을 수 없으면 fallback을 쓴다.
        private Vector3 CurrentToolPosition(Vector3 fallback)
        {
            return _state != null && _state.IsReady ? _state.ToolPose.position : fallback;
        }

        // ToolPoseMover와 같은 규칙(목표에 닿거나, 시간이 지나거나, IK가 실패하면 끝)이다. 결과는 _stepReached에 담는다.
        private IEnumerator MoveTool(Pose target, float positionToleranceM, float rotationToleranceDeg, Precision precision = Precision.Normal)
        {
            _stepReached = false;
            _stepNote = string.Empty;
            _tool.SetToolTarget(target);

            bool correcting = precision != Precision.Normal && correctionGain > 0f && _state != null && _state.IsReady;
            Vector3 correction = Vector3.zero;
            float sinceCorrection = 0f;
            float elapsed = 0f;
            while (elapsed < stepTimeoutSeconds)
            {
                if (HasReached(target, positionToleranceM, rotationToleranceDeg, precision))
                {
                    _stepReached = true;
                    yield break;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                sinceCorrection += Time.fixedDeltaTime;

                // 팔이 먼저 목표 쪽으로 움직인 뒤, 일정 간격마다 남은 오차만큼 반대로 목표를 옮긴다(적분 보정).
                if (correcting && sinceCorrection >= correctionIntervalSeconds)
                {
                    sinceCorrection = 0f;
                    Vector3 error = _state.ToolPose.position - target.position;
                    correction = Vector3.ClampMagnitude(correction - error * correctionGain, maxCorrectionM);
                    _tool.SetToolTarget(new Pose(target.position + correction, target.rotation));
                }

                if (!_tool.LastSolveSucceeded)
                {
                    _stepNote = (precision == Precision.Grasp ? PadClearanceNote() : string.Empty) + $"IK가 닿는 해를 찾지 못함 (IK 위치 오차 {_tool.LastResult.PositionError:0.0000}m, 회전 오차 {_tool.LastResult.RotationErrorDeg:0.0}도, 목표 {target.position}){ActualPoseNote(target)}";
                    yield break;
                }
            }

            _stepNote = (precision == Precision.Grasp ? PadClearanceNote() : string.Empty) + $"{stepTimeoutSeconds:0.#}초 안에 허용 오차({ToleranceText(positionToleranceM, precision)}, {RotationText(rotationToleranceDeg, precision)})에 들지 못함 (IK 위치 오차 {_tool.LastResult.PositionError:0.0000}m, 회전 오차 {_tool.LastResult.RotationErrorDeg:0.0}도, 목표 {target.position}){ActualPoseNote(target)}";
        }

        // 팔 끝점의 실제 위치와 목표의 차이. 팔이 어디서 막혔는지 가늠할 때 쓴다.
        private string ActualPoseNote(Pose target)
        {
            if (_state == null || !_state.IsReady)
            {
                return string.Empty;
            }

            Pose actual = _state.ToolPose;
            Vector3 diff = actual.position - target.position;
            return $" 실제 끝점 {actual.position}, 목표와의 차이 x {diff.x * 1000f:0}mm y {diff.y * 1000f:0}mm z {diff.z * 1000f:0}mm (거리 {diff.magnitude * 1000f:0}mm), 회전 차이 {Quaternion.Angle(actual.rotation, target.rotation):0.0}도(수평 방향 {LidCoverPlanner.YawErrorDeg(actual.rotation, target.rotation):0.0}도, 기울기 {LidCoverPlanner.TiltErrorDeg(actual.rotation, target.rotation):0.0}도){JointNote()}{BlockerNote()}";
        }

        // 팔이 멈춘 자리에서 팔(과 쥔 뚜껑) 곁에 붙은 다른 충돌체. 관절이 목표로 안 가고 서 있을 때 무엇에 걸렸는지 보려는 것이다.
        private string BlockerNote()
        {
            // 이 컴포넌트는 팔의 관절 몸체보다 위에 있으므로 아래로 내려가며 팔의 충돌체를 모은다.
            var movers = new List<Collider>(toolTarget.GetComponentsInChildren<Collider>());
            if (movers.Count == 0)
            {
                return string.Empty;
            }

            var ignore = new HashSet<Collider>(movers);
            if (_heldLid != null)
            {
                foreach (Collider held in _heldLid.GetComponentsInChildren<Collider>())
                {
                    movers.Add(held);
                    ignore.Add(held);
                }
            }

            return ContactBlockerFinder.Describe(movers, ignore, BlockerMarginM);
        }

        // 손가락 패드와 맨 위 뚜껑 옆면 사이 간격(닫히는 방향, mm). 음수면 겹쳐 있다. 어느 쪽 패드가 뚜껑에 붙었는지 가린다.
        // 패드 하단이 뚜껑 윗면보다 얼마나 아래로 내려왔는지도 함께 적는다.
        private string PadClearanceNote()
        {
            Transform lid = dispenser.TopLid;
            if (_state == null || !_state.IsReady || lid == null)
            {
                return string.Empty;
            }

            Pose tool = _state.ToolPose;
            Vector3 closeAxis = tool.rotation * Vector3.right;
            Vector3 downAxis = tool.rotation * Vector3.forward;

            if (!TryProject(lid.GetComponentsInChildren<Collider>(), closeAxis, out float lidMin, out float lidMax)
                || !TryProject(lid.GetComponentsInChildren<Collider>(), downAxis, out float lidTop, out _))
            {
                return string.Empty;
            }

            var negative = new List<Collider>();
            var positive = new List<Collider>();
            foreach (Collider collider in toolTarget.GetComponentsInChildren<Collider>())
            {
                if (!IsPad(collider)) continue;
                float side = Vector3.Dot(collider.bounds.center - tool.position, closeAxis);
                (side < 0f ? negative : positive).Add(collider);
            }

            if (negative.Count == 0 || positive.Count == 0
                || !TryProject(negative, closeAxis, out _, out float negativeInner)
                || !TryProject(positive, closeAxis, out float positiveInner, out _)
                || !TryProject(positive, downAxis, out _, out float padBottom))
            {
                return string.Empty;
            }

            return $"패드-뚜껑 옆 간격(음수면 겹침): 닫히는 방향 -쪽 {(lidMin - negativeInner) * 1000f:0.0}mm, +쪽 {(positiveInner - lidMax) * 1000f:0.0}mm, 패드 하단이 뚜껑 윗면보다 {(padBottom - lidTop) * 1000f:0.0}mm 아래\n";
        }

        // 손가락 패드 콜라이더인지. 부모 중에 이름에 inner_finger_pad가 들어간 것이 있으면 패드로 본다.
        private bool IsPad(Collider collider)
        {
            for (Transform t = collider.transform; t != null && t != toolTarget.transform; t = t.parent)
            {
                if (t.name.IndexOf("inner_finger_pad", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        // 콜라이더 모서리를 axis에 투영한 최소·최대. 박스 콜라이더는 회전을 반영한 모서리를, 그 밖에는 경계 상자를 쓴다.
        private static bool TryProject(IEnumerable<Collider> colliders, Vector3 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            bool any = false;
            foreach (Collider collider in colliders)
            {
                foreach (Vector3 corner in Corners(collider))
                {
                    float value = Vector3.Dot(corner, axis);
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                    any = true;
                }
            }

            return any;
        }

        private static IEnumerable<Vector3> Corners(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                Vector3 half = box.size * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    var local = new Vector3((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y, (i & 4) == 0 ? -half.z : half.z);
                    yield return box.transform.TransformPoint(box.center + local);
                }
            }
            else
            {
                Bounds bounds = collider.bounds;
                for (int i = 0; i < 8; i++)
                {
                    yield return bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                }
            }
        }

        // 관절별 현재 각도, IK가 준 목표 각도, 한계(도). 관절이 목표에 못 간 것인지(구동·한계 문제),
        // 목표에는 갔는데 끝점 위치가 다른 것인지(운동학 모델 오차)를 가른다.
        private string JointNote()
        {
            if (!(toolTarget is IArmTarget armTarget))
            {
                return string.Empty;
            }

            int count = _state.JointNames.Count;
            var current = new float[count];
            var goal = new float[count];
            var lower = new float[count];
            var upper = new float[count];
            _state.GetJointPositions(current);
            _state.GetJointLimits(lower, upper);
            if (!armTarget.TryGetTargets(goal))
            {
                return string.Empty;
            }

            var text = new System.Text.StringBuilder("\n관절(도): ");
            for (int i = 0; i < count; i++)
            {
                float now = current[i] * Mathf.Rad2Deg;
                float want = goal[i] * Mathf.Rad2Deg;
                text.Append($"\n  {_state.JointNames[i]} 현재 {now:0.0} 목표 {want:0.0} 차이 {now - want:0.0} 한계 [{lower[i] * Mathf.Rad2Deg:0}, {upper[i] * Mathf.Rad2Deg:0}]");
            }

            return text.ToString();
        }

        // 팔의 실제 끝점이 목표에 닿았는지. Normal이 아니거나 팔 상태를 읽을 수 없으면 Normal과 같게 본다.
        private bool HasReached(Pose target, float positionToleranceM, float rotationToleranceDeg, Precision precision)
        {
            if (precision == Precision.Normal || _state == null || !_state.IsReady)
            {
                return _tool.HasReachedTarget(positionToleranceM, rotationToleranceDeg);
            }

            Pose actual = _state.ToolPose;
            if (!RotationReached(actual.rotation, target.rotation, rotationToleranceDeg, precision))
            {
                return false;
            }

            Vector3 error = actual.position - target.position;
            if (precision == Precision.Grasp)
            {
                // 손가락이 닫히는 방향이 도구 X이다. 그 방향만 엄격하게 본다.
                Vector3 closeAxis = target.rotation * Vector3.right;
                float across = Vector3.Dot(error, closeAxis);
                float rest = (error - closeAxis * across).magnitude;
                return Mathf.Abs(across) <= positionToleranceM && rest <= looseToleranceM;
            }

            // Cover: 가로 두 방향을 엄격하게, 높이는 느슨하게 본다.
            float horizontal = new Vector2(error.x, error.z).magnitude;
            return horizontal <= positionToleranceM && Mathf.Abs(error.y) <= looseToleranceM;
        }

        // Cover는 수평 방향(박스 긴 변과의 어긋남)을 preciseRotationToleranceDeg로, 기울기를 rotationToleranceDeg로 따로 본다.
        // 그 밖에는 두 회전 사이 각도 하나로 본다.
        private bool RotationReached(Quaternion actual, Quaternion target, float rotationToleranceDeg, Precision precision)
        {
            if (precision == Precision.Cover)
            {
                return LidCoverPlanner.YawErrorDeg(actual, target) <= preciseRotationToleranceDeg
                       && LidCoverPlanner.TiltErrorDeg(actual, target) <= rotationToleranceDeg;
            }

            return Quaternion.Angle(actual, target) <= rotationToleranceDeg;
        }

        private string RotationText(float rotationToleranceDeg, Precision precision)
        {
            return precision == Precision.Cover
                ? $"수평 방향 {preciseRotationToleranceDeg:0.#}도, 기울기 {rotationToleranceDeg:0.#}도"
                : $"회전 {rotationToleranceDeg:0.#}도";
        }

        private string ToleranceText(float positionToleranceM, Precision precision)
        {
            switch (precision)
            {
                case Precision.Grasp:
                    return $"닫히는 방향 {positionToleranceM * 1000f:0.#}mm, 그 외 {looseToleranceM * 1000f:0.#}mm";
                case Precision.Cover:
                    return $"가로 {positionToleranceM * 1000f:0.#}mm, 높이 {looseToleranceM * 1000f:0.#}mm";
                default:
                    return $"위치 {positionToleranceM * 1000f:0.#}mm";
            }
        }

        private IEnumerator WaitForGrasp()
        {
            float elapsed = 0f;
            while (!_grip.IsHolding && elapsed < graspTimeoutSeconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }

        // 실패해서 쥔 뚜껑을 놓을 때. 떨어지게 두고 새 뚜껑을 올린다.
        private void DropLid(Rigidbody body)
        {
            _grip.Open();
            body.useGravity = true;
        }

        private void Fail(string reason)
        {
            string detail = string.IsNullOrEmpty(_stepNote) ? string.Empty : $" [{_stepNote}]";
            Debug.LogWarning($"[LidCoverTask] 뚜껑 덮기 실패: {reason}{detail}", this);
            _stepNote = string.Empty;
            _heldLid = null;
            _tool.ClearToolTarget();
            _station.Set(StationState.Fault);
            _running = null;
            Finished?.Invoke(false);
        }

        private void Finish(bool success)
        {
            _station.Set(StationState.Idle);
            _heldLid = null;
            _running = null;
            Finished?.Invoke(success);
        }

        // root 아래에서 이름에 KitBox가 들어간 첫 오브젝트. 찾은 오브젝트의 자식은 더 보지 않는다.
        private static Transform FindKitBox(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (child.name.IndexOf(CellObjectNames.KitBoxKey, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }

                Transform nested = FindKitBox(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        // 박스 윗면 높이. 박스의 벽과 바닥 렌더러를 모두 감싼 월드 최고 높이이다.
        private static float TopOf(Transform box)
        {
            float top = box.position.y;
            foreach (Renderer renderer in box.GetComponentsInChildren<Renderer>())
            {
                top = Mathf.Max(top, renderer.bounds.max.y);
            }

            return top;
        }
    }
}
