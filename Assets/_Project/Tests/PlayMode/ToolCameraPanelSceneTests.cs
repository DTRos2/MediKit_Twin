#if UNITY_EDITOR
using System.Collections;
using MediKitTwin.Common;
using MediKitTwin.Demo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 툴 카메라 패널이 코드로 그려지는 것이 아니라 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지,
    /// 카메라가 준비되면 보이고 카메라 그림과 위치·찾은 수 칩이 채워지는지, 물건을 찾아보기 전에는 대기 글과 DETECT 0이 나오는지 본다.
    /// 칩과 그림 칸은 이름이 아니라 화면 코드(ToolCameraPanelView)의 연결 필드로 찾는다. 문구는 EditMode 시험(ToolCameraPanelTextsTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class ToolCameraPanelSceneTests
    {
        // 로봇과 카메라가 준비되기를 기다리는 시간(초, 시뮬레이션 시간).
        private const float CameraReadyTimeoutSeconds = 20f;

        [UnityTest, Timeout(180000)]
        public IEnumerator ThePanelIsInTheHierarchy_AndShowsTheCameraWhenItIsReady()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();

            var presenter = Object.FindAnyObjectByType<ToolCameraPanelPresenter>(FindObjectsInactive.Include);
            Assert.That(presenter, Is.Not.Null, "씬에 ToolCameraPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
            Assert.That(presenter.enabled, Is.True, "패널이 꺼져 있다");
            Assert.That(presenter.GetComponentInParent<Canvas>(), Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");

            var view = presenter.GetComponent<ToolCameraPanelView>();
            Assert.That(view, Is.Not.Null);
            var group = presenter.GetComponent<CanvasGroup>();
            Assert.That(group, Is.Not.Null, "보이기 여부는 패널 전체의 CanvasGroup으로 정한다");

            // 카메라가 준비되고 그림이 생길 때까지 기다린다.
            IToolCameraView camera = null;
            yield return CellTestScene.WaitUntil(
                () =>
                {
                    camera = CellTestScene.FindOne<IToolCameraView>();
                    return camera != null && camera.IsReady && camera.Image != null;
                },
                CameraReadyTimeoutSeconds);
            Assert.That(camera, Is.Not.Null, "씬에 도구 카메라(IToolCameraView)가 없다");
            Assert.That(camera.Image, Is.Not.Null, "도구 카메라가 그림을 그리지 않는다(RenderTexture)");
            yield return null;
            yield return null;

            Assert.That(group.alpha, Is.EqualTo(1f), "카메라가 준비되면 패널이 보여야 한다");
            Assert.That(group.blocksRaycasts, Is.True);

            var serialized = new SerializedObject(view);
            var image = (RawImage)serialized.FindProperty("_image").objectReferenceValue;
            var positionChip = (Text)serialized.FindProperty("_positionChip").objectReferenceValue;
            var detectChip = (Text)serialized.FindProperty("_detectChip").objectReferenceValue;
            var marker = (RectTransform)serialized.FindProperty("_marker").objectReferenceValue;

            Assert.That(image.texture, Is.SameAs(camera.Image), "카메라 그림이 그림 칸에 들어가야 한다");
            Assert.That(positionChip.text, Does.StartWith("x ").And.EndWith(" cm"), "카메라 위치 칩이 채워져야 한다");
            Assert.That(detectChip.text, Does.StartWith("DETECT "));

            // 물건을 찾아보기 전에는 인식 네모가 없다.
            if (camera.ObservationCount == 0)
            {
                Assert.That(detectChip.text, Is.EqualTo(ToolCameraPanelTexts.Detect(false)));
                Assert.That(marker.gameObject.activeSelf, Is.False, "찾기 전에는 인식 네모가 없어야 한다");
            }

            int objectCountBefore = presenter.GetComponentsInChildren<Transform>(true).Length;
            yield return null;
            yield return null;
            Assert.That(
                presenter.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(objectCountBefore),
                "시험하는 동안 오브젝트를 새로 만들면 안 된다(계층에 놓인 오브젝트의 글과 그림만 바꾼다)");

            yield return CellTestScene.StopToolCameraFeeds();
        }
    }
}
#endif
