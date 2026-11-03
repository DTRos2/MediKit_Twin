#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediKitTwin.Common;
using MediKitTwin.Label;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 라벨 설비의 표시판·기둥이 팔 B나 컨베이어의 박스 통로를 가로막지 않는지 본다.
    /// 라벨을 씬에 놓은 뒤 팔 B가 뚜껑 위로 닿지 못한다는 실패가 나와서, 물리적으로 겹치는지부터 확인하려고 만들었다.
    /// </summary>
    public class LabelStationClearanceTests
    {
        // 이 거리 안에 들어오면 간섭 후보로 보고 이름을 적는다(m). 기초 바로 옆(10cm 안)에 기둥이 서 있던 배치가 뚜껑 덮기를 막았다.
        private const float NearDistanceM = 0.30f;

        // 씬을 연 뒤 팔이 자리를 잡을 때까지 기다리는 시간(초).
        private const float SettleSeconds = 1f;

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator LabelStationColliders_AreFarFromArmB()
        {
            // 그래픽 없는 배치 모드에서 도구 카메라가 남기는 RenderTexture 오류는 이 시험이 보려는 일이 아니다.
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return new WaitForSeconds(SettleSeconds);

            LabelStation station = Object.FindFirstObjectByType<LabelStation>();
            Assert.That(station, Is.Not.Null, "씬에 LabelStation이 없습니다.");

            GameObject armB = GameObject.Find(CellObjectNames.ArmB);
            Assert.That(armB, Is.Not.Null, "씬에 팔 B가 없습니다.");

            var report = new StringBuilder();
            report.AppendLine($"라벨 설비 위치 {station.transform.position}, 팔 B 위치 {armB.transform.position}");

            var armColliders = new List<Collider>(armB.GetComponentsInChildren<Collider>(true));
            report.AppendLine($"팔 B 충돌체 {armColliders.Count}개");

            bool anyNear = false;
            foreach (Collider label in station.GetComponentsInChildren<Collider>(true))
            {
                report.AppendLine($"  {label.name}: 경계 중심 {label.bounds.center}, 크기 {label.bounds.size}");
                foreach (Collider arm in armColliders)
                {
                    if (arm == null || !arm.enabled)
                    {
                        continue;
                    }

                    Vector3 onArm = arm.ClosestPoint(label.bounds.center);
                    Vector3 onLabel = label.ClosestPoint(onArm);
                    float distance = Vector3.Distance(onArm, onLabel);
                    if (distance < NearDistanceM)
                    {
                        anyNear = true;
                        report.AppendLine($"    가까움 {distance:0.000}m: 팔 B의 {arm.name} ({arm.bounds.center})");
                    }
                }
            }

            Debug.Log("[LabelClearance] " + report);
            yield return CellTestScene.StopToolCameraFeeds();
            Assert.That(anyNear, Is.False, "라벨 설비가 팔 B와 너무 가깝습니다.\n" + report);
        }
    }
}
#endif
