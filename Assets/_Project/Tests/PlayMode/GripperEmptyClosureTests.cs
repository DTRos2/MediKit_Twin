#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 아무것도 쥐지 않고 닫은 그리퍼가 얼마나 닫히는지(손가락 닫힘 비율)를 씬의 모든 그리퍼에서 잰다.
    /// "끝까지 닫혔다 = 손가락 사이에 물건이 없다"는 판정의 기준(FullyClosedRatio)은 이 값보다 충분히 아래여야 하고,
    /// 가장 얇은 물건(채혈관, 1cm)을 쥘 때의 값(0.83~0.86)보다는 위여야 한다. 두 값 사이에 기준이 여유 있게 들어가는지 본다.
    /// </summary>
    public sealed class GripperEmptyClosureTests
    {
        // 물건 없이 닫은 그리퍼는 이 비율 이상 닫혀야 한다. 가장 얇은 물건을 쥘 때의 비율(0.86)과 겹치지 않을 만큼 높아야 한다.
        private const float MinEmptyClosedRatio = 0.97f;

        // 가장 얇은 물건(채혈관, 1cm)을 쥘 때 잰 가장 큰 닫힘 비율(7키트 2배속 실행에서 0.834와 0.857). 판정 기준은 이보다 위여야 한다.
        private const float ThinnestItemClosedRatio = 0.86f;

        // 판정 기준이 위 두 값에서 최소한 떨어져 있어야 하는 여유.
        private const float ThresholdMargin = 0.03f;

        // 닫는 명령이 끝나고 손가락이 가라앉기를 기다리는 시간(초, 시뮬레이션 시간). 닫는 데 0.8초, 안정에 0.2초가 든다.
        private const float CloseWaitSeconds = 2.5f;

        [UnityTest, Timeout(180000)]
        public IEnumerator AGripperClosedOnNothing_ClosesAlmostCompletely()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();
            CellTestScene.DisableAll<KittingController>();

            var grippers = new List<JointGripper>(Object.FindObjectsByType<JointGripper>(FindObjectsInactive.Exclude));
            Assert.That(grippers, Is.Not.Empty, "씬에 JointGripper가 없다");

            foreach (JointGripper gripper in grippers)
            {
                gripper.Open();
            }

            yield return CellTestScene.WaitUntil(() => AllOpen(grippers), CloseWaitSeconds);

            foreach (JointGripper gripper in grippers)
            {
                gripper.Close();
            }

            float deadline = Time.time + CloseWaitSeconds;
            while (Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            foreach (JointGripper gripper in grippers)
            {
                float ratio = gripper.LeastClosedRatio;
                Debug.Log($"[GripperEmpty] {gripper.name}: 빈 손으로 닫은 손가락 닫힘 {ratio:0.0000}(판정 기준 {gripper.FullyClosedRatio:0.00}), 열림 {gripper.Opening:0.00}");
                Assert.That(ratio, Is.GreaterThanOrEqualTo(MinEmptyClosedRatio), $"{gripper.name}: 물건 없이 닫았는데 충분히 닫히지 않았다");

                // 판정 기준은 가장 얇은 물건을 쥘 때의 비율보다 위, 빈 손으로 닫은 비율보다 아래에 여유 있게 들어가야 한다.
                Assert.That(gripper.FullyClosedRatio, Is.GreaterThan(ThinnestItemClosedRatio + ThresholdMargin),
                    $"{gripper.name}: 판정 기준이 너무 낮아 채혈관처럼 얇은 물건을 쥐고도 쥐지 못한 것으로 본다");
                Assert.That(gripper.FullyClosedRatio, Is.LessThan(ratio - ThresholdMargin),
                    $"{gripper.name}: 판정 기준이 너무 높아 빈 손으로 닫은 것도 물건이 걸린 것으로 본다");
            }
        }

        private static bool AllOpen(List<JointGripper> grippers)
        {
            foreach (JointGripper gripper in grippers)
            {
                if (gripper.Opening < 1f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
