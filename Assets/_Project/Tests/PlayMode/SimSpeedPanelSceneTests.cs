#if UNITY_EDITOR
using System.Collections;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 배속 패널이 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지, 그리고 배속 버튼을 누르면 공정 전체 시계(SimClock, Time.timeScale)가
    /// 바뀌고 켜진 버튼과 현재 배속 글이 따라 바뀌는지 본다(FR-15). 마우스로 누르는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로,
    /// 버튼이 누를 때 부르는 접점(onClick)을 직접 부른다. 버튼은 SimClock이 허용하는 배속마다 하나씩 있어야 한다.
    /// 글 문구, 버튼 종류 규칙은 EditMode 시험(SimSpeedPanelTextsTests, SimSpeedButtonKindsTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class SimSpeedPanelSceneTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator ThePanelIsInTheHierarchy_AndTheButtonsChangeTheSpeedOfTheWholeProcess()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            // 배속이 바뀌는 동안 로봇이 움직이면 시험과 상관없는 물리가 돈다. 키팅은 멈춰 둔다.
            CellTestScene.DisableAll<KittingController>();

            try
            {
                var presenter = Object.FindAnyObjectByType<SimSpeedPanelPresenter>();
                Assert.That(presenter, Is.Not.Null, "씬에 SimSpeedPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
                Assert.That(presenter.enabled, Is.True);

                Canvas canvas = presenter.GetComponentInParent<Canvas>();
                Assert.That(canvas, Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");
                Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null, "캔버스에 눌림을 받는 GraphicRaycaster가 있어야 한다");
                Assert.That(Object.FindAnyObjectByType<EventSystem>(), Is.Not.Null, "씬에 EventSystem이 있어야 버튼이 눌린다");

                SimSpeedButton[] buttons = presenter.GetComponentsInChildren<SimSpeedButton>();
                Assert.That(buttons, Has.Length.EqualTo(SimClock.AllowedSpeeds.Length), "배속 버튼은 SimClock이 허용하는 배속마다 하나다");
                foreach (float allowed in SimClock.AllowedSpeeds)
                {
                    Assert.That(FindButton(buttons, allowed), Is.Not.Null, $"{allowed}배 버튼이 없다");
                }

                yield return null;
                Assert.That(SimClock.Speed, Is.EqualTo(1f));
                Assert.That(TextOf(presenter, "Summary"), Is.EqualTo(SimSpeedPanelTexts.Current(1f)));

                // 2배 버튼을 누르면 시계와 Time.timeScale이 2배가 되고, 그 버튼만 켜진 모양이 된다.
                FindButton(buttons, 2f).GetComponentInChildren<Button>().onClick.Invoke();
                yield return null;

                Assert.That(SimClock.Speed, Is.EqualTo(2f));
                Assert.That(Time.timeScale, Is.EqualTo(2f), "배속은 Time.timeScale로 반영된다");
                Assert.That(TextOf(presenter, "Summary"), Is.EqualTo(SimSpeedPanelTexts.Current(2f)));
                AssertOnlyThisButtonLooksSelected(buttons, 2f);

                // 다시 1배 버튼을 누르면 돌아온다.
                FindButton(buttons, 1f).GetComponentInChildren<Button>().onClick.Invoke();
                yield return null;

                Assert.That(SimClock.Speed, Is.EqualTo(1f));
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                AssertOnlyThisButtonLooksSelected(buttons, 1f);

                // 배속이 고정되면(ROS 제어 중) 버튼이 눌리지 않고 고정되었다고 보인다.
                SimClock.SetSpeedLocked(true);
                yield return null;

                Assert.That(TextOf(presenter, "Summary"), Is.EqualTo(SimSpeedPanelTexts.Locked(1f)));
                foreach (SimSpeedButton button in buttons)
                {
                    Assert.That(button.GetComponentInChildren<Button>().interactable, Is.False, "고정되면 배속 버튼은 눌리지 않아야 한다");
                }
            }
            finally
            {
                SimClock.SetSpeedLocked(false);
                SimClock.TrySetSpeed(1f);
            }

            yield return CellTestScene.StopToolCameraFeeds();
        }

        private static SimSpeedButton FindButton(SimSpeedButton[] buttons, float speed)
        {
            foreach (SimSpeedButton button in buttons)
            {
                if (Mathf.Approximately(button.Speed, speed))
                {
                    return button;
                }
            }

            return null;
        }

        // 켜진 버튼은 글자색이 강조색(Selected)이고, 나머지는 보통 글자색(Secondary)이다.
        private static void AssertOnlyThisButtonLooksSelected(SimSpeedButton[] buttons, float speed)
        {
            DashboardPalette palette = DashboardPalette.Default;
            foreach (SimSpeedButton button in buttons)
            {
                Color expected = Mathf.Approximately(button.Speed, speed) ? palette.Accent : palette.Ink;
                Assert.That(button.GetComponentInChildren<Text>().color, Is.EqualTo(expected), $"{button.Speed}배 버튼의 글자색");
            }
        }

        private static string TextOf(SimSpeedPanelPresenter presenter, string objectName)
        {
            foreach (Text text in presenter.GetComponentsInChildren<Text>(true))
            {
                if (text.transform.name == objectName)
                {
                    return text.text;
                }
            }

            Assert.Fail($"패널에 '{objectName}' 글자가 없다");
            return null;
        }
    }
}
#endif
