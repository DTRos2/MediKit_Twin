using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 굴러가는 베이스를 눈으로 시험하는 화면 버튼(IMGUI). Input 설정과 무관하게 동작한다.
    /// 로봇 앞/뒤 1m, 좌/우 1m(옆으로 가려면 먼저 그쪽을 향해 돈다), 시작 위치로 돌아가기, 정지 버튼이 있다.
    /// </summary>
    public class MobileBaseDemoPanel : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _mobileBase;
        [SerializeField] private string _title = "Mobile Base";
        [SerializeField] private Vector2 _screenPosition = new Vector2(10f, 290f);
        [Tooltip("패널 크기. 버튼을 늘리면 마지막 버튼이 잘리지 않도록 높이도 같이 늘린다.")]
        [SerializeField] private Vector2 _panelSize = new Vector2(220f, 240f);
        [SerializeField, Min(0.1f)] private float _stepMeters = 1f;

        private IMobileBase _base;
        private Vector3 _startPosition;

        private void Awake()
        {
            _base = _mobileBase as IMobileBase;
            if (_base == null)
            {
                Debug.LogError($"{name}: Mobile Base(IMobileBase)를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _startPosition = transform.position;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(_screenPosition.x, _screenPosition.y, _panelSize.x, _panelSize.y), _title, GUI.skin.window);
            GUILayout.Space(18f);
            GUILayout.Label(_base.IsMoving ? "상태: 이동 중" : "상태: 정지");

            if (GUILayout.Button("앞으로 1m"))
            {
                _base.DriveTo(transform.position + transform.forward * _stepMeters);
            }

            if (GUILayout.Button("뒤로 1m"))
            {
                _base.DriveTo(transform.position - transform.forward * _stepMeters);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("왼쪽 1m"))
            {
                _base.DriveTo(transform.position - transform.right * _stepMeters);
            }

            if (GUILayout.Button("오른쪽 1m"))
            {
                _base.DriveTo(transform.position + transform.right * _stepMeters);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("시작 위치로"))
            {
                _base.DriveTo(_startPosition);
            }

            if (GUILayout.Button("정지"))
            {
                _base.Stop();
            }

            GUILayout.EndArea();
        }
    }
}
