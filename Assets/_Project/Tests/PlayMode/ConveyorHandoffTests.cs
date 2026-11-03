#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using ConveyorSystem;
using MediKitTwin.ArmA;
using MediKitTwin.ArmB;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 키팅이 끝난 박스가 컨베이어를 타고 팩킹 구역에서 멈췄다가, 뚜껑이 덮이면 끝까지 가는지 본다. (FR-05, FR-10)
    /// 팔 A의 키팅은 돌리지 않고 KitReady만 보낸다.
    /// 첫 시험은 팔 B의 뚜껑 덮기를 끄고 LidClosed를 직접 보내 컨베이어 구간만 보고,
    /// 둘째 시험은 팔 B가 실제로 컨베이어가 가져온 박스에 뚜껑을 덮는지까지 본다.
    /// </summary>
    public sealed class ConveyorHandoffTests
    {

        // 시간은 모두 시뮬레이션 시간(초)이다. 컨베이어는 약 15m를 초속 0.8m(ConveyorSpeedSetter 기본)로 가서 20초가량 걸리므로 90초면 넉넉하다.
        private const float TravelTimeoutSeconds = 90f;
        private const float LidTimeoutSeconds = 300f;
        private const float HoldCheckSeconds = 2f;
        private const float JigRefillTimeoutSeconds = 15f;
        private const float StopToleranceM = 0.02f;
        private const float AlongTrackToleranceM = 0.002f;
        private const float StopArrivalToleranceM = 0.005f;
        private const float HeldDriftToleranceM = 0.001f;

        [UnityTest, Timeout(300000)]
        public IEnumerator FinishedKit_RidesToPacking_WaitsForTheLid_ThenReachesTheEnd()
        {
            Time.captureFramerate = FrameTimingSettings.DefaultCaptureFramerate;
            LogAssert.ignoreFailingMessages = true;
            using (var events = new ConveyorEventRecorder())
            {
                try
                {
                    yield return LoadCell();
                    DisableAll<LidCoverTask>();
                    ConveyorSceneRefs refs = ConveyorSceneRefs.Find();

                    var kit = new KitData(1, 1, "LOT-1", "병원", "EM");
                    CellEvents.RaiseKitReady(kit);

                    // 1) 박스가 컨베이어를 타고 멈출 자리에서 선다.
                    yield return WaitUntil(() => events.AtPacking != null, TravelTimeoutSeconds);
                    Assert.That(events.AtPacking, Is.SameAs(kit), "팩킹 구역에 도착했다는 알림이 오지 않았다");
                    KitBoxCarrier carrier = AssertStoppedAtStopPoint(refs);

                    // 2) 뚜껑이 덮이기 전에는 움직이지 않는다.
                    Vector3 heldAt = carrier.transform.position;
                    float holdUntil = Time.time + HoldCheckSeconds;
                    while (Time.time < holdUntil)
                    {
                        yield return null;
                    }

                    Assert.That(Vector3.Distance(carrier.transform.position, heldAt), Is.LessThan(HeldDriftToleranceM), "뚜껑이 덮이기 전에 박스가 움직였다");
                    Assert.That(events.AtConveyorEnd, Is.Null, "뚜껑이 덮이기 전에 끝에 도착했다고 알렸다");

                    // 3) 뚜껑이 덮이면 다시 출발해 끝까지 간다.
                    CellEvents.RaiseLidClosed(kit);
                    yield return WaitUntil(() => events.AtConveyorEnd != null, TravelTimeoutSeconds);
                    Assert.That(events.AtConveyorEnd, Is.SameAs(kit), "컨베이어 끝에 도착했다는 알림이 오지 않았다");
                    AssertAtConveyorEnd(carrier);

                    // 4) 지그에는 새 박스가 다시 놓인다.
                    yield return WaitUntil(() => refs.Supply.HasBox, JigRefillTimeoutSeconds);
                    Assert.That(refs.Supply.HasBox, Is.True, "박스를 보낸 뒤 지그에 새 박스가 놓이지 않았다");
                }
                finally
                {
                    Time.captureFramerate = 0;
                }
            }
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator FinishedKit_GetsItsLidFromArmB_ThenReachesTheEnd()
        {
            Time.captureFramerate = FrameTimingSettings.DefaultCaptureFramerate;
            LogAssert.ignoreFailingMessages = true;
            using (var events = new ConveyorEventRecorder())
            {
                try
                {
                    yield return LoadCell();
                    ConveyorSceneRefs refs = ConveyorSceneRefs.Find();

                    var kit = new KitData(1, 1, "LOT-1", "병원", "EM");
                    CellEvents.RaiseKitReady(kit);

                    yield return WaitUntil(() => events.AtPacking != null, TravelTimeoutSeconds);
                    Assert.That(events.AtPacking, Is.SameAs(kit), $"팩킹 구역에 도착했다는 알림이 오지 않았다\n{events.Describe()}");
                    KitBoxCarrier carrier = AssertStoppedAtStopPoint(refs);

                    // 팔 B가 알아서 뚜껑을 집어 덮고 LidClosed를 보낸다. 이 시험이 보내지 않는다.
                    yield return WaitUntil(() => events.LidClosed != null, LidTimeoutSeconds);
                    Assert.That(events.LidClosed, Is.SameAs(kit), $"팔 B가 뚜껑을 덮지 못했다\n{events.Describe()}");
                    Assert.That(
                        carrier.GetComponentsInChildren<Transform>().Any(t => t.name.StartsWith(CellObjectNames.LidPrefix, StringComparison.Ordinal)),
                        Is.True,
                        "뚜껑이 박스를 따라 움직이도록 박스 아래에 붙어 있어야 한다");

                    yield return WaitUntil(() => events.AtConveyorEnd != null, TravelTimeoutSeconds);
                    Assert.That(events.AtConveyorEnd, Is.SameAs(kit), $"뚜껑을 덮은 박스가 컨베이어 끝에 도착하지 못했다\n{events.Describe()}");
                    AssertAtConveyorEnd(carrier);
                }
                finally
                {
                    Time.captureFramerate = 0;
                }
            }
        }

        // 그래픽이 없는 배치 모드에서는 도구 카메라가 화면을 그릴 때마다 RenderTexture 오류를 남긴다.
        // 시험이 끝난 뒤에도 씬이 남아 있어서, 다음 시험이 시작하기 전 틈에 나온 오류가 그 시험의 실패로 잡힌다. 끝날 때 그리기를 멈춘다.
        [UnityTearDown]
        public IEnumerator StopToolCameraFeeds()
        {
            foreach (ToolCameraFeed feed in Object.FindObjectsByType<ToolCameraFeed>())
            {
                feed.enabled = false;
            }

            yield return null;
        }

        private static IEnumerator LoadCell()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProjectAssetPaths.CellScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        // 박스 받침이 팔 B가 찾는 센서 아래에서, 멈출 자리에 진행 방향과 옆 방향 모두 맞게 섰는지 확인하고 받침을 돌려준다.
        private static KitBoxCarrier AssertStoppedAtStopPoint(ConveyorSceneRefs refs)
        {
            var carrier = Object.FindAnyObjectByType<KitBoxCarrier>();
            Assert.That(carrier, Is.Not.Null, "컨베이어 위의 박스 받침을 찾을 수 없다");
            Assert.That(carrier.transform.IsChildOf(refs.PackingSensor), Is.True, "팔 B가 찾도록 받침이 팩킹 센서 아래에 있어야 한다");
            Assert.That(
                Mathf.Abs(StopPointArrival.AlongTrack(carrier.transform.position, refs.StopPoint.position, refs.Flow)),
                Is.LessThan(AlongTrackToleranceM),
                "박스가 멈출 자리에 진행 방향으로 맞게 서지 않았다");

            // 벨트 중심선에서 옆으로 미끄러져 멈출 자리(가로 평면)에 정확히 서야 팔 B가 닿는다.
            Vector3 sideways = carrier.transform.position - refs.StopPoint.position;
            sideways.y = 0f;
            Assert.That(sideways.magnitude, Is.LessThan(StopArrivalToleranceM), "박스가 멈출 자리에 옆으로 맞게 서지 않았다");
            return carrier;
        }

        private static void AssertAtConveyorEnd(KitBoxCarrier carrier)
        {
            ConveyorBase last = FindLastConveyor();
            Assert.That(last, Is.Not.Null, "이어진 구간이 없는 마지막 구간을 찾을 수 없다");
            Assert.That(Vector3.Distance(carrier.transform.position, last.CenterPoint.position), Is.LessThan(StopToleranceM));
        }

        // 조건이 참이 되거나 시뮬레이션 시간으로 timeoutSeconds가 지날 때까지 기다린다.
        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds)
        {
            float deadline = Time.time + timeoutSeconds;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }
        }

        private static void DisableAll<T>() where T : Behaviour
        {
            foreach (T behaviour in Object.FindObjectsByType<T>())
            {
                behaviour.enabled = false;
            }
        }

        // 뒤에 이어진 구간이 없는 구간이 컨베이어의 끝이다.
        private static ConveyorBase FindLastConveyor()
        {
            foreach (ConveyorBase conveyor in Object.FindObjectsByType<ConveyorBase>())
            {
                if (!conveyor.HasDownstream)
                {
                    return conveyor;
                }
            }

            return null;
        }
    }
}
#endif
