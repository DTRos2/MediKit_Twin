#if UNITY_EDITOR
using System.Collections;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 관절 제어 패널이 코드로 그려지는 것이 아니라 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지, 머리줄을 누르면 펼쳐지는지,
    /// 수동 제어를 켜면 로봇의 자동 목표가 막히고 관절 슬라이더가 팔을 움직이는지, 끄면 풀리는지 본다.
    /// 마우스로 누르는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로, 버튼이 누를 때 부르는 접점(onClick, slider.value)을 직접 부른다.
    /// 문구는 EditMode 시험(ArmJointPanelTextsTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class ArmJointPanelSceneTests
    {
        // 팔이 목표로 가는 걸 기다리는 시간(초, 시뮬레이션 시간)과 도착으로 보는 오차(도).
        private const float MoveTimeoutSeconds = 12f;
        private const float ArrivedToleranceDeg = 2f;

        // 슬라이더로 보낼 목표를 지금 각도에서 얼마나 옮길지(도).
        private const float TargetOffsetDeg = 10f;

        [UnityTest, Timeout(180000)]
        public IEnumerator ThePanelIsInTheHierarchy_FoldsOpen_AndTheSlidersMoveTheArmInManualControl()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            // 자동 키팅이 팔을 움직이면 시험이 보려는 슬라이더 움직임과 섞인다. 키팅은 멈춰 둔다.
            CellTestScene.DisableAll<KittingController>();

            var presenter = Object.FindAnyObjectByType<ArmJointPanelPresenter>(FindObjectsInactive.Include);
            Assert.That(presenter, Is.Not.Null, "씬에 ArmJointPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
            Assert.That(presenter.enabled, Is.True, "패널이 로봇을 찾지 못해 꺼졌다");
            Assert.That(presenter.GetComponentInParent<Canvas>(), Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");

            // 관절 제어는 접힌 채 시작한다. 머리줄을 누르면 펼쳐진다.
            var collapsible = presenter.GetComponent<UiCollapsiblePanel>();
            Assert.That(collapsible, Is.Not.Null);
            Assert.That(collapsible.IsCollapsed, Is.True, "관절 제어 패널은 접힌 채 시작한다");

            Button header = presenter.transform.Find("Fill/Header").GetComponent<Button>();
            header.onClick.Invoke();
            yield return null;
            Assert.That(collapsible.IsCollapsed, Is.False, "머리줄을 누르면 펼쳐진다");

            // 로봇이 준비될 때까지 기다린다. 준비되면 본문이 보인다.
            IArmState state = FindPanelRobot(presenter);
            Assert.That(state, Is.Not.Null, "패널이 조작할 로봇을 찾을 수 없다");
            yield return CellTestScene.WaitUntil(() => state.IsReady, MoveTimeoutSeconds);
            Assert.That(state.IsReady, Is.True, "로봇이 준비되지 않았다");
            yield return null;
            yield return null;

            GameObject ready = FindChild(presenter, "Ready");
            Assert.That(ready.activeSelf, Is.True, "로봇이 준비되면 본문이 보여야 한다");
            Assert.That(FindChild(presenter, "NotReady").activeSelf, Is.False, "준비되면 안내 글은 사라진다");

            // 관절 줄은 로봇의 관절 수만큼 켜지고, 이름은 관절 이름이다.
            UiSliderRow[] rows = presenter.GetComponentsInChildren<UiSliderRow>(true);
            int jointCount = state.JointNames.Count;
            Assert.That(rows.Length, Is.GreaterThanOrEqualTo(jointCount), "관절 줄이 관절 수보다 적다");
            for (int i = 0; i < rows.Length; i++)
            {
                Assert.That(rows[i].gameObject.activeSelf, Is.EqualTo(i < jointCount), $"관절 줄 {i + 1}의 켜짐이 관절 수와 다르다");
            }

            Assert.That(TextIn(rows[0], "Name"), Is.EqualTo(state.JointNames[0]));
            int objectCountBefore = presenter.GetComponentsInChildren<Transform>(true).Length;

            // 수동 제어를 켜면 자동 목표가 막힌다.
            IManualHoldState hold = (state as Component)?.GetComponent<IManualHoldState>();
            UiSwitchRow manualSwitch = FindSwitch(presenter, "ManualSwitch");
            manualSwitch.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            if (hold != null)
            {
                Assert.That(hold.IsHeld, Is.True, "수동 제어를 켜면 로봇의 자동 목표가 막혀야 한다");
            }

            Assert.That(rows[0].GetComponent<CanvasGroup>().interactable, Is.True, "수동 제어 중에는 관절 슬라이더를 만질 수 있다");

            // 슬라이더로 첫 관절의 목표를 옮기면 팔이 그리로 간다.
            var current = new float[jointCount];
            var lower = new float[jointCount];
            var upper = new float[jointCount];
            state.GetJointPositions(current);
            state.GetJointLimits(lower, upper);
            float startDeg = current[0] * Mathf.Rad2Deg;
            float lowerDeg = lower[0] * Mathf.Rad2Deg;
            float upperDeg = upper[0] * Mathf.Rad2Deg;
            float targetDeg = startDeg + TargetOffsetDeg <= upperDeg ? startDeg + TargetOffsetDeg : startDeg - TargetOffsetDeg;
            Assert.That(targetDeg, Is.InRange(lowerDeg, upperDeg), "첫 관절이 움직일 여유가 없다");

            Slider slider = rows[0].GetComponentInChildren<Slider>(true);
            slider.value = targetDeg;
            yield return CellTestScene.WaitUntil(() => Mathf.Abs(Joint0Deg(state, current) - targetDeg) < ArrivedToleranceDeg, MoveTimeoutSeconds);
            Assert.That(Joint0Deg(state, current), Is.EqualTo(targetDeg).Within(ArrivedToleranceDeg), "슬라이더로 정한 목표로 팔이 가야 한다");

            // 수동 제어를 끄면 막아 둔 자동 목표가 풀린다.
            manualSwitch.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            if (hold != null)
            {
                Assert.That(hold.IsHeld, Is.False, "수동 제어를 끄면 자동 목표가 풀려야 한다");
            }

            Assert.That(rows[0].GetComponent<CanvasGroup>().interactable, Is.False, "수동 제어가 꺼지면 관절 슬라이더는 보기만 한다");

            Assert.That(
                presenter.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(objectCountBefore),
                "시험하는 동안 오브젝트를 새로 만들면 안 된다(계층에 놓인 오브젝트의 글과 색만 바꾼다)");

            // 머리줄을 다시 누르면 접힌다.
            header.onClick.Invoke();
            yield return null;
            Assert.That(collapsible.IsCollapsed, Is.True);

            yield return CellTestScene.StopToolCameraFeeds();
        }

        // 패널이 조작하는 로봇. 패널의 _arm(직렬화 필드)이 지정돼 있으면 그것이고, 아니면 씬에서 찾은 첫 로봇이다.
        private static IArmState FindPanelRobot(ArmJointPanelPresenter presenter)
        {
            var serialized = new UnityEditor.SerializedObject(presenter);
            var assigned = serialized.FindProperty("_arm").objectReferenceValue as MonoBehaviour;
            return assigned as IArmState ?? CellTestScene.FindOne<IArmState>();
        }

        private static float Joint0Deg(IArmState state, float[] buffer)
        {
            state.GetJointPositions(buffer);
            return buffer[0] * Mathf.Rad2Deg;
        }

        private static UiSwitchRow FindSwitch(ArmJointPanelPresenter presenter, string objectName)
        {
            foreach (UiSwitchRow row in presenter.GetComponentsInChildren<UiSwitchRow>(true))
            {
                if (row.name == objectName)
                {
                    return row;
                }
            }

            Assert.Fail($"스위치 줄 '{objectName}'이 패널에 없다");
            return null;
        }

        private static GameObject FindChild(ArmJointPanelPresenter presenter, string objectName)
        {
            foreach (Transform child in presenter.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.gameObject;
                }
            }

            Assert.Fail($"패널에 '{objectName}' 오브젝트가 없다");
            return null;
        }

        private static string TextIn(Component parent, string objectName)
        {
            foreach (Text text in parent.GetComponentsInChildren<Text>(true))
            {
                if (text.name == objectName)
                {
                    return text.text;
                }
            }

            Assert.Fail($"'{parent.name}' 안에 '{objectName}' 글자가 없다");
            return null;
        }
    }
}
#endif
