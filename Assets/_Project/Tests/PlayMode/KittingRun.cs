using System.Collections;
using System.Diagnostics;
using MediKitTwin.ArmA;
using MediKitTwin.Orders;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 주문을 넣고 키트가 정해진 수만큼 끝나거나, 오류로 멈추거나, 시간이 다 될 때까지 지켜보는 한 번의 실행.
    /// 끝난 뒤 결과를 글로 정리한다. 키팅에는 주문을 넣는 것 말고는 손대지 않는다(읽기만 한다).
    /// 시험마다 같은 순서를 다시 쓰지 않도록 따로 뺐다.
    /// </summary>
    public sealed class KittingRun
    {
        public const string Done = "키트 완료";
        public const string Faulted = "오류로 중단";
        public const string TimedOut = "시간 초과";

        // 변화가 있을 때만 적으므로 실행 전체가 들어가도 수백 줄이다. 넉넉히 남긴다.
        private const int RecentTraceLines = 1500;

        private readonly IKittingStation _station;
        private readonly IOrderInput _input;
        private readonly ArmJointDump _jointDump;
        private readonly ArmHomeGapDump _homeGapDump;
        private readonly JointTraceRecorder _firstJointTrace;
        private readonly ArmOverlapProbe _overlapProbe;
        private readonly StageBaseLog _stageLog;
        private readonly ArmPoseSnapshot _poseSnapshot;
        private readonly ToolPathRecorder _toolPath;
        private readonly GraspHoldRecorder _graspHold;
        private readonly string _boxName;
        private readonly float _simTimeoutSeconds;
        private readonly double _wallTimeoutSeconds;

        // 주문을 넣기 전에 팔이 팔이 아닌 충돌체와 이미 겹쳐 있었는지. 시작부터 겹쳤는지, 움직이다 겹쳤는지 가리는 데 쓴다.
        private string _overlapAtStart = string.Empty;

        public KittingRun(
            IKittingStation station, IOrderInput input, GameObject robot, IkArmTarget arm, string watchedCell, string boxName,
            float simTimeoutSeconds, double wallTimeoutSeconds)
        {
            _station = station;
            _input = input;
            // 팔 링크에 접촉 알림(OnCollision*)을 달면 PhysX 물리 단계에서 Unity가 죽으므로, 겹침은 멈춘 뒤 한 번 조회만 한다.
            _jointDump = new ArmJointDump(Object.FindObjectsByType<ArticulationBody>());
            var controller = robot.GetComponentInChildren<ArmController>();
            _homeGapDump = new ArmHomeGapDump(controller, StowToleranceRad());
            _firstJointTrace = new JointTraceRecorder(controller, 0, 0.25f, RecentTraceLines);
            _overlapProbe = new ArmOverlapProbe(robot.transform);
            _poseSnapshot = new ArmPoseSnapshot(arm, watchedCell);
            _toolPath = new ToolPathRecorder(arm, 0.25f);
            _graspHold = new GraspHoldRecorder(robot.GetComponentInChildren<JointGripper>(), controller);
            Recorder = new KittingProgressRecorder(station);
            _stageLog = new StageBaseLog(controller, _overlapProbe);
            Recorder.StageChanged += _stageLog.Record;
            _boxName = boxName;
            _simTimeoutSeconds = simTimeoutSeconds;
            _wallTimeoutSeconds = wallTimeoutSeconds;
        }

        public KittingProgressRecorder Recorder { get; }

        public PlacedItemSnapshot Placed { get; private set; }

        public string EndReason { get; private set; } = TimedOut;

        public int CompletedKits { get; private set; }

        /// <summary>끝난 뒤 정리한 결과. 키트를 다 끝냈으면 짧게, 아니면 어디서 막혔는지 볼 자료를 더 붙인다.</summary>
        public string Report { get; private set; } = string.Empty;

        /// <param name="kitsToFinish">끝나길 기다릴 키트 수.</param>
        public IEnumerator Execute(string orderCsv, int kitsToFinish)
        {
            int completedBefore = _station.Status.CompletedKitCount;
            int faultsBefore = _station.Status.FaultCount;

            // 주문을 넣기 전, 팔이 아직 움직이기 전의 겹침을 한 번 적어 둔다.
            _overlapAtStart = _overlapProbe.Describe();

            OrderIntakeResult intake = _input.LoadText(orderCsv);
            UnityEngine.Debug.Log($"[E2E] 주문 적용: 주문 {intake.Orders.Count}건, 키트 {intake.KitRequests.Count}개");

            var wall = Stopwatch.StartNew();
            float startSim = Time.time;

            while (true)
            {
                Recorder.Sample(Time.time - startSim);
                _toolPath.Sample(Time.time - startSim);
                _firstJointTrace.Sample(Time.time - startSim);
                _graspHold.Sample(Time.time - startSim);
                CompletedKits = _station.Status.CompletedKitCount - completedBefore;

                if (CompletedKits >= kitsToFinish)
                {
                    EndReason = Done;
                    break;
                }

                if (_station.Status.FaultCount > faultsBefore)
                {
                    EndReason = Faulted;
                    break;
                }

                if (Time.time - startSim > _simTimeoutSeconds || wall.Elapsed.TotalSeconds > _wallTimeoutSeconds)
                {
                    break;
                }

                yield return null;
            }

            // 마지막 단계(보통 마지막 구성품을 놓은 직후)는 단계 변화로 잡히기 전에 반복이 끝나므로, 끝난 순간을 따로 적는다.
            _stageLog.Record(Time.time - startSim, $"끝({EndReason})");
            Placed = new PlacedItemSnapshot(_boxName);
            Report = BuildReport(Time.time - startSim, wall.Elapsed.TotalSeconds, kitsToFinish);
            UnityEngine.Debug.Log(Report);

            // 쥐는 순간과 쥐고 있는 동안의 움직임은 길어서 환경 변수로 켰을 때만 따로 남긴다.
            if (GraspHoldRecorder.IsEnabled)
            {
                UnityEngine.Debug.Log($"[E2E] 쥐기 기록:\n{_graspHold.Lines}");
            }
        }

        // 팔 접기 허용 오차는 씬에 놓인 이동 설정에서 읽는다. 설정을 못 찾으면 에셋이 정한 기본값을 쓴다(값을 여기 베끼지 않는다).
        private static float StowToleranceRad()
        {
            ArmBaseApproachConfigSO[] configs = Resources.FindObjectsOfTypeAll<ArmBaseApproachConfigSO>();
            return configs.Length > 0 ? configs[0].StowToleranceRad : ArmBaseApproachConfigSO.DefaultStowToleranceRad;
        }

        private string BuildReport(float simSeconds, double wallSeconds, int kitsToFinish)
        {
            string common =
                $"[E2E] 결과: {EndReason} — 키트 {CompletedKits}/{kitsToFinish}개 (시뮬레이션 {simSeconds:0.0}초, 실제 {wallSeconds:0.0}초)\n" +
                $"[E2E] 마지막 결과: {_station.Status.LastResult} {_station.Status.LastMessage}\n" +
                $"[E2E] 진행 기록:\n{Recorder.Timeline}" +
                $"[E2E] 단계별 팔 베이스 위치와 파고든 곳:\n{_stageLog.Lines}" +
                $"[E2E] 첫째 관절 움직임과 팔 베이스 위치(변화가 있을 때만):\n{_firstJointTrace.Lines}" +
                $"[E2E] 박스에 놓인 물건 {Placed.PlacedCount}개 (키트 계획의 구성품 {Recorder.LargestItemCount}개):\n{Placed.Describe()}";

            // 길 기록은 길어서 실패했을 때만 붙인다.
            return EndReason == Done
                ? common
                : common +
                  $"[E2E] 끝날 때 관절 상태:\n{_jointDump.Describe()}" +
                  $"[E2E] 끝날 때 홈 자세와의 차이:\n{_homeGapDump.Describe()}" +
                  $"[E2E] 시작할 때 팔이 겹친 것들:\n{_overlapAtStart}" +
                  $"[E2E] 도구 끝점이 지나간 길:\n{_toolPath.Lines}" +
                  $"[E2E] 도구 끝점과 A-1 칸 물건:\n{_poseSnapshot.Describe()}" +
                  $"[E2E] 팔이 겹친 것들:\n{_overlapProbe.Describe()}";
        }
    }
}
