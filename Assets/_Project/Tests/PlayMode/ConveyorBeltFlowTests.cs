#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using ConveyorSystem;
using MediKitTwin.ArmA;
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
    /// 씬의 모든 컨베이어 구간(코너 포함)에서 벨트 무늬가 흐르는 방향이 텍스처의 줄무늬 축을 따르는지 본다.
    /// 코너 구간은 직선 구간과 UV가 놓인 방향이 달라, 맞추지 않으면 무늬를 밀어도 움직임이 보이지 않아 코너만 멈춰 있는 것처럼 보였다.
    /// 씬은 한 번만 불러온다(같은 클래스에서 씬을 두 번 불러오면 둘째 시험이 RenderTexture 오류 로그에 걸린다).
    /// </summary>
    public sealed class ConveyorBeltFlowTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator EveryBeltSegment_IncludingTheCorners_FlowsAlongThePatternAxis()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProjectAssetPaths.CellScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            StopToolCameraFeeds();

            var scroller = Object.FindAnyObjectByType<ConveyorBeltScroller>();
            Assert.That(scroller, Is.Not.Null, "씬에 ConveyorBeltScroller가 없다");

            var flows = new List<KeyValuePair<ConveyorBase, Vector2>>(scroller.Flows);
            Assert.That(flows, Is.Not.Empty, "무늬를 흘리는 구간이 하나도 없다");

            bool hasCorner = false;
            foreach (KeyValuePair<ConveyorBase, Vector2> flow in flows)
            {
                hasCorner |= flow.Key is CornerConveyor;
                Assert.That(
                    BeltUvAxes.IsAcrossPattern(flow.Value, scroller.PatternAxis), Is.False,
                    $"{flow.Key.name}: 흐름 {flow.Value}이(가) 무늬 축 {scroller.PatternAxis}이(가) 아닌 쪽이라 무늬가 움직이는 것이 보이지 않는다");
            }

            Assert.That(hasCorner, Is.True, "씬에 코너 구간이 없어 시험이 코너를 보지 못했다");

            // 씬의 모든 컨베이어 구간이 빠짐없이 무늬를 흘리는지도 본다(못 흘리는 구간이 있으면 그 구간만 멈춰 보인다).
            foreach (ConveyorBase segment in Object.FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include))
            {
                Assert.That(flows.Exists(f => f.Key == segment), Is.True, $"{segment.name}은(는) 벨트 무늬를 흘리지 않는다");
            }
        }

        // 그래픽이 없는 배치 모드에서 도구 카메라가 남기는 RenderTexture 오류가 다음 시험에 번지지 않게 끝날 때 그리기를 멈춘다.
        [UnityTearDown]
        public IEnumerator StopToolCameraFeedsAfterTest()
        {
            StopToolCameraFeeds();
            yield return null;
        }

        private static void StopToolCameraFeeds()
        {
            foreach (ToolCameraFeed feed in Object.FindObjectsByType<ToolCameraFeed>())
            {
                feed.enabled = false;
            }
        }
    }
}
#endif
