using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Demo;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 관절 제어 패널 프리팹(PF_ArmJointPanel)을 만든다. 머리줄(로봇 이름 칩, 접기 표시), 수동 제어 스위치, 관절 슬라이더 줄,
    /// 자세 버튼, 베이스 키보드 구간, 도구 끝점 글, 집는 자세 저장 구간이 모두 계층의 오브젝트로 들어가고,
    /// 화면 코드(ArmJointPanelView)와 동작 코드(ArmJointPanelPresenter)가 그 오브젝트들을 연결한다.
    /// 관절 줄은 로봇의 관절 수만큼 켜므로 넉넉히(ArmJointPanelTexts.JointRowCapacity) 미리 놓는다.
    /// 위치와 처음 접힘은 IMGUI 시절과 같은 자리 표(DemoPanelSlots.Joints)를 쓴다. 만든 뒤에는 프리팹을 직접 열어 고치면 되고,
    /// 이 빌더를 다시 돌리면 처음 모양으로 돌아간다.
    /// </summary>
    public static class ArmJointPanelPrefabBuilder
    {
        private const string RootName = "PF_ArmJointPanel";

        public static ArmJointPanelPresenter Build()
        {
            DashboardPalette palette = DashboardPalette.Default;
            DemoPanelSlot slot = DemoPanelSlots.Joints;

            UiPanelFrame.Frame frame = UiPanelFrame.Build(RootName, slot, ArmJointPanelTexts.Title, ArmJointPanelTexts.Subtitle);
            UiChip robotChip = UiWidgets.AddChip(frame.Header, "RobotChip", string.Empty, ChipTone.Neutral, hasDot: false, isClickable: true);
            UiWidgets.ScrollView body = UiPanelFrame.AddScrollBody(frame, DashboardMetrics.BlockGap);
            UiCollapsiblePanel panel = UiPanelFrame.AddCollapse(frame, body.Root.gameObject, slot.StartsCollapsed);

            Text notReady = UiPrimitives.AddLabel(body.Content, "NotReady", ArmJointPanelTexts.NotReady, TextRole.Body, palette.Muted);

            // 로봇이 준비된 뒤에만 보이는 본문 전체.
            RectTransform ready = UiPrimitives.CreateRect("Ready", body.Content);
            UiPrimitives.AddColumn(ready, DashboardMetrics.BlockGap);

            UiSwitchRow manualSwitch = UiWidgets.AddSwitchRow(ready, "ManualSwitch");
            for (int i = 0; i < ArmJointPanelTexts.JointRowCapacity; i++)
            {
                UiWidgets.AddSliderRow(ready, $"JointRow_{i + 1}", $"J{i + 1}");
            }

            RectTransform shortcuts = UiPrimitives.CreateRect("Shortcuts", ready);
            UiPrimitives.AddRow(shortcuts, DashboardMetrics.BlockGap);
            UiPrimitives.SetLayout(shortcuts, preferredHeight: DashboardMetrics.ControlHeight);
            UiKindButton homeButton = UiWidgets.AddKindButton(
                shortcuts, "HomeButton", ArmJointPanelTexts.HomePose, ButtonKind.Secondary, DashboardMetrics.ControlHeight);
            UiKindButton matchButton = UiWidgets.AddKindButton(
                shortcuts, "MatchButton", ArmJointPanelTexts.MatchCurrent, ButtonKind.Secondary, DashboardMetrics.ControlHeight);

            RectTransform baseSection = UiWidgets.AddSection(ready, "BaseSection", string.Empty, DashboardMetrics.BlockGap);
            UiSwitchRow keyboardSwitch = UiWidgets.AddSwitchRow(baseSection, "KeyboardSwitch");
            AddKeyRow(
                baseSection, "KeyRow_Move",
                (ArmJointPanelTexts.MoveKeys, ArmJointPanelTexts.MoveMeaning), (ArmJointPanelTexts.TurnKeys, ArmJointPanelTexts.TurnMeaning));
            AddKeyRow(
                baseSection, "KeyRow_Modifier",
                (ArmJointPanelTexts.FastKey, ArmJointPanelTexts.FastMeaning), (ArmJointPanelTexts.LogKey, ArmJointPanelTexts.LogMeaning));
            Text basePose = UiPrimitives.AddLabel(
                baseSection, "BasePose", string.Empty, TextRole.Mono, palette.Muted, TextAnchor.UpperLeft, isWrapped: true);

            RectTransform toolSection = UiWidgets.AddSection(ready, "ToolSection", string.Empty, DashboardMetrics.RowGap);
            Text toolPosition = AddToolLine(toolSection, "ToolPosition");
            Text toolApproach = AddToolLine(toolSection, "ToolApproach");
            Text toolEuler = AddToolLine(toolSection, "ToolEuler");

            RectTransform taughtSection = UiWidgets.AddSection(ready, "TaughtSection", ArmJointPanelTexts.TaughtHeading, DashboardMetrics.BlockGap);
            UiPrimitives.AddLabel(
                taughtSection, "Description", ArmJointPanelTexts.TaughtDescription, TextRole.Small, palette.Muted, TextAnchor.UpperLeft, isWrapped: true);

            RectTransform slotRow = UiPrimitives.CreateRect("SlotRow", taughtSection);
            UiPrimitives.AddRow(slotRow, DashboardMetrics.BlockGap);
            UiPrimitives.SetLayout(slotRow, preferredHeight: DashboardMetrics.ControlHeight);
            UiPrimitives.AddLabel(slotRow, "SlotLabel", ArmJointPanelTexts.SlotLabel, TextRole.Small, palette.Muted);
            InputField slotInput = UiWidgets.AddSingleLineInput(slotRow, "SlotIdInput", DashboardMetrics.ShortFieldWidth, DashboardMetrics.ControlHeight);
            UiKindButton saveButton = UiWidgets.AddKindButton(
                slotRow, "SaveButton", ArmJointPanelTexts.Save, ButtonKind.Secondary, DashboardMetrics.ControlHeight);
            UiKindButton loadButton = UiWidgets.AddKindButton(
                slotRow, "LoadButton", ArmJointPanelTexts.Load, ButtonKind.Secondary, DashboardMetrics.ControlHeight);
            UiKindButton removeButton = UiWidgets.AddKindButton(
                slotRow, "RemoveButton", ArmJointPanelTexts.Remove, ButtonKind.Secondary, DashboardMetrics.ControlHeight);

            Text savedText = UiPrimitives.AddLabel(
                taughtSection, "SavedText", string.Empty, TextRole.Small, palette.Muted, TextAnchor.UpperLeft, isWrapped: true);
            Text taughtMessage = UiPrimitives.AddLabel(
                taughtSection, "Message", string.Empty, TextRole.Small, palette.InkSoft, TextAnchor.UpperLeft, isWrapped: true);

            var view = frame.Root.AddComponent<ArmJointPanelView>();
            SerializedFieldAssigner.SetReference(view, "_panel", panel);
            SerializedFieldAssigner.SetReference(view, "_robotChip", robotChip);
            SerializedFieldAssigner.SetReference(view, "_notReadyText", notReady);
            SerializedFieldAssigner.SetReference(view, "_readyContent", ready.gameObject);
            SerializedFieldAssigner.SetReference(view, "_manualSwitch", manualSwitch);
            SerializedFieldAssigner.SetReference(view, "_homeButton", homeButton);
            SerializedFieldAssigner.SetReference(view, "_matchButton", matchButton);
            SerializedFieldAssigner.SetReference(view, "_baseSection", baseSection.gameObject);
            SerializedFieldAssigner.SetReference(view, "_keyboardSwitch", keyboardSwitch);
            SerializedFieldAssigner.SetReference(view, "_basePose", basePose);
            SerializedFieldAssigner.SetReference(view, "_toolPosition", toolPosition);
            SerializedFieldAssigner.SetReference(view, "_toolApproach", toolApproach);
            SerializedFieldAssigner.SetReference(view, "_toolEuler", toolEuler);
            SerializedFieldAssigner.SetReference(view, "_taughtSection", taughtSection.gameObject);
            SerializedFieldAssigner.SetReference(view, "_slotInput", slotInput);
            SerializedFieldAssigner.SetReference(view, "_saveButton", saveButton);
            SerializedFieldAssigner.SetReference(view, "_loadButton", loadButton);
            SerializedFieldAssigner.SetReference(view, "_removeButton", removeButton);
            SerializedFieldAssigner.SetReference(view, "_savedText", savedText);
            SerializedFieldAssigner.SetReference(view, "_taughtMessage", taughtMessage);

            var presenter = frame.Root.AddComponent<ArmJointPanelPresenter>();
            SerializedFieldAssigner.SetReference(presenter, "_view", view);

            return UiPrefabWriter.SaveAndLoad<ArmJointPanelPresenter>(frame.Root, ProjectAssetPaths.ArmJointPanelPrefab);
        }

        // 키 안내 한 줄: 키 칸과 뜻 한 쌍이 가로로 두 개 놓인다.
        private static void AddKeyRow(Transform parent, string name, (string Key, string Meaning) first, (string Key, string Meaning) second)
        {
            RectTransform row = UiPrimitives.CreateRect(name, parent);
            UiPrimitives.AddRow(row, DashboardMetrics.KeyHintGap);
            UiPrimitives.SetLayout(row, preferredHeight: DashboardMetrics.KeyCapHeight);
            UiWidgets.AddKeyHint(row, first.Key, first.Meaning);
            UiWidgets.AddKeyHint(row, second.Key, second.Meaning);
        }

        private static Text AddToolLine(Transform parent, string name)
        {
            return UiPrimitives.AddLabel(
                parent, name, string.Empty, TextRole.Mono, DashboardPalette.Default.Muted, TextAnchor.UpperLeft, isWrapped: true);
        }
    }
}
