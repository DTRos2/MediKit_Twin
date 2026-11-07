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
    /// 선반 재고 패널이 코드로 그려지는 것이 아니라 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지, 그리고 칸을 누르면 재고가 줄고
    /// 되돌리기 모드에서는 늘며, 칸 타일의 글과 안내 글이 따라 바뀌는지 본다. 칸 타일은 선반의 칸 ID마다 하나씩 있어야 한다.
    /// 마우스로 누르는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로, 버튼이 누를 때 부르는 접점(onClick)을 직접 부른다.
    /// 집기 규칙과 문구는 EditMode 시험(ShelfStockActionsTests, ShelfStockPanelTextsTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class ShelfStockPanelSceneTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator ThePanelIsInTheHierarchy_AndPressingASlotTakesOrReturnsOnePiece()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            // 키팅이 같은 칸을 집어 가면 시험이 세는 수량이 어긋난다. 키팅은 멈춰 둔다.
            CellTestScene.DisableAll<KittingController>();

            var presenter = Object.FindAnyObjectByType<ShelfStockPanelPresenter>();
            Assert.That(presenter, Is.Not.Null, "씬에 ShelfStockPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
            Assert.That(presenter.enabled, Is.True, "패널이 재고를 찾지 못해 꺼졌다");
            Assert.That(presenter.GetComponentInParent<Canvas>(), Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");

            IStock stock = CellTestScene.FindOne<IStock>();
            Assert.That(stock, Is.Not.Null);
            ShelfSlotTile[] tiles = presenter.GetComponentsInChildren<ShelfSlotTile>(true);
            Assert.That(tiles, Has.Length.EqualTo(stock.ShelfSlotIds.Count), "칸 타일은 선반의 칸마다 하나다");
            foreach (string slotId in stock.ShelfSlotIds)
            {
                Assert.That(FindTile(tiles, slotId), Is.Not.Null, $"{slotId} 칸 타일이 없다");
            }

            yield return null;
            yield return null;
            int objectCountBefore = presenter.GetComponentsInChildren<Transform>(true).Length;

            // 재고가 있는 칸을 하나 골라 누르면 하나 줄고, 타일 숫자와 안내 글이 따라 바뀐다.
            ShelfSlotTile tile = FindTileWithStock(tiles, stock);
            Assert.That(tile, Is.Not.Null, "재고가 있는 칸이 하나도 없다");
            string slot = tile.SlotId;
            int before = stock.GetQuantity(slot);

            tile.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(stock.GetQuantity(slot), Is.EqualTo(before - 1), "칸을 누르면 하나 집혀야 한다");
            Assert.That(TextIn(tile, "Quantity"), Is.EqualTo((before - 1).ToString()));
            Assert.That(TextOf(presenter, "Message"), Is.EqualTo(ShelfStockPanelTexts.Taken(slot, before - 1)));
            Assert.That(TextOf(presenter, "Summary"), Is.EqualTo(ShelfStockPanelTexts.Total(TotalOf(stock))));

            // 되돌리기 모드를 켜고 같은 칸을 누르면 하나 돌아온다.
            presenter.GetComponentsInChildren<UiSwitchRow>(true)[SwitchIndex(presenter, "ReturnSwitch")].GetComponent<Button>().onClick.Invoke();
            yield return null;
            tile.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(stock.GetQuantity(slot), Is.EqualTo(before), "되돌리기 모드에서는 칸을 누르면 하나 돌아와야 한다");
            Assert.That(TextOf(presenter, "Message"), Is.EqualTo(ShelfStockPanelTexts.Returned(slot, before)));

            // 수동 키팅이 있으면 수동 스위치로 키트 종류 버튼 줄이 열리고 되돌리기 스위치는 가려진다. 다시 누르면 원래대로 돌아온다.
            IManualKitting manual = CellTestScene.FindOne<IManualKitting>();
            if (manual != null)
            {
                UiSwitchRow manualSwitch = presenter.GetComponentsInChildren<UiSwitchRow>(true)[SwitchIndex(presenter, "ManualSwitch")];
                Assert.That(manualSwitch.gameObject.activeSelf, Is.True, "수동 키팅이 있으면 수동 스위치가 보여야 한다");

                manualSwitch.GetComponent<Button>().onClick.Invoke();
                yield return null;
                yield return null;
                Assert.That(manual.IsManualMode, Is.True);
                Assert.That(FindChild(presenter, "ManualOperations").activeSelf, Is.True, "수동 모드에서는 키트 종류 버튼 줄이 보여야 한다");
                Assert.That(FindChild(presenter, "ReturnSwitch").activeSelf, Is.False, "수동 모드에서는 되돌리기 스위치가 가려진다");

                manualSwitch.GetComponent<Button>().onClick.Invoke();
                yield return null;
                yield return null;
                Assert.That(manual.IsManualMode, Is.False);
                Assert.That(FindChild(presenter, "ManualOperations").activeSelf, Is.False);
            }

            Assert.That(
                presenter.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(objectCountBefore),
                "시험하는 동안 오브젝트를 새로 만들면 안 된다(계층에 놓인 오브젝트의 글과 색만 바꾼다)");

            yield return CellTestScene.StopToolCameraFeeds();
        }

        private static ShelfSlotTile FindTile(ShelfSlotTile[] tiles, string slotId)
        {
            foreach (ShelfSlotTile tile in tiles)
            {
                if (tile.SlotId == slotId)
                {
                    return tile;
                }
            }

            return null;
        }

        private static ShelfSlotTile FindTileWithStock(ShelfSlotTile[] tiles, IStock stock)
        {
            foreach (ShelfSlotTile tile in tiles)
            {
                if (stock.GetQuantity(tile.SlotId) > 0)
                {
                    return tile;
                }
            }

            return null;
        }

        private static int TotalOf(IStock stock)
        {
            int total = 0;
            foreach (string slotId in stock.ShelfSlotIds)
            {
                total += stock.GetQuantity(slotId);
            }

            return total;
        }

        private static int SwitchIndex(ShelfStockPanelPresenter presenter, string objectName)
        {
            UiSwitchRow[] switches = presenter.GetComponentsInChildren<UiSwitchRow>(true);
            for (int i = 0; i < switches.Length; i++)
            {
                if (switches[i].name == objectName)
                {
                    return i;
                }
            }

            Assert.Fail($"스위치 줄 '{objectName}'이 패널에 없다");
            return -1;
        }

        private static GameObject FindChild(ShelfStockPanelPresenter presenter, string objectName)
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

        private static string TextOf(ShelfStockPanelPresenter presenter, string objectName)
        {
            return TextIn(presenter, objectName);
        }
    }
}
#endif
