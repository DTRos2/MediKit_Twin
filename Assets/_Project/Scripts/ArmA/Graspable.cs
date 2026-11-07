using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>그리퍼가 집을 수 있는 물체 표시. 박스, 뚜껑 등에 붙인다.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Graspable : MonoBehaviour
    {
        private Rigidbody _body;

        // Awake 전에도(예: 플레이 모드가 아닌 시험) 읽을 수 있게 처음 부를 때 찾는다.
        public Rigidbody Body => _body != null ? _body : (_body = GetComponent<Rigidbody>());

        public bool IsHeld { get; internal set; }

        /// <summary>이 물체와 그 아래의 충돌체 전부.</summary>
        public Collider[] Colliders => GetComponentsInChildren<Collider>();

        /// <summary>충돌체들을 감싸는 영역의 중심(월드). 충돌체가 없으면 물체의 위치.</summary>
        public Vector3 Center
        {
            get
            {
                bool hasBounds = false;
                Bounds total = default;
                foreach (Collider collider in GetComponentsInChildren<Collider>())
                {
                    if (collider == null)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        total = collider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        total.Encapsulate(collider.bounds);
                    }
                }

                return hasBounds ? total.center : transform.position;
            }
        }

        /// <summary>
        /// 고정(키네마틱) 상태에서 물리로 움직이는 상태로 바꾼다. 손가락이 닫히는 동안에는 물체가 밀려나지 않게 고정해 두고,
        /// 그리퍼에 붙은 뒤에 이것을 불러 물체가 손을 따라 움직이게 한다.
        /// </summary>
        /// <param name="interpolation">
        /// 화면 위치를 물리 스텝 사이에서 보간할지. 기본은 보간한다. 손에 쥐고 옮기는 동안에는 보간을 끈다.
        /// 팔 몸체는 보간이 없어 물리 스텝마다 뚝뚝 움직이는데, 쥔 물건만 보간하면 프레임 시간이 제각각인 에디터에서 물건이 손보다 늦게 따라와 손에서 떨어져 보인다.
        /// </param>
        public void EnablePhysics(bool useGravity, RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate)
        {
            Body.isKinematic = false;
            Body.useGravity = useGravity;
            Body.interpolation = interpolation;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        /// <summary>
        /// 물체를 지금 자리에 고정해 부모를 따라 움직이게 한다. 속도를 0으로 만든 뒤 키네마틱으로 바꾸고, 보간과 연속 충돌 검출을 끈다.
        /// 보간이 켜진 몸체는 화면 위치를 물리가 낸 위치에서 매 프레임 덮어써서, 부모(박스)가 스크립트로 컨베이어를 타고 움직여도
        /// 제자리에 남는다. 박스에 놓인 물건이 박스를 따라가려면 보간이 꺼져 있어야 한다. 연속 충돌 검출은 고정된 몸체에는 맞지 않는 설정이다.
        /// 속도는 켜 둔 채로 0을 쓰고 나서 고정한다(고정한 몸체에 속도를 쓰면 경고가 난다).
        /// </summary>
        public void Freeze()
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            Body.interpolation = RigidbodyInterpolation.None;
            Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
    }
}
