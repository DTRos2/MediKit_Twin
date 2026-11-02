using MediKitTwin.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키보드로 ArmA(굴러가는 베이스)를 손으로 움직이고, 자리를 Console에 적는다. 카메라가 쓰는 W/A/S/D/Q/E와 겹치지 않도록
    /// 방향키 = 앞뒤·좌우(베이스가 보는 방향 기준), , / . = 왼쪽·오른쪽으로 돌기, 오른쪽 Shift = 빠르게를 쓴다.
    /// 키를 떼어 멈추면 그때의 자리를 한 번 적고, Enter를 누르면 언제든 지금 자리를 적는다.
    /// 이동 계산은 ManualBaseMotion, 글자 만들기는 BasePoseReport가 맡고 여기서는 키를 읽어 잇기만 한다.
    /// </summary>
    public sealed class ArmBaseKeyboardControl
    {
        private readonly IMobileBase _mobile;
        private readonly IManualPlacement _placement;
        private readonly IArmBaseLocator _armBase;
        private readonly ManualBaseMotion _motion;
        private readonly string _label;

        private bool _wasMoving;

        public ArmBaseKeyboardControl(
            IMobileBase mobile, IManualPlacement placement, IArmBaseLocator armBase, ManualBaseMotion motion, string label)
        {
            _mobile = mobile;
            _placement = placement;
            _armBase = armBase;
            _motion = motion;
            _label = label;
        }

        /// <summary>끄면 키를 읽지 않는다. 글자를 입력하는 동안 방향키가 베이스를 움직이지 않게 할 때 끈다.</summary>
        public bool IsEnabled { get; set; } = true;

        public BasePlacement Current => new BasePlacement(_mobile.Position, _mobile.YawDeg);

        /// <summary>팔 베이스(어깨)의 월드 위치. 알려 줄 쪽이 없으면 null이다.</summary>
        public Vector3? ArmBasePosition => _armBase != null ? _armBase.BaseWorldPosition : (Vector3?)null;

        /// <summary>매 프레임 부른다. 시간은 배속의 영향을 받지 않는 실제 시간을 넘긴다.</summary>
        public void Update(float deltaSeconds)
        {
            Keyboard keyboard = Keyboard.current;
            if (!IsEnabled || keyboard == null)
            {
                _wasMoving = false;
                return;
            }

            BaseMoveInput input = ReadInput(keyboard);
            if (!input.IsIdle)
            {
                BasePlacement next = _motion.Advance(Current, input, deltaSeconds);
                _placement.PlaceAt(next.Position, next.YawDeg);
                _wasMoving = true;
            }
            else if (_wasMoving)
            {
                _wasMoving = false;
                LogPosition();
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                LogPosition();
            }
        }

        /// <summary>키 입력 없이 베이스(루트)를 곧바로 이 자리와 방향에 놓는다. 진행 중이던 이동은 취소된다.</summary>
        public void PlaceAt(Vector3 rootPosition, float yawDeg)
        {
            _wasMoving = false;
            _placement.PlaceAt(rootPosition, yawDeg);
        }

        public void LogPosition()
        {
            Debug.Log($"{_label} ArmA 위치: {BasePoseReport.Format(Current, ArmBasePosition)}");
        }

        private static BaseMoveInput ReadInput(Keyboard keyboard)
        {
            return new BaseMoveInput(
                Axis(keyboard.upArrowKey, keyboard.downArrowKey),
                Axis(keyboard.rightArrowKey, keyboard.leftArrowKey),
                Axis(keyboard.periodKey, keyboard.commaKey),
                keyboard.rightShiftKey.isPressed);
        }

        private static float Axis(KeyControl positive, KeyControl negative)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }
    }
}
