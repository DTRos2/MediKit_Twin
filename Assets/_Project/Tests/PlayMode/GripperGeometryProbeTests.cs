#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.ArmA;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 그리퍼가 도구 끝점 둘레로 얼마나 퍼지는지 잰다. 박스에 물건을 놓을 때 손가락과 몸체가 박스 안쪽 폭에 들어가는지 가늠하는 값이며,
    /// 키팅을 돌리지 않고 로봇만 세워 두고 벌림을 바꿔 가며 잰다. 결과는 로그의 [그리퍼 측정] 줄에 남기고 판정은 하지 않는다.
    /// </summary>
    public sealed class GripperGeometryProbeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Cell.unity";
        private const int CaptureFramerate = 50;
        private const float SettleSeconds = 2f;
        private static readonly float[] Openings = { 1f, 0.5f, 0f };

        [UnityTest, Timeout(300000)]
        public IEnumerator Measure_TheGripperSpreadAroundTheToolPoint_AtSeveralOpenings()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.captureFramerate = CaptureFramerate;

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var arm = Object.FindAnyObjectByType<IkArmTarget>();
            var gripper = Object.FindAnyObjectByType<JointGripper>();
            Assert.That(arm, Is.Not.Null, "씬에서 IkArmTarget을 찾을 수 없다");
            Assert.That(gripper, Is.Not.Null, "씬에서 JointGripper를 찾을 수 없다");

            IArmModel model = ((IArmModelSource)arm).Model;
            float waited = 0f;
            while ((model == null || !model.IsReady) && waited < 20f)
            {
                yield return null;
                waited += Time.deltaTime;
                model = ((IArmModelSource)arm).Model;
            }

            Assert.That(model != null && model.IsReady, Is.True, "팔 모델이 준비되지 않았다");
            var host = (IGripperHost)model;
            HashSet<Collider> fingers = new HashSet<Collider>(gripper.FingerColliders);

            foreach (float opening in Openings)
            {
                gripper.SetOpening(opening);
                yield return new WaitForSeconds(SettleSeconds);
                Report(opening, host, fingers);
            }

            Time.captureFramerate = 0;
        }

        // 도구 링크 아래의 충돌체를 손가락과 그 밖(몸체, 카메라 하우징 등)으로 나눠, 도구 끝점 기준으로 어디까지 퍼지는지 적는다.
        private static void Report(float opening, IGripperHost host, HashSet<Collider> fingers)
        {
            Pose tool = host.ToolPose;
            var fingerExtent = new ToolFrameExtent(tool);
            var bodyExtent = new ToolFrameExtent(tool);
            var allExtent = new ToolFrameExtent(tool);
            var perFinger = new List<string>();

            foreach (Collider collider in host.ToolBody.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }

                var one = new ToolFrameExtent(tool);
                one.Include(ColliderPoints.Of(collider));
                allExtent.Include(ColliderPoints.Of(collider));

                if (fingers.Contains(collider))
                {
                    fingerExtent.Include(ColliderPoints.Of(collider));
                    perFinger.Add($"{collider.transform.parent?.name}/{collider.name}: {one.Describe()}");
                }
                else
                {
                    bodyExtent.Include(ColliderPoints.Of(collider));
                }
            }

            Debug.Log(
                $"[그리퍼 측정] 벌림 {opening:0.0} (도구 링크 {host.ToolBody.name}, 도구 끝점 기준 · x/y/z는 도구의 축)\n" +
                $"  손가락 전체: {fingerExtent.Describe()}\n" +
                $"  몸체(손가락 제외): {bodyExtent.Describe()}\n" +
                $"  전체: {allExtent.Describe()}\n" +
                $"  손가락 충돌체별:\n    {string.Join("\n    ", perFinger)}");
        }
    }
}
#endif
