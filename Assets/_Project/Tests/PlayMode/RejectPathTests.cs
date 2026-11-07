#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
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
    /// 불량 판정을 받은 박스가 컨베이어에서 푸셔 앞까지 가서 밀리고 불량 박스에 들어가는지만 본다. (FR-09)
    /// 키팅은 돌리지 않고 빈 박스를 보낸다. 빈 박스는 슬롯이 모두 비어 있어 검사에서 불량이 나오므로, 키팅 80초를 기다리지 않고 푸셔 경로만 빠르게 볼 수 있다.
    /// 배치 모드에서는 검사 카메라가 그리지 못해 규칙 판정(슬롯 센서)으로 판정된다.
    /// </summary>
    public sealed class RejectPathTests
    {
        private const float TimeoutSeconds = 150f;
        private const double WallTimeoutSeconds = 600d;
        private const string OutsideWarningCode = "PUSHER_KIT_OUTSIDE";

        [UnityTest, Timeout(900000)]
        public IEnumerator DefectiveBox_IsPushedFromTheConveyorIntoTheRejectBin()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.captureFramerate = FrameTimingSettings.DefaultCaptureFramerate;

            using (var events = new ConveyorEventRecorder())
            {
                try
                {
                    yield return CellTestScene.Load();

                    var bin = Object.FindAnyObjectByType<RejectBin>();
                    var pusher = Object.FindAnyObjectByType<PusherStation>();
                    Assert.That(bin, Is.Not.Null, "씬에서 불량 박스(RejectBin)를 찾을 수 없다");
                    Assert.That(pusher, Is.Not.Null, "씬에서 푸셔(Pusher)를 찾을 수 없다");
                    Assert.That(Object.FindAnyObjectByType<InspectionConveyorLine>(), Is.Not.Null, "씬에서 검사 구간 제어(InspectionConveyorLine)를 찾을 수 없다");

                    int totalBefore = bin.Total;
                    var trace = new PushedBoxTrace(bin, pusher);
                    var kit = new KitData(1, 1, "LOT-1", "병원", "EM");
                    CellEvents.RaiseKitReady(kit);

                    var wall = Stopwatch.StartNew();
                    float start = Time.time;
                    while (events.KitsWith(FlowStep.KitRejected).Count == 0
                           && Time.time - start < TimeoutSeconds && wall.Elapsed.TotalSeconds < WallTimeoutSeconds)
                    {
                        trace.Sample(Time.time);
                        yield return null;
                    }

                    string report = $"{events.DescribeTimeline()}\n{events.Describe()}\n{trace.Dump()}";
                    Debug.Log($"[RejectPath] {report}");

                    Assert.That(events.IndexOf(1, FlowStep.InspectionResult), Is.GreaterThanOrEqualTo(0), "검사 결과가 나오지 않았다\n" + report);
                    Assert.That(kit.Verdict.HasValue && !kit.Verdict.Value.IsPass, Is.True, "빈 박스가 불량으로 판정되지 않았다\n" + report);
                    Assert.That(events.KitsWith(FlowStep.KitRejected), Is.EqualTo(new[] { 1 }), "불량 박스에 키트가 들어가지 않았다\n" + report);
                    Assert.That(events.Warnings, Has.None.StartsWith(OutsideWarningCode), "박스가 불량 박스 밖으로 떨어졌다\n" + report);
                    Assert.That(bin.Total - totalBefore, Is.EqualTo(1), "누적 불량 수가 1이 아니다\n" + report);
                    Assert.That(bin.Loaded, Is.EqualTo(1), "불량 박스에 든 키트 수가 1이 아니다\n" + report);
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
    }
}
#endif
