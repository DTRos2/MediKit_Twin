#if UNITY_EDITOR
using System.Collections;
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
    /// 인식 표시가 플레이 중 코드가 만든 오브젝트가 아니라 열린 씬(Scene_Cell)의 캔버스 아래에 미리 놓인 오브젝트인지,
    /// 물건을 인식하지 않았을 때는 꺼져 있는지, 화면 좌표를 주면 그 자리로 옮겨져 글이 적히는지, 숨기면 꺼지는지 본다.
    /// 로봇이 실제로 물건을 인식하는 일은 키팅 전체 시험이 본다. 여기서는 화면 코드(RecognitionMarkerView)의 접점을 직접 부른다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class RecognitionMarkerSceneTests
    {
        // 화면 좌표로 옮긴 자리와 기대하는 자리가 얼마나 어긋나도 같다고 볼지(캔버스 단위).
        private const float PositionTolerance = 1f;

        [UnityTest, Timeout(120000)]
        public IEnumerator TheMarkerIsInTheHierarchy_AndFollowsTheScreenPoint()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            var presenter = Object.FindAnyObjectByType<RecognitionMarkerPresenter>(FindObjectsInactive.Include);
            Assert.That(presenter, Is.Not.Null, "씬에 RecognitionMarkerPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
            Assert.That(presenter.GetComponentInParent<Canvas>(), Is.Not.Null, "인식 표시가 캔버스 아래에 놓여 있어야 한다");

            var view = presenter.GetComponent<RecognitionMarkerView>();
            Assert.That(view, Is.Not.Null);

            var serialized = new SerializedObject(view);
            var area = (RectTransform)serialized.FindProperty("_area").objectReferenceValue;
            var mark = (RectTransform)serialized.FindProperty("_mark").objectReferenceValue;
            var label = (Text)serialized.FindProperty("_label").objectReferenceValue;

            // 영역은 눌림을 받지 않아 뒤의 화면을 가리지 않는다.
            Assert.That(area.GetComponent<Graphic>() == null || !area.GetComponent<Graphic>().raycastTarget, Is.True, "영역이 눌림을 가로채면 안 된다");

            // 인식한 물건이 없으면 표시는 꺼져 있다.
            yield return null;
            Assert.That(mark.gameObject.activeSelf, Is.False, "인식하지 않았으면 표시가 꺼져 있어야 한다");

            // 시험이 화면 코드를 직접 부르는 동안 동작 코드가 덮어쓰지 않도록 멈춘다.
            presenter.enabled = false;
            int objectCountBefore = area.GetComponentsInChildren<Transform>(true).Length;

            // 화면 가운데를 주면 영역 가운데에 놓인다.
            view.Show(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), "인식 A-1 · 0.42m");
            Assert.That(mark.gameObject.activeSelf, Is.True, "화면 좌표를 주면 표시가 켜져야 한다");
            Assert.That(label.text, Is.EqualTo("인식 A-1 · 0.42m"));
            Rect rect = area.rect;
            Assert.That(mark.anchoredPosition.x, Is.EqualTo(rect.center.x).Within(PositionTolerance));
            Assert.That(mark.anchoredPosition.y, Is.EqualTo(rect.center.y).Within(PositionTolerance));

            // 화면 왼쪽 아래 모서리를 주면 영역의 왼쪽 아래 모서리에 놓인다(캔버스가 화면에 맞춰 배율이 바뀌어도 어긋나지 않는다).
            view.Show(Vector2.zero, "모서리");
            Assert.That(mark.anchoredPosition.x, Is.EqualTo(rect.xMin).Within(PositionTolerance));
            Assert.That(mark.anchoredPosition.y, Is.EqualTo(rect.yMin).Within(PositionTolerance));

            // 숨기면 꺼진다.
            view.Hide();
            Assert.That(mark.gameObject.activeSelf, Is.False);

            Assert.That(
                area.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(objectCountBefore),
                "시험하는 동안 오브젝트를 새로 만들면 안 된다(미리 놓은 표시를 켜고 끌 뿐이다)");

            yield return CellTestScene.StopToolCameraFeeds();
        }
    }
}
#endif
