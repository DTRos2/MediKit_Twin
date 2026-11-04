#if UNITY_EDITOR
using System;
using System.Collections;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static MediKitTwin.PlayMode.Tests.ProcessPanelReader;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 공정 현황 패널이 코드로 그려지는 것이 아니라 열린 씬(Scene_Cell)의 계층에 놓인 오브젝트인지, 그리고 공정 신호(CellEvents)를 받으면
    /// 단계 글, 설비 칩, 숫자 칸, 경고 글이 그 오브젝트에 나타나는지 본다. 설비 칩은 StationId 값마다 하나씩 있어야 한다.
    /// 글 문구와 색 규칙은 EditMode 시험(ProcessStatusTextTests, ProcessChipTonesTests, ProcessTallyTests)이 본다.
    /// 시험은 하나로 합쳤다: 이 클래스에 씬을 불러오는 시험을 둘 두면 둘째가 도구 카메라의 RenderTexture 오류로 실패한다(OrderPanelSceneTests 참고).
    /// </summary>
    public sealed class ProcessStatusPanelSceneTests
    {
        private const string WarningCode = "TEST";
        private const string WarningMessage = "시험 경고";

        [UnityTest, Timeout(120000)]
        public IEnumerator ThePanelIsInTheHierarchy_AndTheSignalsOfTheProcessFillIt()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();
            yield return CellTestScene.StopToolCameraFeeds();

            // 시험이 쏘는 신호와 상관없는 설비 신호가 섞이면 숫자가 어긋난다. 키팅은 멈춰 둔다.
            CellTestScene.DisableAll<KittingController>();

            var presenter = Object.FindAnyObjectByType<ProcessStatusPanelPresenter>();
            Assert.That(presenter, Is.Not.Null, "씬에 ProcessStatusPanelPresenter가 없다(Tools/MediKit/Setup Dashboard Panels로 올린다)");
            Assert.That(presenter.enabled, Is.True);
            Assert.That(presenter.GetComponentInParent<Canvas>(), Is.Not.Null, "패널이 캔버스 아래에 놓여 있어야 한다");

            ProcessStationChip[] chips = presenter.GetComponentsInChildren<ProcessStationChip>(true);
            Assert.That(chips, Has.Length.EqualTo(Enum.GetValues(typeof(StationId)).Length), "설비 칩은 설비마다 하나다");

            int objectCountBefore = presenter.GetComponentsInChildren<Transform>(true).Length;

            // 패널이 켜진 뒤 지나간 신호만 센다. 완료 한 건, 불량 한 건, 재고 부족 한 번, 정지 한 번, 경고 하나를 흘려 넣는다.
            var kit = new KitData(1, 1, "LOT-1", "병원", "BL");
            var rejected = new KitData(2, 2, "LOT-2", "병원", "BL");
            CellEvents.RaiseKitReady(kit);
            CellEvents.RaiseKitShipped(kit);
            CellEvents.RaiseKitRejected(rejected);
            CellEvents.RaiseStockEmpty("A-1", 1);
            CellEvents.RaiseStationStateChanged(StationId.Conveyor, StationState.Fault);
            CellEvents.RaiseWarning(WarningCode, WarningMessage);
            yield return null;
            yield return null;

            Assert.That(TextOfChip(chips, StationId.Conveyor), Is.EqualTo(ProcessStatusText.StationChip(StationId.Conveyor, StationState.Fault)));
            Assert.That(TileValue(presenter, "Completed"), Is.EqualTo("1"));
            Assert.That(TileValue(presenter, "Rejected"), Is.EqualTo("1"));
            Assert.That(TileValue(presenter, "StockShortage"), Is.EqualTo("1"));
            Assert.That(TileValue(presenter, "Fault"), Is.EqualTo("1"));
            Assert.That(TextOf(presenter, "Headline"), Is.EqualTo(ProcessStatusText.Stage(ProcessStage.Rejected)), "가장 뒤 키트의 단계가 큰 글자로 보인다");
            Assert.That(TextOf(presenter, "Warning"), Does.Contain(WarningMessage));
            Assert.That(
                presenter.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(objectCountBefore),
                "신호를 받아도 오브젝트를 새로 만들면 안 된다(계층에 놓인 오브젝트의 글과 색만 바꾼다)");

            yield return CellTestScene.StopToolCameraFeeds();
        }

        private static string TextOfChip(ProcessStationChip[] chips, StationId station)
        {
            foreach (ProcessStationChip chip in chips)
            {
                if (chip.Station == station)
                {
                    return chip.GetComponentInChildren<Text>().text;
                }
            }

            Assert.Fail($"{station} 칩이 패널에 없다");
            return null;
        }

    }
}
#endif
