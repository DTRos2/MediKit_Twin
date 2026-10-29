using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 집기 동작을 눈으로 확인하는 시험 패널. 화면 버튼(IMGUI)이라 Input 설정과 무관하게 동작한다.
    /// 제품 코드(ArmA)와 분리된 어셈블리에 두어서, 빌드에서 이 어셈블리만 빼면 시험 도구가 함께 빠진다.
    /// 구체 클래스가 아니라 인터페이스(IArmDriver, IToolPoseTarget, IGripper)로 로봇을 다룬다.
    /// </summary>
    public class ArmDemoPanel : MonoBehaviour
    {
        [SerializeField] private string _title = "Arm Demo";
        [Tooltip("IArmDriver를 구현하는 로봇(ArmController).")]
        [SerializeField] private MonoBehaviour _arm;
        [Tooltip("IToolPoseTarget을 구현하는 쪽(IkArmTarget).")]
        [SerializeField] private MonoBehaviour _ikTarget;
        [SerializeField] private MonoBehaviour _gripper;
        [SerializeField] private PickAndLiftTask _task;
        [SerializeField] private Graspable _pickTarget;
        [SerializeField] private Vector2 _screenPosition = new Vector2(10f, 10f);

        private IArmDriver _driver;
        private IToolPoseTarget _tool;
        private IGripper _grip;
        private string _status = "대기";

        private void Awake()
        {
            _driver = _arm as IArmDriver;
            _tool = _ikTarget as IToolPoseTarget;
            _grip = _gripper as IGripper;
            if (_task != null)
            {
                _task.Finished += OnTaskFinished;
            }
        }

        private void OnDestroy()
        {
            if (_task != null)
            {
                _task.Finished -= OnTaskFinished;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(_screenPosition.x, _screenPosition.y, 220f, 260f), _title, GUI.skin.window);
            GUILayout.Space(18f);

            GUILayout.Label($"상태: {_status}");
            GUILayout.Label(_tool != null && _tool.HasToolTarget
                ? $"IK: {(_tool.LastSolveSucceeded ? "성공" : "실패")} (위치 오차 {_tool.LastResult.PositionError * 1000f:F1}mm)"
                : "IK: 목표 없음");
            GUILayout.Label(_grip != null ? $"그리퍼: 개폐 {_grip.Opening:F2}, 쥠 {(_grip.IsHolding ? "예" : "아니오")}" : "그리퍼 없음");

            if (GUILayout.Button("집어서 들어올리기") && _task != null && !_task.IsRunning)
            {
                _status = "작업 중";
                _task.Begin(_pickTarget);
            }

            if (GUILayout.Button("그리퍼 열기") && _grip != null)
            {
                _grip.Open();
            }

            if (GUILayout.Button("그리퍼 닫기") && _grip != null)
            {
                _grip.Close();
            }

            if (GUILayout.Button("홈 자세") && _driver != null)
            {
                if (_task != null)
                {
                    _task.Cancel();
                }

                _driver.SetJointTargets(_driver.HomePoseRad);
                _status = "홈으로 이동";
            }

            GUILayout.EndArea();
        }

        private void OnTaskFinished(bool success)
        {
            _status = success ? "성공: 들어올림" : "실패";
        }
    }
}
