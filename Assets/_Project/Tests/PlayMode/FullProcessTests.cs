#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.Linq;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using MediKitTwin.Label;
using MediKitTwin.Orders;
using MediKitTwin.Pusher;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PusherStation = MediKitTwin.Pusher.Pusher;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 한 공정을 처음부터 끝까지 한 번에 본다: 주문 → 팔 A 키팅 → 컨베이어 → 검사 → 정상은 팩킹(팔 B 뚜껑) → 컨베이어 끝, 불량은 푸셔 → 불량 박스.
    /// 주문은 키트 3개다. 누락 규칙(FR-04)이 3번 키트에서만 구성품 하나를 빠뜨리도록 시나리오에 정해져 있으므로,
    /// 1·2번은 정상(2개 성공), 3번은 불량(1개 불량)이 나와야 한다. 키트는 검사·푸셔·팩킹으로 가는 길이 서로 다르므로 키트별로 지나간 알림과 그 순서를 본다.
    /// 하역 로봇이 아직 없어서 컨베이어 끝에 닿은 박스는 EndOfLineUnloaderStub이 치운다.
    /// 배치 모드(그래픽 없음)에서는 검사 카메라를 그릴 수 없어 검사는 슬롯 센서 규칙 판정으로 대체된다. CNN 판정은 이 시험이 보지 않는다.
    /// </summary>
    public sealed class FullProcessTests
    {
        private const string ThreeKitOrder = "1,시험병원,EM,3";
        private const int KitCount = 3;
        private const int GoodKitCount = 2;
        private const int DefectiveKitSequence = 3;
        private const string OutsideWarningCode = "PUSHER_KIT_OUTSIDE";

        // 정상으로 나와야 하는 키트 번호들: 불량 하나를 뺀 앞의 번호들(1부터).
        private static readonly int[] GoodKitSequences = Enumerable.Range(1, GoodKitCount).ToArray();

        // 시간은 시뮬레이션 시간(초)이다. 키팅 약 80초 × 3, 뚜껑 덮기와 컨베이어 이동이 더해진다. 실제 시간 제한은 배치 모드에서 막혔을 때 빨리 끝내려는 안전장치다.
        private const float SimTimeoutSeconds = 1500f;
        private const double WallTimeoutSeconds = 2100d;
        private const int TestTimeoutMilliseconds = 2700000;
        // 하역 대역이 끝에 닿은 박스를 치우기 전에 두는 시간. 끝 알림이 박스가 선 뒤에 나오는지 눈으로 볼 만큼만 둔다.
        private const float UnloaderHoldSeconds = 2f;
        // 모든 키트가 끝난 뒤에도 더 나오는 알림이 없는지 보는 시간.
        private const float QuietSeconds = 5f;

        [UnityTest, Timeout(TestTimeoutMilliseconds)]
        public IEnumerator TwoGoodKitsAreShipped_OneDefectiveKitIsRejected()
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
                    var rejectBin = Object.FindAnyObjectByType<RejectBin>();
                    Assert.That(station, Is.Not.Null, "씬에서 키팅 설비(IKittingStation)를 찾을 수 없다");
                    Assert.That(input, Is.Not.Null, "씬에서 주문 입력(IOrderInput)을 찾을 수 없다");
                    Assert.That(rejectBin, Is.Not.Null, "씬에서 불량 박스(RejectBin)를 찾을 수 없다");
                    Assert.That(Object.FindAnyObjectByType<InspectionConveyorLine>(), Is.Not.Null, "씬에서 검사 구간 제어(InspectionConveyorLine)를 찾을 수 없다");

                    int faultsBefore = station.Status.FaultCount;
                    int rejectedBefore = rejectBin.Total;
                    var unloader = new EndOfLineUnloaderStub(UnloaderHoldSeconds);
                    var trace = new PushedBoxTrace(rejectBin, Object.FindAnyObjectByType<PusherStation>());
                    var labels = new LabelRecorder();
                    var labelStation = Object.FindAnyObjectByType<LabelStation>();
                    Assert.That(labelStation, Is.Not.Null, "씬에서 라벨 설비(LabelStation)를 찾을 수 없다(Tools/MediKit/Setup Label Station으로 놓는다)");

                    OrderIntakeResult intake = input.LoadText(ThreeKitOrder);
                    Debug.Log($"[FullProcess] 주문 적용: 주문 {intake.Orders.Count}건, 키트 {intake.KitRequests.Count}개");

                    var wall = Stopwatch.StartNew();
                    float startSim = Time.time;
                    string endReason = "시간 초과";
                    while (true)
                    {
                        unloader.Tick(events);
                        trace.Sample(Time.time);
                        labels.Sample(Time.time);
                        if (IsProcessFinished(events))
                        {
                            endReason = "공정 끝";
                            break;
                        }

                        if (station.Status.FaultCount > faultsBefore)
                        {
                            endReason = "키팅 오류로 중단";
                            break;
                        }

                        if (Time.time - startSim > SimTimeoutSeconds || wall.Elapsed.TotalSeconds > WallTimeoutSeconds)
                        {
                            break;
                        }

                        yield return null;
                    }

                    string report = BuildReport(endReason, Time.time - startSim, wall.Elapsed.TotalSeconds, events, station, trace, labels);
                    Debug.Log(report);

                    // 더 나오는 알림이 없는지(예: 불량 키트가 팩킹으로도 가는지) 보려고 잠시 더 돌린다.
                    float quietUntil = Time.time + QuietSeconds;
                    while (Time.time < quietUntil)
                    {
                        unloader.Tick(events);
                        yield return null;
                    }

                    Assert.That(endReason, Is.EqualTo("공정 끝"), report);
                    AssertKitsWereMadeAsPlanned(events, report);
                    AssertGoodKitsWentThroughPacking(events, report);
                    AssertDefectiveKitWasRejected(events, rejectBin, rejectedBefore, report);
                    AssertLabelsWereAttached(events, labels, labelStation, report);
                    Assert.That(unloader.ShippedCount, Is.EqualTo(GoodKitCount), "끝에 닿은 정상 키트를 모두 하역하지 못했다\n" + report);
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

        // 정상 키트는 모두 끝에 닿았고 불량 키트는 불량 박스에 들어갔으면 공정이 끝난 것이다.
        private static bool IsProcessFinished(ConveyorEventRecorder events)
        {
            return events.KitsWith(FlowStep.AtConveyorEnd).Count >= GoodKitCount
                   && events.KitsWith(FlowStep.KitRejected).Count >= 1;
        }

        // 키트 3개가 만들어졌고 누락 규칙대로 3번만 구성품이 빠졌으며, 검사가 그것을 맞게 가려냈는지.
        private static void AssertKitsWereMadeAsPlanned(ConveyorEventRecorder events, string report)
        {
            var ready = events.Timeline.Where(e => e.Step == FlowStep.KitReady).Select(e => e.Kit).OrderBy(k => k.KitSequence).ToList();
            Assert.That(ready.Select(k => k.KitSequence), Is.EqualTo(Enumerable.Range(1, KitCount)), "키트 3개가 순서대로 완성되지 않았다\n" + report);

            // 전제: 누락 규칙이 3번 키트만 빠뜨린다. 시드나 확률이 바뀌어 1·2번이 빠지면 "2개 성공" 시나리오가 아니므로 여기서 알린다.
            foreach (KitData kit in ready)
            {
                bool shouldBeMissing = kit.KitSequence == DefectiveKitSequence;
                Assert.That(
                    kit.GetMissingAnswerForEvaluation().HasValue,
                    Is.EqualTo(shouldBeMissing),
                    $"키트 {kit.KitSequence}의 누락 여부가 시나리오와 다르다(누락 슬롯: {kit.GetMissingAnswerForEvaluation()?.ToString() ?? "없음"})\n{report}");
            }

            foreach (KitData kit in ready)
            {
                Assert.That(events.IndexOf(kit.KitSequence, FlowStep.AtInspection), Is.GreaterThan(events.IndexOf(kit.KitSequence, FlowStep.KitReady)), $"키트 {kit.KitSequence}가 검사 자리에 오지 않았다\n{report}");
                Assert.That(events.IndexOf(kit.KitSequence, FlowStep.InspectionResult), Is.GreaterThan(events.IndexOf(kit.KitSequence, FlowStep.AtInspection)), $"키트 {kit.KitSequence}의 검사 결과가 나오지 않았다\n{report}");
                Assert.That(kit.Verdict.HasValue, Is.True, $"키트 {kit.KitSequence}에 판정이 채워지지 않았다\n{report}");

                bool shouldPass = kit.KitSequence != DefectiveKitSequence;
                Assert.That(kit.Verdict.Value.IsPass, Is.EqualTo(shouldPass), $"키트 {kit.KitSequence}의 검사 판정이 맞지 않다(누락 {kit.GetMissingAnswerForEvaluation()?.ToString() ?? "없음"}, 판정 슬롯 {string.Join(",", kit.Verdict.Value.MissingSlots)})\n{report}");
            }

            KitData defective = ready.Single(k => k.KitSequence == DefectiveKitSequence);
            Assert.That(defective.Verdict.Value.MissingSlots, Does.Contain(defective.GetMissingAnswerForEvaluation().Value), $"불량 키트의 판정이 빠진 슬롯을 짚지 못했다\n{report}");
        }

        // 정상 키트 1·2번: 검사 → 팩킹 → 뚜껑 → 컨베이어 끝의 순서로 지나갔고, 박스에 뚜껑이 덮여 있다.
        private static void AssertGoodKitsWentThroughPacking(ConveyorEventRecorder events, string report)
        {
            Assert.That(events.KitsWith(FlowStep.AtPacking), Is.EquivalentTo(GoodKitSequences), "팩킹 자리에 선 키트가 정상 키트가 아니다\n" + report);
            Assert.That(events.KitsWith(FlowStep.LidClosed), Is.EquivalentTo(GoodKitSequences), "뚜껑이 덮인 키트가 정상 키트가 아니다\n" + report);
            Assert.That(events.KitsWith(FlowStep.AtConveyorEnd), Is.EquivalentTo(GoodKitSequences), "컨베이어 끝에 닿은 키트가 정상 키트가 아니다\n" + report);

            foreach (int kit in GoodKitSequences)
            {
                int[] order =
                {
                    events.IndexOf(kit, FlowStep.KitReady),
                    events.IndexOf(kit, FlowStep.AtInspection),
                    events.IndexOf(kit, FlowStep.InspectionResult),
                    events.IndexOf(kit, FlowStep.AtPacking),
                    events.IndexOf(kit, FlowStep.LidClosed),
                    events.IndexOf(kit, FlowStep.AtConveyorEnd)
                };
                Assert.That(order, Is.Ordered.Ascending, $"키트 {kit}가 키팅 → 검사 → 팩킹 → 뚜껑 → 끝 순서로 지나가지 않았다\n{report}");
                Assert.That(order.All(i => i >= 0), Is.True, $"키트 {kit}가 거치지 않은 단계가 있다\n{report}");
                Assert.That(events.IndexOf(kit, FlowStep.KitRejected), Is.EqualTo(-1), $"정상 키트 {kit}가 불량으로 처리됐다\n{report}");
            }
        }

        // 정상 키트 1·2번은 뚜껑이 덮인 뒤 컨베이어 끝에 닿기 전에 뚜껑 윗면에 라벨을 받고(병원명, 로트, 키트 코드), 불량 키트 3번은 받지 않는다. (FR-11)
        private static void AssertLabelsWereAttached(ConveyorEventRecorder events, LabelRecorder labels, LabelStation station, string report)
        {
            Assert.That(labels.Kits, Is.EquivalentTo(GoodKitSequences), "라벨을 받은 키트가 정상 키트가 아니다\n" + report);
            Assert.That(station.LabeledCount, Is.EqualTo(GoodKitCount), "라벨 설비가 센 라벨 수가 정상 키트 수와 다르다\n" + report);

            foreach (int kit in GoodKitSequences)
            {
                Assert.That(labels.TryGet(kit, out LabelRecorder.Record label), Is.True, report);
                KitData data = events.Timeline.First(e => e.Kit.KitSequence == kit).Kit;
                Assert.That(label.Lines, Is.EqualTo(LabelText.Compose(data)), $"키트 {kit}의 라벨에 적힌 글이 병원명·로트·키트 코드와 다르다\n{report}");
                Assert.That(label.ParentName, Does.StartWith(CellObjectNames.LidPrefix), $"키트 {kit}의 라벨이 뚜껑에 붙지 않았다(붙은 곳 {label.ParentName})\n{report}");

                // 뚜껑이 덮인 뒤에 붙고, 끝에 닿기 전에 붙어야 한다.
                float lidClosedAt = events.Timeline[events.IndexOf(kit, FlowStep.LidClosed)].Time;
                float atEndAt = events.Timeline[events.IndexOf(kit, FlowStep.AtConveyorEnd)].Time;
                Assert.That(label.Time, Is.GreaterThan(lidClosedAt).And.LessThan(atEndAt), $"키트 {kit}의 라벨이 뚜껑을 덮은 뒤 끝에 닿기 전에 붙지 않았다({lidClosedAt:0.0}초 → 라벨 {label.Time:0.0}초 → 끝 {atEndAt:0.0}초)\n{report}");
            }
        }

        // 불량 키트 3번: 푸셔가 밀어 불량 박스에 한 번 들어갔고, 팩킹과 뚜껑, 끝으로는 가지 않았다.
        private static void AssertDefectiveKitWasRejected(ConveyorEventRecorder events, RejectBin rejectBin, int rejectedBefore, string report)
        {
            Assert.That(events.KitsWith(FlowStep.KitRejected), Is.EqualTo(new[] { DefectiveKitSequence }), "불량 박스에 들어간 키트가 불량 키트 하나가 아니다\n" + report);
            Assert.That(rejectBin.Total - rejectedBefore, Is.EqualTo(1), "불량 박스의 누적 불량 수가 1이 아니다\n" + report);

            // 푸셔는 5초 안에 박스가 불량 박스에 들어오지 않으면 "밖으로 떨어졌다"며 KitRejected를 대신 보낸다. 그것은 성공이 아니므로 따로 막는다.
            // 누적 수(Total)에는 밖으로 떨어진 것도 들어가므로, 실제로 박스 안에 들어 있는지(Loaded)로 확인한다.
            Assert.That(events.Warnings, Has.None.StartsWith(OutsideWarningCode), "푸셔가 민 박스가 불량 박스에 들어가지 못하고 밖으로 처리됐다\n" + report);
            Assert.That(rejectBin.Loaded, Is.EqualTo(1), "불량 박스 안에 실제로 들어 있는 키트가 1개가 아니다\n" + report);
            Assert.That(events.IndexOf(DefectiveKitSequence, FlowStep.KitRejected), Is.GreaterThan(events.IndexOf(DefectiveKitSequence, FlowStep.InspectionResult)), "불량 판정 뒤에 불량 박스로 가지 않았다\n" + report);

            Assert.That(events.IndexOf(DefectiveKitSequence, FlowStep.AtPacking), Is.EqualTo(-1), "불량 키트가 팩킹 자리까지 갔다\n" + report);
            Assert.That(events.IndexOf(DefectiveKitSequence, FlowStep.LidClosed), Is.EqualTo(-1), "불량 키트에 뚜껑이 덮였다\n" + report);
            Assert.That(events.IndexOf(DefectiveKitSequence, FlowStep.AtConveyorEnd), Is.EqualTo(-1), "불량 키트가 컨베이어 끝까지 갔다\n" + report);
        }

        private static string BuildReport(string endReason, float simSeconds, double wallSeconds, ConveyorEventRecorder events, IKittingStation station, PushedBoxTrace trace, LabelRecorder labels)
        {
            return $"[FullProcess] 결과: {endReason} (시뮬레이션 {simSeconds:0.0}초, 실제 {wallSeconds:0.0}초)\n" +
                   $"[FullProcess] 키팅: 완료 {station.Status.CompletedKitCount}개, 오류 {station.Status.FaultCount}번, 마지막 결과 {station.Status.LastResult} {station.Status.LastMessage}\n" +
                   $"[FullProcess] 알림 순서:\n{events.DescribeTimeline()}\n" +
                   $"[FullProcess] {events.Describe()}\n" +
                   $"[FullProcess] 라벨:\n{labels.Describe()}\n" +
                   $"[FullProcess] {trace.Dump()}";
        }
    }
}
#endif
