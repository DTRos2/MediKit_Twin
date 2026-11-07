#if UNITY_EDITOR
using System.Collections;
using MediKitTwin.ArmA;
using MediKitTwin.ArmB;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using MediKitTwin.Orders;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 키팅으로 물건을 담은 박스가 컨베이어를 타고 끝까지 가는 동안, 안의 물건이 박스와 함께 움직이는지 본다.
    /// 컨베이어 시험(ConveyorHandoffTests)은 빈 박스로 흐름만 보므로 물건이 따로 움직이는 문제를 잡지 못해 따로 만들었다.
    /// 키트를 실제로 하나 만들고(팔 A), 박스가 지그를 떠난 뒤부터 끝에 닿을 때까지 물건이 받침 기준 제자리에 있는지 잰다.
    /// 첫 시험은 팔 B의 뚜껑 덮기를 끄고 컨베이어와 물건만 보고, 둘째 시험은 팔 B가 물건이 든 박스에 뚜껑을 덮는 것까지 함께 본다.
    /// 둘째 시험은 물건을 쥘 때 충돌체 짝(Physics.IgnoreCollision)을 많이 걸어 둔 채 팔 B가 구동 값을 쓰면 PhysX가 죽던 문제의 회귀 시험이기도 하다.
    /// </summary>
    public sealed class KitBoxRideTests
    {
        private const string OneKitOrder = "1,시험병원,EM,1";
        private const float KittingTimeoutSeconds = 600f;
        private const float RideTimeoutSeconds = 400f;
        private const float ProgressLogIntervalSeconds = 2f;
        private const float HoldSeconds = 2f;

        // 물건이 박스와 함께 가면 받침 기준 자리가 변하지 않는다. 물리가 풀리는 오차를 감안한 값이다.
        private const float DriftToleranceM = 0.005f;

        [UnityTest, Timeout(1800000)]
        public IEnumerator ItemsInTheBox_RideWithTheBox_ToTheEnd_WithoutTheLidCover()
        {
            yield return Ride(runLidCover: false);
        }

        [UnityTest, Timeout(1800000)]
        public IEnumerator ItemsInTheBox_RideWithTheBox_ThroughTheLidCover_ToTheEnd()
        {
            yield return Ride(runLidCover: true);
        }

        private static IEnumerator Ride(bool runLidCover)
        {
            Time.captureFramerate = FrameTimingSettings.DefaultCaptureFramerate;
            LogAssert.ignoreFailingMessages = true;
            using (var events = new ConveyorEventRecorder())
            {
                try
                {
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProjectAssetPaths.CellScene, new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null;
                    yield return null;
                    StopToolCameraFeedsNow();
                    if (!runLidCover)
                    {
                        foreach (LidCoverTask task in Object.FindObjectsByType<LidCoverTask>())
                        {
                            task.enabled = false;
                        }
                    }

                    IOrderInput input = FindOrderInput();
                    Assert.That(input, Is.Not.Null, "씬에서 주문 입력(IOrderInput)을 찾을 수 없다");
                    input.LoadText(OneKitOrder);

                    // 1) 키트가 끝나 박스가 받침에 얹히면 그때부터 물건의 받침 기준 자리를 적는다.
                    KitBoxCarrier carrier = null;
                    float deadline = Time.time + KittingTimeoutSeconds;
                    while (carrier == null && Time.time < deadline)
                    {
                        yield return null;
                        carrier = Object.FindAnyObjectByType<KitBoxCarrier>();
                    }

                    Assert.That(carrier, Is.Not.Null, "키트가 끝나 박스가 컨베이어에 오르는 데까지 가지 못했다");
                    var drift = new CarriedItemDrift(carrier);
                    Assert.That(drift.Count, Is.GreaterThan(0), "컨베이어에 오른 박스 안에 물건이 하나도 없다");
                    Debug.Log($"[KitBoxRideTests] 박스가 컨베이어에 올랐다. 물건 {drift.Count}개\n{drift.Describe()}");

                    // 2) 팩킹 구역에 서기까지 프레임마다 잰다.
                    float nextLog = Time.time;
                    deadline = Time.time + RideTimeoutSeconds;
                    while (events.AtPacking == null && Time.time < deadline)
                    {
                        drift.Sample();
                        nextLog = LogProgress(carrier, drift, events, nextLog);
                        yield return null;
                    }

                    Assert.That(events.AtPacking, Is.Not.Null, $"박스가 팩킹 구역에 서지 못했다\n{events.Describe()}\n{drift.Describe()}");
                    Debug.Log("[KitBoxRideTests] 팩킹 구역에 섰다");

                    // 3) 팔 B가 없으면 정해 둔 시간 서 있다가 뚜껑이 덮였다고 알려 출발시키고, 있으면 팔 B가 덮을 때까지 기다린다.
                    if (!runLidCover)
                    {
                        float holdUntil = Time.time + HoldSeconds;
                        while (Time.time < holdUntil)
                        {
                            drift.Sample();
                            yield return null;
                        }

                        CellEvents.RaiseLidClosed(events.AtPacking);
                    }

                    // 4) 끝에 닿을 때까지 잰다.
                    deadline = Time.time + RideTimeoutSeconds;
                    while (events.AtConveyorEnd == null && Time.time < deadline)
                    {
                        drift.Sample();
                        nextLog = LogProgress(carrier, drift, events, nextLog);
                        yield return null;
                    }

                    drift.Sample();
                    Debug.Log($"[KitBoxRideTests] 물건 {drift.Count}개, 최대 벗어남 {drift.MaxDriftM * 100f:0.0}cm\n{drift.Describe()}");

                    Assert.That(events.AtConveyorEnd, Is.Not.Null, $"박스가 끝까지 가지 못했다\n{events.Describe()}\n{drift.Describe()}");
                    Assert.That(drift.AnyWasSimulated, Is.False, $"컨베이어를 타는 동안 물리로 움직인 물건이 있다\n{drift.Describe()}");
                    Assert.That(drift.MaxDriftM, Is.LessThan(DriftToleranceM), $"박스 안의 물건이 박스와 따로 움직였다\n{drift.Describe()}");
                }
                finally
                {
                    Time.captureFramerate = 0;
                }
            }
        }

        private static float LogProgress(KitBoxCarrier carrier, CarriedItemDrift drift, ConveyorEventRecorder events, float nextLog)
        {
            if (Time.time < nextLog)
            {
                return nextLog;
            }

            Debug.Log($"[KitBoxRideTests] t={Time.time:0.0} 받침 {carrier.transform.position}, 최대 벗어남 {drift.MaxDriftM * 100f:0.0}cm, 도착 알림 {events.AtPacking != null}, 뚜껑 {events.LidClosed != null}");
            return nextLog + ProgressLogIntervalSeconds;
        }

        // 그래픽이 없는 배치 모드에서 도구 카메라가 남기는 RenderTexture 오류가 다음 시험에 번지지 않게 끝날 때 그리기를 멈춘다.
        [UnityTearDown]
        public IEnumerator StopToolCameraFeeds()
        {
            StopToolCameraFeedsNow();
            yield return null;
        }

        private static void StopToolCameraFeedsNow()
        {
            foreach (ToolCameraFeed feed in Object.FindObjectsByType<ToolCameraFeed>())
            {
                feed.enabled = false;
            }
        }

        private static IOrderInput FindOrderInput()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour is IOrderInput found)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
#endif
