#if UNITY_EDITOR
using System.Collections;
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
    /// 키팅 로그 패널이 코드로 그려지는 것이 아니라 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지, 경고가 줄로 나타나는지,
    /// "경고·오류만" 단추로 정보 로그까지 보이는지, 복사가 클립보드에 담는지, 지우기가 줄을 치우는지, 머리줄을 누르면 접히는지 본다.
    /// 마우스로 누르는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로, 단추가 누를 때 부르는 접점(onClick)을 직접 부른다.
    /// 문구는 EditMode 시험(KittingLogPanelTextsTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class KittingLogPanelSceneTests
    {
        private const string WarningToken = "LOGPANEL-WARNING-7F3A";
        private const string InfoToken = "LOGPANEL-INFO-91C2";

        [UnityTest, Timeout(120000)]
        public IEnumerator ThePanelIsInTheHierarchy_ShowsWarnings_FiltersCopiesAndClears()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            var presenter = Object.FindAnyObjectByType<KittingLogPanelPresenter>(FindObjectsInactive.Include);
            Assert.That(presenter, Is.Not.Null, "씬에 KittingLogPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
            Assert.That(presenter.GetComponentInParent<Canvas>(), Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");

            // 줄은 편집기에서 미리 놓아 둔 만큼 있고, 실행 중에는 켜고 끄기만 한다.
            LogRowItem[] rows = presenter.GetComponentsInChildren<LogRowItem>(true);
            Assert.That(rows, Has.Length.EqualTo(KittingLogPanelTexts.RowCapacity), "미리 놓은 로그 줄 수가 RowCapacity와 같아야 한다");
            int objectCountBefore = presenter.GetComponentsInChildren<Transform>(true).Length;

            var collapsible = presenter.GetComponent<UiCollapsiblePanel>();
            Assert.That(collapsible, Is.Not.Null);
            Button header = presenter.transform.Find("Fill/Header").GetComponent<Button>();
            if (collapsible.IsCollapsed)
            {
                header.onClick.Invoke();
                yield return null;
            }

            Assert.That(collapsible.IsCollapsed, Is.False);

            // 경고는 처음 보기(경고·오류만)에서도 줄로 나타난다. 정보 로그는 가려진다.
            Debug.LogWarning(WarningToken);
            Debug.Log(InfoToken);
            yield return null;
            yield return null;
            Assert.That(HasRowWith(rows, WarningToken), Is.True, "경고 로그가 줄로 나타나야 한다");
            Assert.That(HasRowWith(rows, InfoToken), Is.False, "처음에는 정보 로그를 가린다");

            // "경고·오류만"을 끄면 정보 로그도 보인다.
            ClickButton(presenter, "FilterButton");
            yield return null;
            yield return null;
            Assert.That(HasRowWith(rows, InfoToken), Is.True, "전체 보기에서는 정보 로그도 보여야 한다");

            // 복사는 담아 둔 로그를 클립보드에 넣는다.
            GUIUtility.systemCopyBuffer = string.Empty;
            ClickButton(presenter, "CopyButton");
            yield return null;
            Assert.That(GUIUtility.systemCopyBuffer, Does.Contain(WarningToken), "복사한 글에 경고 로그가 들어 있어야 한다");

            // 지우기는 줄을 모두 치운다.
            ClickButton(presenter, "ClearButton");
            yield return null;
            yield return null;
            Assert.That(CountActive(rows), Is.LessThan(3), "지우면 로그 줄이 사라져야 한다(지우는 동안 새 로그 한두 줄은 생길 수 있다)");
            Assert.That(HasRowWith(rows, WarningToken), Is.False);

            Assert.That(
                presenter.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(objectCountBefore),
                "시험하는 동안 오브젝트를 새로 만들면 안 된다(미리 놓은 줄을 켜고 끌 뿐이다)");

            // 머리줄을 다시 누르면 접힌다.
            header.onClick.Invoke();
            yield return null;
            Assert.That(collapsible.IsCollapsed, Is.True);

            yield return CellTestScene.StopToolCameraFeeds();
        }

        private static void ClickButton(Component panel, string objectName)
        {
            foreach (UiKindButton button in panel.GetComponentsInChildren<UiKindButton>(true))
            {
                if (button.name == objectName)
                {
                    button.Button.onClick.Invoke();
                    return;
                }
            }

            Assert.Fail($"패널에 '{objectName}' 단추가 없다");
        }

        private static bool HasRowWith(LogRowItem[] rows, string token)
        {
            foreach (LogRowItem row in rows)
            {
                if (!row.gameObject.activeSelf)
                {
                    continue;
                }

                foreach (Text text in row.GetComponentsInChildren<Text>(true))
                {
                    if (text.name == "Message" && text.text.Contains(token))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CountActive(LogRowItem[] rows)
        {
            int count = 0;
            foreach (LogRowItem row in rows)
            {
                count += row.gameObject.activeSelf ? 1 : 0;
            }

            return count;
        }
    }
}
#endif
