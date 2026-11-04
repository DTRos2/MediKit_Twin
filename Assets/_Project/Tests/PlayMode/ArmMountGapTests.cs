#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using MediKitTwin.Common;
using MediKitTwin.Mobile;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 바퀴 달린 베이스가 움직일 때 그 위의 팔 뿌리(base_link)가 베이스에 붙어 따라오는지 본다.
    /// "베이스가 가속하면 팔 뿌리와 아래 이동 부위가 벌어지고, 2배속에서 더 심해진다"는 말을 재려는 시험이다.
    /// 재는 시점은 프레임의 맨 끝(LateUpdate, 실행 순서 맨 끝)이라 카메라가 그리는 자세와 같다.
    /// 팔 뿌리는 시작할 때 베이스 기준으로 놓인 자리를 기억해 두고, 움직이는 동안 그 자리에서 얼마나 떨어졌는지를 잰다.
    /// 팔의 나머지 링크는 팔 뿌리 기준으로 같은 방식으로 재서, 뿌리는 붙는데 팔 몸통이 늦게 오는지도 가른다.
    /// </summary>
    public sealed class ArmMountGapTests
    {
        // 한 번에 가는 거리(m)와 제자리에서 도는 각도.
        private const float DriveMeters = 1.2f;
        private const float SpinDegrees = 90f;

        // 팔 뿌리가 베이스에서 이만큼(m) 넘게 떨어지면 눈에 띄게 벌어진 것으로 본다.
        private const float MaxRootGapM = 0.003f;

        // 팔 링크가 팔 뿌리에서 이만큼(m) 넘게 떨어지면 팔 몸통이 늦게 따라오는 것으로 본다.
        private const float MaxChainGapM = 0.005f;

        private const float SettleSeconds = 1f;
        private const float IdleSeconds = 0.4f;
        private const float LegTimeoutSeconds = 12f;
        private const float MetersToMm = 1000f;

        [UnityTest, Timeout(300000)]
        public IEnumerator TheArmRoot_StaysOnTheMovingBase_AtEverySpeed()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.captureFramerate = FrameTimingSettings.DefaultCaptureFramerate;
            var report = new StringBuilder();
            var failures = new List<string>();
            try
            {
                yield return CellTestScene.Load();

                var follower = Object.FindFirstObjectByType<MountedBaseFollower>();
                Assert.That(follower, Is.Not.Null, "씬에서 MountedBaseFollower를 찾을 수 없다");

                FieldInfo mountField = typeof(MountedBaseFollower).GetField("_mount", BindingFlags.NonPublic | BindingFlags.Instance);
                var mount = (Transform)mountField.GetValue(follower);
                Assert.That(mount, Is.Not.Null, "팔이 얹힌 베이스(_mount)가 비어 있다");

                MobileBaseDriver driver = mount.GetComponentInParent<MobileBaseDriver>();
                Assert.That(driver, Is.Not.Null, "베이스를 굴리는 MobileBaseDriver를 찾을 수 없다");

                ArticulationBody[] bodies = follower.GetComponentsInChildren<ArticulationBody>(true);
                ArticulationBody root = Array.Find(bodies, body => body.isRoot);
                Assert.That(root, Is.Not.Null, "팔 뿌리(ArticulationBody)를 찾을 수 없다");

                // 팔의 IK 목표는 월드 좌표라서 베이스가 가면 팔이 그 목표를 따라 움직인다. 팔 몸통이 뿌리에 붙어 오는지만 보려고 팔 제어를 끈다.
                // (관절 구동 목표는 그대로 남아 팔은 자세를 지킨다.)
                foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>())
                {
                    string typeName = behaviour.GetType().Name;
                    if (typeName == "ArmController" || typeName == "IkArmTarget" || typeName == "KittingController")
                    {
                        behaviour.enabled = false;
                    }
                }

                // 실제 공정은 베이스가 가기 전에 팔을 접고 팔과 선반의 충돌을 끈다. 여기서는 그 길을 거치지 않으므로,
                // 펴진 팔이 선반에 걸려 뒤처지는 것이 팔 몸통 측정에 섞이지 않게 팔의 충돌체를 끈다.
                foreach (Collider collider in follower.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                }

                // 팔이 시작 자세로 자리 잡을 때까지 기다린 뒤 기준 자리를 기억한다.
                yield return CellTestScene.WaitUntil(() => false, SettleSeconds);

                var sampler = new GameObject("MountGapSampler") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<MountGapSampler>();
                sampler.Bind(mount, root, bodies);

                report.AppendLine($"팔 뿌리 {root.name}, 베이스 {mount.name}, 링크 {bodies.Length}개, 프레임 고정 {Time.captureFramerate}fps");

                foreach (float speed in new[] { 1f, 2f })
                {
                    Assert.That(SimClock.TrySetSpeed(speed), Is.True, $"{speed}배속을 정할 수 없다");
                    string tag = $"{speed:0.#}배속";

                    yield return Idle(sampler, tag + " 정지", report, failures);

                    Vector3 start = driver.Position;
                    float startYaw = driver.YawDeg;

                    driver.DriveTo(start + driver.transform.forward * DriveMeters);
                    yield return Leg(sampler, driver, tag + " 전진", report, failures);

                    driver.ReverseTo(start);
                    yield return Leg(sampler, driver, tag + " 후진", report, failures);

                    driver.DriveTo(driver.Position, startYaw + SpinDegrees);
                    yield return Leg(sampler, driver, tag + " 제자리 회전", report, failures);

                    driver.DriveTo(driver.Position, startYaw);
                    yield return Leg(sampler, driver, tag + " 제자리 회전(복귀)", report, failures);
                }
            }
            finally
            {
                Time.captureFramerate = 0;
                SimClock.TrySetSpeed(1f);
            }

            Debug.Log("[ArmMountGap]\n" + report);
            Assert.That(failures, Is.Empty, "팔 뿌리가 움직이는 베이스에서 벌어졌다:\n" + string.Join("\n", failures) + "\n--- 전체 ---\n" + report);
        }

        // 도구 카메라가 그릴 때마다 남기는 RenderTexture 오류를 막으려고 끝날 때 그리기를 멈춘다.
        [UnityTearDown]
        public IEnumerator StopToolCameraFeeds()
        {
            return CellTestScene.StopToolCameraFeeds();
        }

        private static IEnumerator Idle(MountGapSampler sampler, string label, StringBuilder report, List<string> failures)
        {
            sampler.Begin();
            yield return CellTestScene.WaitUntil(() => false, IdleSeconds);
            Finish(sampler, label, report, failures);
        }

        private static IEnumerator Leg(MountGapSampler sampler, MobileBaseDriver driver, string label, StringBuilder report, List<string> failures)
        {
            sampler.Begin();
            yield return CellTestScene.WaitUntil(() => !driver.IsMoving, LegTimeoutSeconds);

            // 멈춘 뒤 한두 프레임 더 재서 멈추는 순간의 어긋남도 담는다.
            yield return null;
            yield return null;
            Finish(sampler, label, report, failures);
        }

        private static void Finish(MountGapSampler sampler, string label, StringBuilder report, List<string> failures)
        {
            GapStats stats = sampler.End();
            string line = stats.Describe(label);
            report.AppendLine(line);

            if (stats.MaxRootGapM > MaxRootGapM)
            {
                failures.Add($"[뿌리] {line}");
            }

            if (stats.MaxChainGapM > MaxChainGapM)
            {
                failures.Add($"[팔 몸통] {line}");
            }
        }

        private readonly struct GapStats
        {
            public GapStats(int frames, float maxRootGapM, float maxRootRotDeg, float speedAtMaxMps, float deltaAtMaxS, float alongAtMaxM, float maxChainGapM, float maxSpeedMps)
            {
                Frames = frames;
                MaxRootGapM = maxRootGapM;
                MaxRootRotDeg = maxRootRotDeg;
                SpeedAtMaxMps = speedAtMaxMps;
                DeltaAtMaxS = deltaAtMaxS;
                AlongAtMaxM = alongAtMaxM;
                MaxChainGapM = maxChainGapM;
                MaxSpeedMps = maxSpeedMps;
            }

            public int Frames { get; }

            public float MaxRootGapM { get; }

            public float MaxRootRotDeg { get; }

            public float SpeedAtMaxMps { get; }

            public float DeltaAtMaxS { get; }

            // 어긋남이 베이스가 가던 방향으로 얼마인지(음수면 뒤처짐).
            public float AlongAtMaxM { get; }

            public float MaxChainGapM { get; }

            public float MaxSpeedMps { get; }

            public string Describe(string label)
            {
                // 한 프레임에 베이스가 가는 거리. 어긋남이 이것과 비슷하면 팔 뿌리가 한 프레임 늦게 그려지는 것이다.
                float perFrameM = SpeedAtMaxMps * DeltaAtMaxS;
                float ratio = perFrameM > 1e-5f ? MaxRootGapM / perFrameM : 0f;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: 프레임 {1}, 뿌리 어긋남 최대 {2:0.0}mm(회전 {3:0.00}°, 그때 속도 {4:0.00}m/s, 프레임당 이동 {5:0.0}mm, 어긋남/프레임이동 {6:0.00}, 진행 방향 성분 {7:+0.0;-0.0}mm), 팔 몸통 어긋남 최대 {8:0.0}mm, 최고 속도 {9:0.00}m/s",
                    label,
                    Frames,
                    MaxRootGapM * MetersToMm,
                    MaxRootRotDeg,
                    SpeedAtMaxMps,
                    perFrameM * MetersToMm,
                    ratio,
                    AlongAtMaxM * MetersToMm,
                    MaxChainGapM * MetersToMm,
                    MaxSpeedMps);
            }
        }

        // 프레임 맨 끝(LateUpdate)에서 팔 뿌리가 베이스 기준 처음 자리에서 얼마나 떨어졌는지 잰다. 읽기만 한다.
        [DefaultExecutionOrder(32000)]
        private sealed class MountGapSampler : MonoBehaviour
        {
            private Transform _mount;
            private ArticulationBody _root;
            private ArticulationBody[] _bodies;
            private Vector3 _rootInMount;
            private Quaternion _rootRotInMount;
            private Vector3[] _inRoot;

            private bool _recording;
            private bool _hasPrevious;
            private Vector3 _previousMount;
            private int _frames;
            private float _maxRootGap;
            private float _maxRootRot;
            private float _speedAtMax;
            private float _deltaAtMax;
            private float _alongAtMax;
            private float _maxChainGap;
            private float _maxSpeed;

            public void Bind(Transform mount, ArticulationBody root, ArticulationBody[] bodies)
            {
                _mount = mount;
                _root = root;
                _bodies = bodies;
                _rootInMount = mount.InverseTransformPoint(root.transform.position);
                _rootRotInMount = Quaternion.Inverse(mount.rotation) * root.transform.rotation;
                _inRoot = new Vector3[bodies.Length];
                for (int i = 0; i < bodies.Length; i++)
                {
                    _inRoot[i] = root.transform.InverseTransformPoint(bodies[i].transform.position);
                }
            }

            public void Begin()
            {
                _frames = 0;
                _maxRootGap = 0f;
                _maxRootRot = 0f;
                _speedAtMax = 0f;
                _deltaAtMax = 0f;
                _alongAtMax = 0f;
                _maxChainGap = 0f;
                _maxSpeed = 0f;
                _hasPrevious = false;
                _recording = true;
            }

            public GapStats End()
            {
                _recording = false;
                return new GapStats(_frames, _maxRootGap, _maxRootRot, _speedAtMax, _deltaAtMax, _alongAtMax, _maxChainGap, _maxSpeed);
            }

            private void LateUpdate()
            {
                if (!_recording)
                {
                    return;
                }

                Vector3 mountPosition = _mount.position;
                float delta = Time.deltaTime;
                Vector3 travel = _hasPrevious ? mountPosition - _previousMount : Vector3.zero;
                float speed = _hasPrevious && delta > 1e-6f ? travel.magnitude / delta : 0f;

                Vector3 expected = _mount.TransformPoint(_rootInMount);
                Vector3 gap = _root.transform.position - expected;
                float gapLength = gap.magnitude;
                Quaternion expectedRot = _mount.rotation * _rootRotInMount;
                float rotGap = Quaternion.Angle(_root.transform.rotation, expectedRot);

                if (gapLength > _maxRootGap)
                {
                    _maxRootGap = gapLength;
                    _speedAtMax = speed;
                    _deltaAtMax = delta;
                    _alongAtMax = travel.sqrMagnitude > 1e-12f ? Vector3.Dot(gap, travel.normalized) : 0f;
                }

                _maxRootRot = Mathf.Max(_maxRootRot, rotGap);
                _maxSpeed = Mathf.Max(_maxSpeed, speed);

                for (int i = 0; i < _bodies.Length; i++)
                {
                    if (_bodies[i] == _root)
                    {
                        continue;
                    }

                    Vector3 expectedLink = _root.transform.TransformPoint(_inRoot[i]);
                    _maxChainGap = Mathf.Max(_maxChainGap, Vector3.Distance(_bodies[i].transform.position, expectedLink));
                }

                _previousMount = mountPosition;
                _hasPrevious = true;
                _frames++;
            }
        }
    }
}
#endif
