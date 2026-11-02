using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>그리퍼가 집을 수 있는 물체 표시. 박스, 뚜껑 등에 붙인다.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Graspable : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }

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

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// 고정(키네마틱) 상태에서 물리로 움직이는 상태로 바꾼다. 손가락이 닫히는 동안에는 물체가 밀려나지 않게 고정해 두고,
        /// 그리퍼에 붙은 뒤에 이것을 불러 물체가 손을 따라 움직이게 한다.
        /// </summary>
        public void EnablePhysics(bool useGravity)
        {
            Body.isKinematic = false;
            Body.useGravity = useGravity;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
}
