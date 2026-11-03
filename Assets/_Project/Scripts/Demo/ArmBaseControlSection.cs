using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 패널 안에서 ArmA(굴러가는 베이스)의 키보드 조작을 안내하는 구간. 조작 켜기/끄기, 키 안내, 지금 자리를 보여 준다.
    /// 키를 읽어 베이스를 움직이는 일은 ArmBaseKeyboardControl이 하고, 패널은 이 구간의 Update를 매 프레임 불러 준다.
    /// </summary>
    public sealed class ArmBaseControlSection
    {
        private readonly ArmBaseKeyboardControl _control;

        private ArmBaseControlSection(ArmBaseKeyboardControl control)
        {
            _control = control;
        }

        /// <summary>
        /// 로봇이 실려 있는 베이스를 찾아 구간을 만든다. 베이스가 없거나(바닥에 고정된 팔),
        /// 베이스를 곧바로 옮길 수 없으면(IManualPlacement 없음) null이다.
        /// </summary>
        public static ArmBaseControlSection TryCreate(MonoBehaviour robot, float moveSpeedMps, float turnSpeedDegPerSec, float fastMultiplier)
        {
            IMobileBase mobile = MobileBaseResolver.Resolve(robot);
            if (!(mobile is IManualPlacement placement))
            {
                return null;
            }

            var motion = new ManualBaseMotion(moveSpeedMps, turnSpeedDegPerSec, fastMultiplier);
            return new ArmBaseControlSection(new ArmBaseKeyboardControl(mobile, placement, robot as IArmBaseLocator, motion, robot.name));
        }

        /// <summary>베이스 루트(ArmA)의 월드 위치.</summary>
        public Vector3 RootPosition => _control.Current.Position;

        /// <summary>베이스 루트가 지금 보는 방향(도).</summary>
        public float RootYawDeg => _control.Current.YawDeg;

        /// <summary>팔 베이스(어깨)의 월드 위치. 알려 줄 쪽이 없으면 null이다.</summary>
        public Vector3? ArmBasePosition => _control.ArmBasePosition;

        /// <summary>베이스 루트(ArmA)를 곧바로 이 자리와 방향에 놓는다. 높이는 지금 그대로다.</summary>
        public void PlaceAt(Vector3 rootPosition, float yawDeg)
        {
            _control.PlaceAt(rootPosition, yawDeg);
        }

        public void Update(float deltaSeconds)
        {
            _control.Update(deltaSeconds);
        }

        public void Draw()
        {
            DashboardSpacing.Section();
            _control.IsEnabled = SwitchRow.Draw(_control.IsEnabled, "키보드 조작", "글자 입력 중에는 꺼짐");
            DashboardSpacing.Row();
            DrawKeyGuide();
            DashboardSpacing.Row();
            DashboardText.Paragraph(BasePoseReport.Format(_control.Current, _control.ArmBasePosition), TextRole.Mono, DashboardPalette.Default.Muted);
        }

        // 키 모양 칸과 하는 일을 두 줄로 안내한다.
        private static void DrawKeyGuide()
        {
            using (DashboardRow.Begin())
            {
                KeyHint.Draw("↑↓←→", "앞뒤·옆 이동");
                KeyHint.Draw(", .", "회전");
                GUILayout.FlexibleSpace();
            }

            DashboardSpacing.Row();
            using (DashboardRow.Begin())
            {
                KeyHint.Draw("R-Shift", "빠르게");
                KeyHint.Draw("Enter", "위치 출력");
                GUILayout.FlexibleSpace();
            }
        }
    }
}
