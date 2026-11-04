using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 패널 안에서 ArmA(굴러가는 베이스)의 키보드 조작을 다루는 구간. 조작 켜기/끄기와 지금 자리 글을 내주고,
    /// 패널은 이 구간의 Update를 매 프레임 불러 준다. 키를 읽어 베이스를 움직이는 일은 ArmBaseKeyboardControl이 하고,
    /// 화면은 이 클래스를 모른다(ArmJointPanelView가 계층의 오브젝트로 보여 준다).
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

        /// <summary>끄면 키를 읽지 않는다. 글자를 입력하는 동안 방향키가 베이스를 움직이지 않게 할 때 끈다.</summary>
        public bool IsKeyboardEnabled
        {
            get => _control.IsEnabled;
            set => _control.IsEnabled = value;
        }

        /// <summary>패널에 적을 지금 자리 한 줄(루트 자리와 방향, 팔 베이스 자리).</summary>
        public string PoseText => BasePoseReport.Format(_control.Current, _control.ArmBasePosition);

        /// <summary>베이스 루트(ArmA)를 곧바로 이 자리와 방향에 놓는다. 높이는 지금 그대로다.</summary>
        public void PlaceAt(Vector3 rootPosition, float yawDeg)
        {
            _control.PlaceAt(rootPosition, yawDeg);
        }

        public void Update(float deltaSeconds)
        {
            _control.Update(deltaSeconds);
        }
    }
}
