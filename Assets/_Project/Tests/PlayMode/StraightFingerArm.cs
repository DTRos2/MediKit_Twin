#if UNITY_EDITOR
using System;
using System.Collections;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 씬에서 직선 손가락 2개인 팔(팔 A, FR5)과 그 팔의 그리퍼·손가락 관절을 한 묶음으로 찾아 준다.
    /// 씬에는 팔이 둘(직선 손가락 2개의 FR5, 회전 관절 6개의 UR3)이라 팔과 그리퍼를 FindAnyObjectByType으로 따로 집으면 짝이 어긋난다.
    /// 그래서 팔 하나를 먼저 정하고, 그리퍼는 그 팔의 손가락 관절에서 거슬러 올라가 얻는다.
    /// </summary>
    internal sealed class StraightFingerArm
    {
        private const int ExpectedFingerCount = 2;

        private StraightFingerArm(IkArmTarget arm, IGripperHost host, FingerJointSet fingers, JointGripper gripper)
        {
            Arm = arm;
            Host = host;
            Fingers = fingers;
            Gripper = gripper;
        }

        public IkArmTarget Arm { get; }
        public IGripperHost Host { get; }
        public FingerJointSet Fingers { get; }
        public JointGripper Gripper { get; }

        /// <summary>준비된 팔 중 손가락이 직선 관절 2개이고 그리퍼가 있는 팔을 돌려준다. 없으면 null.</summary>
        public static StraightFingerArm Find()
        {
            foreach (IkArmTarget arm in UnityEngine.Object.FindObjectsByType<IkArmTarget>())
            {
                IArmModel model = ((IArmModelSource)arm).Model;
                if (model == null || !model.IsReady || !(model is IGripperHost host))
                {
                    continue;
                }

                if (!FingerJointSet.TryCreate(host, out FingerJointSet fingers, out _) || !IsStraightPair(fingers))
                {
                    continue;
                }

                var gripper = fingers.Joints[0].GetComponentInParent<JointGripper>();
                if (gripper != null)
                {
                    return new StraightFingerArm(arm, host, fingers, gripper);
                }
            }

            return null;
        }

        /// <summary>팔 모델이 준비될 때까지 기다렸다가 찾은 팔을 onFound로 넘긴다. 시간 안에 못 찾으면 null을 넘긴다.</summary>
        public static IEnumerator WaitUntilFound(float timeoutSeconds, Action<StraightFingerArm> onFound)
        {
            StraightFingerArm found = null;
            float waited = 0f;
            while (found == null && waited < timeoutSeconds)
            {
                found = Find();
                if (found == null)
                {
                    yield return null;
                    waited += Time.deltaTime;
                }
            }

            onFound(found);
        }

        private static bool IsStraightPair(FingerJointSet set)
        {
            if (set.Count != ExpectedFingerCount)
            {
                return false;
            }

            for (int i = 0; i < set.Count; i++)
            {
                if (set.IsRevolute(i))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
