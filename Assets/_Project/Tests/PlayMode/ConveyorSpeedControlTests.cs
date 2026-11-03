#if UNITY_EDITOR
using System.Collections;
using ConveyorSystem;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using MediKitTwin.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 컨베이어 속도 조절 패널이 열린 씬(Scene_Cell)에 붙어 있고, 속도를 바꾸면 모든 컨베이어 구간에 반영되는지 본다.
    /// 슬라이더를 마우스로 끄는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로, 패널이 부르는 접점(IConveyorSpeedControl)을 직접 부른다.
    /// 범위 맞추기와 글자 모양은 EditMode 시험(ConveyorSpeedRangeTests, ConveyorSpeedTextTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두었을 때 둘째만 도구 카메라의 RenderTexture 오류 로그에 걸려 실패했다
    /// (로그 무시 설정과 카메라 끄기를 시험 안에서 해도 같았고 이유는 찾지 못했다). 씬은 한 번만 불러온다.
    /// </summary>
    public sealed class ConveyorSpeedControlTests
    {
        private const float ChangedSpeedMps = 1.2f;
        private const float WayTooFastMps = 99f;
        private const float SpeedTolerance = 0.0001f;

        [UnityTest, Timeout(120000)]
        public IEnumerator ThePanelIsInTheScene_AndSettingTheSpeedReachesEveryConveyorSegment_InsideTheRange()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ProjectAssetPaths.CellScene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            StopToolCameraFeeds();

            // 패널이 씬에 있고, 속도 조절 컴포넌트를 찾았다(못 찾으면 패널이 스스로 꺼진다).
            var panel = Object.FindAnyObjectByType<ConveyorSpeedDemoPanel>();
            Assert.That(panel, Is.Not.Null, "씬에 ConveyorSpeedDemoPanel이 없다(Tools/MediKit/Setup Conveyor Handoff로 붙인다)");
            Assert.That(panel.enabled, Is.True, "패널이 속도 조절 컴포넌트를 찾지 못해 꺼졌다");

            IConveyorSpeedControl control = Object.FindAnyObjectByType<ConveyorSpeedSetter>();
            Assert.That(control, Is.Not.Null, "씬에 ConveyorSpeedSetter가 없다");
            ConveyorBase[] segments = Object.FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include);
            Assert.That(segments, Is.Not.Empty, "씬에 컨베이어 구간이 없다");

            control.SetSpeed(ChangedSpeedMps);
            Assert.That(control.SpeedMps, Is.EqualTo(ChangedSpeedMps).Within(SpeedTolerance));
            AssertEverySegmentRunsAt(segments, ChangedSpeedMps);

            // 박스가 멈춰 서는 0 이하의 속도는 받지 않고 가장 느린 속도로 맞춘다.
            control.SetSpeed(0f);
            Assert.That(control.SpeedMps, Is.EqualTo(control.MinSpeedMps).Within(SpeedTolerance));
            Assert.That(control.MinSpeedMps, Is.GreaterThan(0f));
            AssertEverySegmentRunsAt(segments, control.MinSpeedMps);

            control.SetSpeed(WayTooFastMps);
            Assert.That(control.SpeedMps, Is.EqualTo(control.MaxSpeedMps).Within(SpeedTolerance));
            AssertEverySegmentRunsAt(segments, control.MaxSpeedMps);
        }

        private static void AssertEverySegmentRunsAt(ConveyorBase[] segments, float expected)
        {
            foreach (ConveyorBase segment in segments)
            {
                Assert.That(segment.Speed, Is.EqualTo(expected).Within(SpeedTolerance), $"{segment.name}의 속도가 반영되지 않았다");
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
