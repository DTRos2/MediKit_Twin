#if UNITY_EDITOR
using System.Collections;
using MediKitTwin.ArmA;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using MediKitTwin.Orders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 주문 패널이 코드로 그려지는 것이 아니라 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지, 그리고 그 오브젝트로 주문을 넣으면
    /// 주문 줄, 요약, 경고가 화면 오브젝트에 나타나는지 본다. 마우스로 누르는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로,
    /// 버튼이 누를 때 부르는 접점(onClick)을 직접 부른다.
    /// 글자 문구와 칩 색, 앵커 변환 같은 순수 규칙은 EditMode 시험(OrderPanelTextsTests, OrderChipTonesTests, DemoPanelRectAnchorTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(ConveyorSpeedControlTests 참고).
    /// </summary>
    public sealed class OrderPanelSceneTests
    {
        private const string SampleOrders = "1,A병원,EM,2\n2,B병원,DX,3";
        private const string BrokenLine = "이 줄은 주문이 아니다";
        private const int ButtonCount = 3;

        [UnityTest, Timeout(120000)]
        public IEnumerator ThePanelIsInTheHierarchy_AndApplyingOrdersFillsItsRowsSummaryAndWarnings()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            // 주문이 들어오자마자 로봇이 움직이면 시험과 상관없는 물리가 돈다. 키팅은 멈춰 둔다.
            CellTestScene.DisableAll<KittingController>();

            var presenter = Object.FindAnyObjectByType<OrderPanelPresenter>();
            Assert.That(presenter, Is.Not.Null, "씬에 OrderPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
            Assert.That(presenter.enabled, Is.True, "패널이 주문 입력을 찾지 못해 꺼졌다");

            AssertItIsPlacedInTheCanvasAndClickable(presenter);

            var view = presenter.GetComponent<OrderPanelView>();
            Button apply = FindButton(presenter, OrderPanelTexts.ApplyButton);
            Button sample = FindButton(presenter, OrderPanelTexts.SampleButton);
            Assert.That(presenter.GetComponentsInChildren<Button>(), Has.Length.EqualTo(ButtonCount), "버튼은 적용, 샘플 채우기, 파일 읽기 세 개다");

            // 시작하면 샘플이 입력창에 들어 있고, "샘플 채우기"로 다시 채울 수 있다.
            view.CsvText = string.Empty;
            sample.onClick.Invoke();
            Assert.That(view.CsvText, Is.Not.Empty, "샘플 채우기를 누르면 입력창에 샘플 주문이 들어가야 한다");

            // 올바른 주문만 넣으면 줄이 생기고 경고가 없다.
            view.CsvText = SampleOrders;
            apply.onClick.Invoke();
            yield return null;
            yield return null;

            IOrderInput input = CellTestScene.FindOne<IOrderInput>();
            Assert.That(input.LastIntake.Orders, Has.Count.EqualTo(2));
            Assert.That(ActiveRows<OrderRowItem>(presenter), Has.Length.EqualTo(2), "주문 줄이 주문 수만큼 보여야 한다");
            Assert.That(ActiveRows<UiTextRow>(presenter), Is.Empty, "경고가 없으면 경고 줄이 없어야 한다");
            Assert.That(TextOf(presenter, "Summary"), Is.EqualTo(OrderPanelTexts.Summary(2, input.LastIntake.KitRequests.Count)));
            Assert.That(TextOf(presenter, "IssueChip"), Is.EqualTo(OrderPanelTexts.Issues(0)));

            // 깨진 줄이 섞이면 그 줄은 건너뛰고 경고로 보인다.
            view.CsvText = SampleOrders + "\n" + BrokenLine;
            apply.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(input.LastIntake.Orders, Has.Count.EqualTo(2), "깨진 줄은 건너뛴다");
            Assert.That(input.LastIntake.Issues, Is.Not.Empty, "깨진 줄은 경고가 된다");
            Assert.That(ActiveRows<UiTextRow>(presenter), Has.Length.EqualTo(input.LastIntake.Issues.Count), "경고 줄이 경고 수만큼 보여야 한다");
            Assert.That(TextOf(presenter, "IssueChip"), Is.EqualTo(OrderPanelTexts.Issues(input.LastIntake.Issues.Count)));

            // 미리 놓아 둔 줄보다 주문이 많으면 줄을 새로 만들지 않고, 못 보인 개수를 "외 N건 더"로 알린다.
            int objectCountBefore = presenter.GetComponentsInChildren<Transform>(true).Length;
            int rowCapacity = presenter.GetComponentsInChildren<OrderRowItem>(true).Length;
            int extraOrders = 3;
            view.CsvText = ManyOrders(rowCapacity + extraOrders);
            apply.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(ActiveRows<OrderRowItem>(presenter), Has.Length.EqualTo(rowCapacity), "줄은 미리 놓아 둔 수까지만 보인다");
            Assert.That(TextOf(presenter, "Overflow"), Is.EqualTo(OrderPanelTexts.Overflow(extraOrders)), "못 보인 주문 수를 알려야 한다");
            Assert.That(
                presenter.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(objectCountBefore),
                "실행 중에 오브젝트를 새로 만들면 안 된다(계층에 미리 놓은 줄만 켜고 끈다)");

            yield return CellTestScene.StopToolCameraFeeds();
        }

        private static string ManyOrders(int count)
        {
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 1; i <= count; i++)
            {
                lines.Add($"{i},A병원,EM,1");
            }

            return string.Join("\n", lines);
        }

        private static void AssertItIsPlacedInTheCanvasAndClickable(OrderPanelPresenter presenter)
        {
            Canvas canvas = presenter.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null, "캔버스에 눌림을 받는 GraphicRaycaster가 있어야 한다");
            Assert.That(Object.FindAnyObjectByType<EventSystem>(), Is.Not.Null, "씬에 EventSystem이 있어야 버튼이 눌린다");
        }

        private static Button FindButton(OrderPanelPresenter presenter, string label)
        {
            foreach (Button button in presenter.GetComponentsInChildren<Button>())
            {
                if (button.GetComponentInChildren<Text>().text == label)
                {
                    return button;
                }
            }

            Assert.Fail($"'{label}' 버튼이 패널에 없다");
            return null;
        }

        // 꺼 둔 줄은 빼고 켜진 줄만 센다. GetComponentsInChildren은 기본으로 켜진 오브젝트만 찾는다.
        private static T[] ActiveRows<T>(OrderPanelPresenter presenter)
            where T : Component
        {
            return presenter.GetComponentsInChildren<T>();
        }

        private static string TextOf(OrderPanelPresenter presenter, string objectName)
        {
            foreach (Text text in presenter.GetComponentsInChildren<Text>(true))
            {
                if (text.transform.name == objectName || text.transform.parent.name == objectName)
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
