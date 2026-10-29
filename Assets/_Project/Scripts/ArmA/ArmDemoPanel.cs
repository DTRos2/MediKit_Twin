using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 집기 동작을 눈으로 확인하는 테스트 패널. 화면 버튼(IMGUI)이라 Input 설정과 무관하게 동작한다.
    /// 로봇마다 하나씩 붙이고, 같은 Pick Target을 가리키게 하면 두 로봇을 번갈아 시험할 수 있다.
    /// </summary>
    public class ArmDemoPanel : MonoBehaviour
    {
        [SerializeField] private string _title = "Arm Demo";
        [SerializeField] private ArmController _arm;
        [SerializeField] private IkArmTarget _ikTarget;
        [SerializeField] private MonoBehaviour _gripper;
        [SerializeField] private PickAndLiftTask _task;
        [SerializeField] private Graspable _pickTarget;
        [SerializeField] private Vector2 _screenPosition = new Vector2(10f, 10f);

        private IGripper _grip;
        private string _status = "대기";

        private void Awake()
        {
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
            GUILayout.Label(_ikTarget != null && _ikTarget.HasToolTarget
                ? $"IK: {(_ikTarget.LastSolveSucceeded ? "성공" : "실패")} (위치 오차 {_ikTarget.LastResult.PositionError * 1000f:F1}mm)"
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

            if (GUILayout.Button("홈 자세(모든 관절 0)") && _arm != null)
            {
                if (_task != null)
                {
                    _task.Cancel();
                }

                _arm.SetJointTargets(new float[_arm.JointCount]);
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
