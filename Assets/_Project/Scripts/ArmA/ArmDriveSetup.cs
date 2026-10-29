using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 시작할 때 루트를 고정하고 모든 관절에 위치 구동을 걸어 현재 자세를 유지시킨다.
    /// 로봇 최상위 오브젝트(fr5_robot, ur3_with_gripper)에 붙인다. 구체 설정은 ArmDriveSettings가 갖는다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ArmDriveSetup : MonoBehaviour
    {
        [SerializeField] private ArmDriveSettings _settings = new ArmDriveSettings();

        private void Awake()
        {
            _settings.Apply(GetComponentsInChildren<ArticulationBody>(true));
        }
    }
}
