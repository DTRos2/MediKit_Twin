#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Demo;
using MediKitTwin.Orders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using static MediKitTwin.PlayMode.Tests.ProcessPanelReader;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 공정이 끝나 "공정 완료"가 뜬 뒤 주문을 다시 적용하면, 공정 현황 패널이 처음 상태로 돌아가 새 주문을 처음부터 따라가고
    /// 센 값(완료 수)은 지우지 않고 이어서 세는지 본다. 주문을 다시 불러오면 키트 순번이 1부터 다시 시작하므로,
    /// 두 번째 주문의 키트도 1번이다. 순번으로 키트를 구분하면 두 번째 키트가 이미 센 키트로 보여 화면이 "공정 완료"에서 멈춘다.
    /// 키트는 1개짜리 주문으로 두 번 돌린다(누락 규칙은 3번 키트만 빠뜨리므로 둘 다 정상 키트다).
    /// 하역 로봇이 아직 없어서 컨베이어 끝에 닿은 박스는 EndOfLineUnloaderStub이 치운다.
    /// </summary>
    public sealed class ReapplyOrderProcessTests
    {
        private const string OneKitOrder = "1,시험병원,EM,1";

        // 시간은 시뮬레이션 시간(초)이다. 키트 1개를 만들어 컨베이어 끝까지 보내는 데 한 번에 걸리는 시간보다 넉넉하게 둔다.
        // 실제 시간 제한은 배치 모드에서 막혔을 때 빨리 끝내려는 안전장치다.
        private const float SimTimeoutSeconds = 600f;
        private const double WallTimeoutSeconds = 900d;
        private const int TestTimeoutMilliseconds = 2400000;

        // 하역 대역이 끝에 닿은 박스를 치우기 전에 두는 시간.
        private const float UnloaderHoldSeconds = 2f;

        // 하역 대역이 박스를 치운 것을 확인하기 위해 보태는 시뮬레이션 시간(초).
        private const float ShipMarginSeconds = 1f;

        // 주문을 적용한 뒤 패널이 다시 그려질 때까지 기다리는 프레임 수. 키트가 처음 나오기까지는 한참 걸리므로 이 사이에 "공정 완료"가 되살아날 수 없다.
        private const int RedrawFrames = 3;

        private const int FirstKitSequence = 1;

        [UnityTest, Timeout(TestTimeoutMilliseconds)]
        public IEnumerator AfterTheProcessIsDone_ApplyingTheOrderAgain_StartsOverAndTheCountsKeepAddingUp()
        {
            LogAssert.ignoreFailingMessages = true;
            FrameTimingSettings timing = FrameTimingSettings.FromEnvironment();
            Time.captureFramerate = timing.CaptureFramerate;

            using (var events = new ConveyorEventRecorder())
            {
                try
                {
                    yield return CellTestScene.Load();

                    IKittingStation station = CellTestScene.FindOne<IKittingStation>();
                    IOrderInput input = CellTestScene.FindOne<IOrderInput>();
                    var presenter = Object.FindAnyObjectByType<ProcessStatusPanelPresenter>();
                    Assert.That(station, Is.Not.Null, "씬에서 키팅 설비(IKittingStation)를 찾을 수 없다");
                    Assert.That(input, Is.Not.Null, "씬에서 주문 입력(IOrderInput)을 찾을 수 없다");
                    Assert.That(presenter, Is.Not.Null, "씬에 ProcessStatusPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");

                    var rig = new Rig(presenter, station, new EndOfLineUnloaderStub(UnloaderHoldSeconds), events);
                    IProcessStatus status = presenter.Status;

                    // 첫 번째 적용: 끝까지 간다.
                    input.LoadText(OneKitOrder);
                    var first = new RunOutcome();
                    yield return rig.RunUntilDone(expectedCompleted: 1, first);

                    Assert.That(first.Reason, Is.EqualTo(RunOutcome.Finished), "첫 주문이 끝까지 가지 못했다\n" + Describe(status));
                    Assert.That(status.CompletedCount, Is.EqualTo(1), Describe(status));
                    Assert.That(status.CurrentKit.KitSequence, Is.EqualTo(FirstKitSequence), Describe(status));
                    KitData firstKit = status.CurrentKit;
                    for (int i = 0; i < RedrawFrames; i++)
                    {
                        yield return null;
                    }

                    Assert.That(TextOf(presenter, "Headline"), Is.EqualTo(ProcessStatusText.Stage(ProcessStage.Done)), "첫 주문이 끝나면 화면에 \"공정 완료\"가 떠야 한다");
                    Assert.That(TileValue(presenter, "Completed"), Is.EqualTo("1"));

                    // 같은 주문을 다시 적용: 화면이 처음 상태로 돌아가야 하고, 센 값은 그대로여야 한다.
                    input.LoadText(OneKitOrder);
                    for (int i = 0; i < RedrawFrames; i++)
                    {
                        yield return null;
                    }

                    Assert.That(status.CurrentKit, Is.Null, "다시 적용하면 지난 주문의 키트가 화면에 남아 있으면 안 된다\n" + Describe(status));
                    Assert.That(status.CurrentStage, Is.EqualTo(ProcessStage.None), Describe(status));
                    Assert.That(TextOf(presenter, "Headline"), Is.Not.EqualTo(ProcessStatusText.Stage(ProcessStage.Done)), "다시 적용했는데 화면이 \"공정 완료\"에 머물러 있다");
                    Assert.That(status.CompletedCount, Is.EqualTo(1), "센 값은 지우지 않고 이어서 센다\n" + Describe(status));
                    Assert.That(TileValue(presenter, "Completed"), Is.EqualTo("1"));

                    // 두 번째 적용도 처음부터 끝까지 간다. 키트 순번이 다시 1이어도 새 키트로 센다.
                    var second = new RunOutcome();
                    yield return rig.RunUntilDone(expectedCompleted: 2, second);
                    Debug.Log($"[Reapply] 두 번째 적용 결과: {second.Reason} — {Describe(status)}");

                    Assert.That(second.Reason, Is.EqualTo(RunOutcome.Finished), "다시 적용한 주문이 끝까지 가지 못했다\n" + Describe(status));
                    Assert.That(status.CompletedCount, Is.EqualTo(2), "다시 적용한 주문의 키트가 완료 수에 더해지지 않았다\n" + Describe(status));
                    Assert.That(status.CurrentKit, Is.Not.SameAs(firstKit), "화면이 첫 주문의 키트에 머물러 있다\n" + Describe(status));
                    Assert.That(status.CurrentKit.KitSequence, Is.EqualTo(FirstKitSequence), "전제: 주문을 다시 불러오면 키트 순번이 1부터 다시 시작한다\n" + Describe(status));
                    Assert.That(status.CurrentStage, Is.EqualTo(ProcessStage.Done), Describe(status));
                    Assert.That(status.RejectedCount, Is.EqualTo(0), Describe(status));

                    // 하역 대역은 끝에 닿고 UnloaderHoldSeconds가 지나야 박스를 치우고 KitShipped를 보낸다. 그 신호까지 와도 같은 키트를 두 번 세지 않아야 한다.
                    yield return rig.TickFor(UnloaderHoldSeconds + ShipMarginSeconds);
                    Assert.That(rig.Unloader.ShippedCount, Is.EqualTo(2), "끝에 닿은 키트를 둘 다 하역하지 못했다");
                    Assert.That(status.CompletedCount, Is.EqualTo(2), "하역 신호가 와도 같은 키트를 다시 세면 안 된다\n" + Describe(status));

                    for (int i = 0; i < RedrawFrames; i++)
                    {
                        yield return null;
                    }

                    Assert.That(TextOf(presenter, "Headline"), Is.EqualTo(ProcessStatusText.Stage(ProcessStage.Done)));
                    Assert.That(TileValue(presenter, "Completed"), Is.EqualTo("2"));
                }
                finally
                {
                    Time.captureFramerate = 0;
                }
            }
        }

        // 그래픽이 없는 배치 모드에서는 도구 카메라가 화면을 그릴 때마다 RenderTexture 오류를 남긴다. 끝날 때 그리기를 멈춘다.
        [UnityTearDown]
        public IEnumerator StopToolCameraFeeds()
        {
            return CellTestScene.StopToolCameraFeeds();
        }

        private static string Describe(IProcessStatus status)
        {
            return $"[공정 현황] 완료 {status.CompletedCount}, 불량 {status.RejectedCount}, 단계 {status.CurrentStage}, "
                   + $"지금 키트 {(status.CurrentKit != null ? $"#{status.CurrentKit.KitSequence} {status.CurrentKit.LotNumber}" : "없음")}";
        }

        // 공정이 도는 동안 시험이 매 프레임 해야 할 일(하역 대역 돌리기, 멈춤 확인)을 한 곳에 모은다.
        private sealed class Rig
        {
            private readonly ProcessStatusPanelPresenter _presenter;
            private readonly IKittingStation _station;
            private readonly ConveyorEventRecorder _events;
            private readonly int _faultsBefore;

            public Rig(ProcessStatusPanelPresenter presenter, IKittingStation station, EndOfLineUnloaderStub unloader, ConveyorEventRecorder events)
            {
                _presenter = presenter;
                _station = station;
                Unloader = unloader;
                _events = events;
                _faultsBefore = station.Status.FaultCount;
            }

            public EndOfLineUnloaderStub Unloader { get; }

            // 하역 대역만 돌리며 시뮬레이션 시간으로 seconds만큼 기다린다.
            public IEnumerator TickFor(float seconds)
            {
                float until = Time.time + seconds;
                while (Time.time < until)
                {
                    Unloader.Tick(_events);
                    yield return null;
                }
            }

            // 센 완료 수가 expectedCompleted에 이르고 화면의 단계가 "공정 완료"가 될 때까지 돈다. 이유는 outcome에 남긴다.
            public IEnumerator RunUntilDone(int expectedCompleted, RunOutcome outcome)
            {
                var wall = Stopwatch.StartNew();
                float startSim = Time.time;
                while (true)
                {
                    Unloader.Tick(_events);
                    IProcessStatus status = _presenter.Status;
                    if (status.CompletedCount >= expectedCompleted && status.CurrentStage == ProcessStage.Done)
                    {
                        outcome.Reason = RunOutcome.Finished;
                        yield break;
                    }

                    if (_station.Status.FaultCount > _faultsBefore)
                    {
                        outcome.Reason = "키팅 오류로 중단";
                        yield break;
                    }

                    if (Time.time - startSim > SimTimeoutSeconds || wall.Elapsed.TotalSeconds > WallTimeoutSeconds)
                    {
                        yield break;
                    }

                    yield return null;
                }
            }
        }

        private sealed class RunOutcome
        {
            public const string Finished = "공정 끝";

            public string Reason { get; set; } = "시간 초과";
        }
    }
}
#endif
