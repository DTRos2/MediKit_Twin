#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConveyorSystem;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 팩킹 실패(CellEvents.PackingFailed)가 났을 때 사용자 대신 움직이는 시험 도구(FR-10). 실패가 나면 사용자가 알아차릴 만큼(ReactionSeconds) 기다린 뒤
    /// 그때의 모습(경고 패널이 떴는지, 컨베이어와 팔 A가 멈췄는지)을 적고 패널의 "확인하고 다시 덮기" 버튼을 누른다. 뚜껑이 덮이면(LidClosed)
    /// 다시 ReactionSeconds 뒤에 이어서 움직이는지(패널이 감춰지고, 컨베이어가 돌고, 팔 A가 풀렸는지)를 적는다.
    /// 시험 루프가 매 프레임 Tick을 불러 주어야 하고, 끝나면(Dispose) 구독을 풀어 다음 시험이 이 시험의 알림을 받지 않게 한다.
    /// </summary>
    internal sealed class PackingFailureUser : IDisposable
    {
        // 사용자가 경고를 알아차리고 버튼을 누르기까지의 시뮬레이션 시간(초). 컨베이어와 팔 A가 멈출 틈도 이 안에 있다.
        private const float ReactionSeconds = 2f;

        private readonly List<Case> _cases = new List<Case>();

        public PackingFailureUser()
        {
            CellEvents.PackingFailed += OnPackingFailed;
            CellEvents.LidClosed += OnLidClosed;
        }

        /// <summary>팩킹 실패 한 번의 경과와 그때 본 모습.</summary>
        internal sealed class Case
        {
            public Case(KitData kit, string reason, float failedAt)
            {
                Kit = kit;
                Reason = reason;
                FailedAt = failedAt;
            }

            public KitData Kit { get; }

            public string Reason { get; }

            public float FailedAt { get; }

            // 실패 뒤 버튼을 누르기 직전에 본 모습.
            public bool PanelShown { get; set; }

            public int ConveyorCount { get; set; }

            public int MovingConveyors { get; set; }

            public bool ArmAFrozen { get; set; }

            public bool Clicked { get; set; }

            public bool PanelMissing { get; set; }

            // 뚜껑이 덮이고 나서 본 모습.
            public float ClosedAt { get; set; } = -1f;

            public bool ResumeObserved { get; set; }

            public bool PanelHiddenAfter { get; set; }

            public int MovingConveyorsAfter { get; set; }

            public bool ArmAFrozenAfter { get; set; }
        }

        public IReadOnlyList<Case> Cases => _cases;

        public void Tick()
        {
            foreach (Case failure in _cases)
            {
                if (!failure.Clicked && Time.time >= failure.FailedAt + ReactionSeconds)
                {
                    ObserveHalted(failure);
                    Click(failure);
                }
                else if (failure.Clicked && failure.ClosedAt >= 0f && !failure.ResumeObserved && Time.time >= failure.ClosedAt + ReactionSeconds)
                {
                    ObserveResumed(failure);
                }
            }
        }

        public string Describe()
        {
            if (_cases.Count == 0)
            {
                return "[FullProcess] 팩킹 실패: 없었다";
            }

            var text = new StringBuilder("[FullProcess] 팩킹 실패:");
            foreach (Case failure in _cases)
            {
                text.Append($"\n  키트 {failure.Kit?.KitSequence}번 {failure.FailedAt:0.0}초 \"{failure.Reason}\" — ")
                    .Append($"멈춘 모습: 패널 {(failure.PanelShown ? "뜸" : "안 뜸")}, 움직이는 컨베이어 {failure.MovingConveyors}/{failure.ConveyorCount}개, ")
                    .Append($"팔 A {(failure.ArmAFrozen ? "멈춤" : "안 멈춤")}, 버튼 {(failure.Clicked ? "누름" : "안 누름")}");
                if (failure.ResumeObserved)
                {
                    text.Append($" / 재개한 모습({failure.ClosedAt:0.0}초 뚜껑 덮임): 패널 {(failure.PanelHiddenAfter ? "감춤" : "그대로 뜸")}, ")
                        .Append($"움직이는 컨베이어 {failure.MovingConveyorsAfter}개, 팔 A {(failure.ArmAFrozenAfter ? "멈춘 채" : "풀림")}");
                }
                else
                {
                    text.Append(failure.ClosedAt >= 0f ? " / 뚜껑은 덮였지만 재개한 모습은 못 봄" : " / 뚜껑이 덮이지 않음");
                }
            }

            return text.ToString();
        }

        public void Dispose()
        {
            CellEvents.PackingFailed -= OnPackingFailed;
            CellEvents.LidClosed -= OnLidClosed;
        }

        private void OnPackingFailed(KitData kit, string reason)
        {
            _cases.Add(new Case(kit, reason, Time.time));
        }

        private void OnLidClosed(KitData kit)
        {
            Case open = _cases.LastOrDefault(failure => failure.Clicked && failure.ClosedAt < 0f);
            if (open != null)
            {
                open.ClosedAt = Time.time;
            }
        }

        private static PackingFailureAlertPanelPresenter FindPanel()
        {
            return Object.FindAnyObjectByType<PackingFailureAlertPanelPresenter>(FindObjectsInactive.Include);
        }

        private static bool IsShown(PackingFailureAlertPanelPresenter panel)
        {
            return panel != null && panel.GetComponent<PackingFailureAlertPanelView>().IsShown;
        }

        private static int CountMovingConveyors(out int total)
        {
            ConveyorBase[] conveyors = Object.FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include);
            total = conveyors.Length;
            return conveyors.Count(conveyor => conveyor.Speed > 0f);
        }

        private static bool IsArmAFrozen()
        {
            var controller = Object.FindAnyObjectByType<KittingController>();
            return controller != null && controller.IsFrozen;
        }

        private static void ObserveHalted(Case failure)
        {
            PackingFailureAlertPanelPresenter panel = FindPanel();
            failure.PanelMissing = panel == null;
            failure.PanelShown = IsShown(panel);
            failure.MovingConveyors = CountMovingConveyors(out int total);
            failure.ConveyorCount = total;
            failure.ArmAFrozen = IsArmAFrozen();
        }

        private static void ObserveResumed(Case failure)
        {
            failure.PanelHiddenAfter = !IsShown(FindPanel());
            failure.MovingConveyorsAfter = CountMovingConveyors(out _);
            failure.ArmAFrozenAfter = IsArmAFrozen();
            failure.ResumeObserved = true;
        }

        // 마우스로 누르는 일은 그릴 수 없는 배치 모드에서 볼 수 없으므로 버튼이 누를 때 부르는 접점(onClick)을 직접 부른다.
        private static void Click(Case failure)
        {
            PackingFailureAlertPanelPresenter panel = FindPanel();
            UiKindButton button = panel != null ? panel.GetComponentInChildren<UiKindButton>(true) : null;
            if (button == null)
            {
                failure.PanelMissing = true;
                failure.Clicked = true;
                return;
            }

            failure.Clicked = true;
            button.Button.onClick.Invoke();
        }
    }
}
#endif
