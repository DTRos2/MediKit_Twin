using UnityEngine;

namespace MediKitTwin.Mobile
{
    /// <summary>
    /// 로봇암을 움직이는 베이스(ArmA의 바퀴 달린 하부) 위에 얹어서 함께 움직이게 한다.
    /// ArticulationBody 루트는 부모 Transform이 움직여도 물리 쪽 위치가 따라오지 않아 팔이 제자리에 남으므로,
    /// 매 프레임 베이스의 위치와 회전을 계산해 TeleportRoot로 루트를 옮긴다.
    /// 시작할 때 베이스 기준의 상대 위치를 기억하므로, 팔을 베이스 위 어디에 두었든 그 자리를 유지한다.
    /// 어떤 이동 방식(MobileBaseDriver 등)이든 베이스 Transform만 움직이면 된다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class MountedBaseFollower : MonoBehaviour
    {
        [Tooltip("팔이 얹힐 베이스(예: ArmA 프리팹 인스턴스). 이 Transform이 움직이면 팔이 따라간다.")]
        [SerializeField] private Transform _mount;

        private ArticulationBody _root;
        private Vector3 _localPositionInMount;
        private Quaternion _localRotationInMount;
        private bool _ready;

        private void Start()
        {
            if (_mount == null)
            {
                Debug.LogError($"{name}: Mount(베이스)를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _root = FindRoot();
            Transform reference = _root != null ? _root.transform : transform;

            // 지금 놓인 자리를 베이스 기준 상대 위치로 기억한다.
            _localPositionInMount = _mount.InverseTransformPoint(reference.position);
            _localRotationInMount = Quaternion.Inverse(_mount.rotation) * reference.rotation;
            _ready = true;
        }

        // 이동 컴포넌트가 Update에서 베이스를 움직인 뒤에 실행되도록 실행 순서를 늦췄다.
        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            Vector3 position = _mount.TransformPoint(_localPositionInMount);
            Quaternion rotation = _mount.rotation * _localRotationInMount;

            if (_root != null)
            {
                _root.TeleportRoot(position, rotation);
            }
            else
            {
                transform.SetPositionAndRotation(position, rotation);
            }
        }

        private ArticulationBody FindRoot()
        {
            foreach (ArticulationBody body in GetComponentsInChildren<ArticulationBody>(true))
            {
                if (body.isRoot)
                {
                    return body;
                }
            }

            return null;
        }
    }
}
