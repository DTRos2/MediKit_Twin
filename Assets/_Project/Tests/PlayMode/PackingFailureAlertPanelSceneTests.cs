#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.ArmA;
using MediKitTwin.ArmB;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 팩킹 실패 경고 패널이 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지, 평소에는 감춰져 있다가 팩킹 실패 사건(CellEvents.PackingFailed)이 오면
    /// 뜨고, "확인하고 다시 덮기"를 누르면 감추고 다시 덮기 요청(PackingRetryRequested)을 내며, 뚜껑이 덮이면(LidClosed) 감춰지는지 본다(FR-10).
    /// 마우스로 누르는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로 버튼이 누를 때 부르는 접점(onClick)을 직접 부른다.
    /// 컨베이어와 팔 A를 멈추는 일은 PackingFailureHandler가, 다시 덮는 일은 팔 B(LidCoverTask)가 하므로 여기서는 꺼 둔다(켜 두면 시험 씬에는 키트 박스가 없어 다시 덮기가 곧바로 실패해 패널이 다시 뜬다). 글 문구는 EditMode 시험(PackingFailureAlertPanelTextsTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class PackingFailureAlertPanelSceneTests
    {
        private const string Reason = "뚜껑이 박스 위에 안착하지 못했다 — 안에 든 물건이 비스듬히 앉았다(시험에서 만든 긴 실패 이유).";

        [UnityTest, Timeout(120000)]
        public IEnumerator ThePanelIsHiddenUntilAPackingFailure_ThenTheButtonAsksToCoverAgain()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            CellTestScene.DisableAll<KittingController>();
            CellTestScene.DisableAll<PackingFailureHandler>();
            CellTestScene.DisableAll<LidCoverTask>();

            var retried = new List<KitData>();
            void OnRetry(KitData kit) => retried.Add(kit);
            CellEvents.PackingRetryRequested += OnRetry;

            try
            {
                var presenter = Object.FindAnyObjectByType<PackingFailureAlertPanelPresenter>(FindObjectsInactive.Include);
                Assert.That(presenter, Is.Not.Null, "씬에 PackingFailureAlertPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
                Assert.That(presenter.gameObject.activeInHierarchy, Is.True, "꺼 두면 사건을 받지 못한다. 감추는 것은 CanvasGroup이 한다");
                Assert.That(presenter.enabled, Is.True);

                Canvas canvas = presenter.GetComponentInParent<Canvas>();
                Assert.That(canvas, Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");
                Assert.That(Object.FindAnyObjectByType<EventSystem>(), Is.Not.Null, "씬에 EventSystem이 있어야 버튼이 눌린다");

                var view = presenter.GetComponent<PackingFailureAlertPanelView>();
                var group = presenter.GetComponent<CanvasGroup>();
                var button = presenter.GetComponentInChildren<UiKindButton>(true);
                Assert.That(view, Is.Not.Null);
                Assert.That(group, Is.Not.Null);
                Assert.That(button, Is.Not.Null, "다시 덮기 버튼이 없다");

                // 평소에는 보이지 않고 눌림도 가로채지 않는다.
                yield return null;
                Assert.That(view.IsShown, Is.False);
                Assert.That(group.alpha, Is.EqualTo(0f));
                Assert.That(group.blocksRaycasts, Is.False, "감춘 패널이 아래의 다른 패널 눌림을 막으면 안 된다");

                // 팩킹 실패가 오면 키트 번호와 이유가 적혀 뜬다.
                var kit = new KitData(3, 1, "LOT-3", "시험병원", "DX");
                CellEvents.RaisePackingFailed(kit, Reason);
                yield return null;

                Assert.That(view.IsShown, Is.True);
                Assert.That(group.alpha, Is.EqualTo(1f));
                Assert.That(group.blocksRaycasts, Is.True);
                Assert.That(group.interactable, Is.True);
                Assert.That(TextOf(presenter, "Headline"), Is.EqualTo(PackingFailureAlertPanelTexts.Headline(3)));
                Assert.That(TextOf(presenter, "Reason"), Is.EqualTo(Reason));
                Assert.That(TextOf(presenter, "Guidance"), Is.EqualTo(PackingFailureAlertPanelTexts.Guidance));

                // 긴 이유가 와도 버튼이 카드 밖으로 밀려나지 않는다.
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertInside((RectTransform)presenter.transform, (RectTransform)button.transform);

                // 버튼을 누르면 감추고, 같은 키트로 다시 덮기를 요청한다.
                button.Button.onClick.Invoke();
                yield return null;

                Assert.That(retried, Has.Count.EqualTo(1));
                Assert.That(retried[0], Is.SameAs(kit), "다시 덮기는 실패한 그 키트로 요청해야 한다");
                Assert.That(view.IsShown, Is.False);
                Assert.That(group.blocksRaycasts, Is.False);

                // 다시 해도 또 실패하면 다시 뜬다. 뚜껑이 덮이면 누르지 않아도 감춘다.
                CellEvents.RaisePackingFailed(kit, Reason);
                yield return null;
                Assert.That(view.IsShown, Is.True);

                CellEvents.RaiseLidClosed(kit);
                yield return null;
                Assert.That(view.IsShown, Is.False);
                Assert.That(retried, Has.Count.EqualTo(1), "뚜껑이 덮여 감춘 것은 다시 덮기 요청이 아니다");
            }
            finally
            {
                CellEvents.PackingRetryRequested -= OnRetry;
            }

            yield return CellTestScene.StopToolCameraFeeds();
        }

        private static string TextOf(Component panel, string objectName)
        {
            foreach (UnityEngine.UI.Text text in panel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (text.gameObject.name == objectName)
                {
                    return text.text;
                }
            }

            Assert.Fail($"패널 아래에 {objectName} 글이 없다");
            return null;
        }

        private static void AssertInside(RectTransform card, RectTransform item)
        {
            var cardCorners = new Vector3[4];
            var itemCorners = new Vector3[4];
            card.GetWorldCorners(cardCorners);
            item.GetWorldCorners(itemCorners);

            const float tolerance = 0.5f;
            Assert.That(itemCorners[0].y, Is.GreaterThanOrEqualTo(cardCorners[0].y - tolerance), $"{item.name}의 아래가 카드 밖으로 나갔다");
            Assert.That(itemCorners[1].y, Is.LessThanOrEqualTo(cardCorners[1].y + tolerance), $"{item.name}의 위가 카드 밖으로 나갔다");
        }
    }
}
#endif
