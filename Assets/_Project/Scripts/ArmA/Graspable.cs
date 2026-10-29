using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>그리퍼가 집을 수 있는 물체 표시. 박스, 뚜껑 등에 붙인다.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Graspable : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }

        public bool IsHeld { get; internal set; }

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }
    }
}
