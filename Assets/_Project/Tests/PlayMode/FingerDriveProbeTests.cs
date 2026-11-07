#if UNITY_EDITOR
using System.Collections;
using System.Text;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 팔 A(FR5)의 직선 손가락 2개가 아무것도 닿지 않을 때 구동 목표를 제대로 지키는지 잰다. 쥔 뒤 손가락이 목표보다 더 닫히는 것이
    /// 구동 때문인지 닿는 물체 때문인지 가르려는 것이다. 키팅은 돌리지 않고, 결과는 로그의 [손가락 구동] 줄에 남기며 판정은 하지 않는다.
    /// 손가락 관절은 이름을 따로 적지 않고 그리퍼 프로필에서 읽어(FingerJointSet) JointGripper와 같은 관절을 잰다.
    /// </summary>
    public sealed class FingerDriveProbeTests
    {
        private const float SettleSeconds = 1.5f;
        private const float ModelReadyTimeoutSeconds = 20f;

        // 쥔 자리 목표를 흉내 낸 값(m): 얇은 물건을 쥐었을 때 나온 값(32.3mm, 25.9mm)과 그 사이 값들.
        private static readonly float[][] Targets =
        {
            new[] { 0.0323f, 0.0259f },
            new[] { 0.0200f, 0.0200f },
            new[] { 0.0100f, 0.0300f },
            new[] { 0.0f, 0.0f },
        };

        [UnityTest, Timeout(300000)]
        public IEnumerator Measure_FingersKeepTheirDriveTarget_WhenNothingTouchesThem()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.captureFramerate = FrameTimingSettings.DefaultCaptureFramerate;

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProjectAssetPaths.CellScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            // 씬에는 팔이 둘이라 "아무거나 하나"를 고르면 안 된다. 이 시험이 가정하는 직선 손가락 2개인 팔과 그 팔의 그리퍼를 한 묶음으로 얻는다.
            StraightFingerArm armA = null;
            yield return StraightFingerArm.WaitUntilFound(ModelReadyTimeoutSeconds, found => armA = found);
            Assert.That(armA, Is.Not.Null, "직선 손가락 2개인 팔(과 그 그리퍼)을 찾을 수 없다");
            FingerJointSet fingers = armA.Fingers;

            // 그리퍼 스크립트가 매 스텝 목표를 덮어쓰지 못하게 끄고, 이 시험이 목표를 직접 정한다.
            armA.Gripper.enabled = false;
            yield return new WaitForSeconds(0.5f);

            var report = new StringBuilder();
            foreach (float[] target in Targets)
            {
                for (int i = 0; i < fingers.Count; i++)
                {
                    SetTarget(fingers.Joints[i], target[i]);
                }

                yield return new WaitForSeconds(SettleSeconds);
                report.AppendLine(Describe(target, fingers));
            }

            Debug.Log($"[손가락 구동] 아무것도 닿지 않을 때 목표와 도달 위치(mm):\n{report}");
            Time.captureFramerate = 0;
        }

        private static void SetTarget(ArticulationBody body, float targetM)
        {
            ArticulationDrive drive = body.xDrive;
            drive.target = targetM;
            body.xDrive = drive;
        }

        private static string Describe(float[] target, FingerJointSet fingers)
        {
            ArticulationBody first = fingers.Joints[0];
            ArticulationBody second = fingers.Joints[1];
            return $"  목표 {target[0] * 1000f:0.0}/{target[1] * 1000f:0.0} → 도달 {first.jointPosition[0] * 1000f:0.0}/{second.jointPosition[0] * 1000f:0.0}, " +
                   $"속도 {first.jointVelocity[0] * 1000f:0.0}/{second.jointVelocity[0] * 1000f:0.0}mm/s";
        }
    }
}
#endif
