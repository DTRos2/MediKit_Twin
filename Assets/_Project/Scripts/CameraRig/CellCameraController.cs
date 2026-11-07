using UnityEngine;
using UnityEngine.InputSystem;

namespace MediKitTwin.CameraRig
{
    /// <summary>
    /// 키보드와 마우스 오른쪽 버튼으로 카메라를 조작한다. (왼쪽 버튼은 UI 클릭용으로 비워 둔다)
    /// 키보드(항상): W/A/S/D = 앞뒤좌우, E/Q = 위/아래, Shift = 빠르게.
    /// 이동 키를 누르는 동안: 마우스 휠 = 이동 속도 조절. 오른쪽 버튼을 누른 채: 마우스 이동 = 시점 회전.
    /// 시간은 실제 시간(Time.unscaledDeltaTime)을 쓰므로 일시정지나 배속 중에도 움직인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CellCameraController : MonoBehaviour
    {
        [Header("회전")]
        [Tooltip("마우스 1픽셀당 회전 각도(도)")]
        [SerializeField] private float lookSensitivity = 0.15f;

        [Header("이동")]
        [Tooltip("기본 이동 속도(m/s)")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float minMoveSpeed = 0.5f;
        [SerializeField] private float maxMoveSpeed = 30f;
        [Tooltip("Shift를 누르면 이동 속도에 곱하는 값")]
        [SerializeField] private float fastMultiplier = 3f;
        [Tooltip("휠 한 칸당 이동 속도에 곱하는 값")]
        [SerializeField] private float scrollSpeedFactor = 1.2f;

        private float yaw;
        private float pitch;
        private bool looking;

        private void OnEnable()
        {
            // 현재 카메라 방향에서 이어서 돌리도록 시작 각도를 읽는다.
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        private void OnDisable()
        {
            SetLooking(false);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // 창을 벗어났다가 돌아올 때 커서가 잠긴 채로 남지 않게 한다.
            if (!hasFocus) SetLooking(false);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) return;

            // 키보드 이동은 항상 가능하다. 이동 키를 누르는 동안에는 휠로 속도를 조절하고,
            // 시점 회전은 오른쪽 버튼을 누르는 동안만 한다.
            if (Move(keyboard))
                AdjustSpeed(mouse.scroll.ReadValue().y);

            SetLooking(mouse.rightButton.isPressed);
            if (looking)
                Look(mouse.delta.ReadValue());
        }

        private void Look(Vector2 delta)
        {
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void AdjustSpeed(float scroll)
        {
            if (Mathf.Approximately(scroll, 0f)) return;

            float factor = scroll > 0f ? scrollSpeedFactor : 1f / scrollSpeedFactor;
            moveSpeed = Mathf.Clamp(moveSpeed * factor, minMoveSpeed, maxMoveSpeed);
        }

        // 이동 키가 눌려 있으면 true를 돌려준다.
        private bool Move(Keyboard keyboard)
        {
            Vector3 local = new Vector3(
                Axis(keyboard.dKey, keyboard.aKey),
                0f,
                Axis(keyboard.wKey, keyboard.sKey));
            float vertical = Axis(keyboard.eKey, keyboard.qKey);
            if (local == Vector3.zero && vertical == 0f) return false;

            Vector3 direction = transform.TransformDirection(local) + Vector3.up * vertical;

            float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? fastMultiplier : 1f);
            transform.position += Vector3.ClampMagnitude(direction, 1f) * (speed * Time.unscaledDeltaTime);
            return true;
        }

        private static float Axis(UnityEngine.InputSystem.Controls.KeyControl positive, UnityEngine.InputSystem.Controls.KeyControl negative)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        // 조작하는 동안에는 커서를 숨기고 잠가서 화면 가장자리에서 멈추지 않게 한다.
        private void SetLooking(bool value)
        {
            if (looking == value) return;

            looking = value;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
        }
    }
}
