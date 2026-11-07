using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥔 물체를 그리퍼에 고정 조인트로 붙인다. 마찰에 의존하지 않아 물체가 미끄러지지 않는다(안정 파지).
    /// 어떻게 쥐는지(JointGripper)와 어떻게 붙이는지를 분리해서, 나중에 마찰 파지로 바꿔도 JointGripper는 그대로다.
    /// </summary>
    public sealed class GraspAttachment
    {
        private FixedJoint _joint;

        public Graspable Held { get; private set; }

        public bool IsAttached => Held != null;

        public void Attach(Graspable target, ArticulationBody anchor)
        {
            Detach();

            _joint = target.gameObject.AddComponent<FixedJoint>();
            _joint.connectedArticulationBody = anchor;
            _joint.enableCollision = false;

            target.IsHeld = true;
            Held = target;
        }

        /// <summary>
        /// 쥔 물건을 motion(월드 변환)만큼 옮긴다. 팔이 통째로 옮겨질 때 물건을 같은 만큼 옮겨 손과의 상대 자세를 지킨다.
        /// 물리 위치와 화면 위치를 함께 옮기고 속도는 건드리지 않는다. 쥐고 있지 않으면 아무것도 하지 않는다.
        /// </summary>
        public void Carry(Matrix4x4 motion)
        {
            if (Held == null)
            {
                return;
            }

            Rigidbody body = Held.Body;
            Vector3 position = motion.MultiplyPoint3x4(body.position);
            Quaternion rotation = motion.rotation * body.rotation;
            body.position = position;
            body.rotation = rotation;
            Held.transform.SetPositionAndRotation(position, rotation);
        }

        public void Detach()
        {
            if (_joint != null)
            {
                Object.Destroy(_joint);
            }

            if (Held != null)
            {
                Held.IsHeld = false;
            }

            _joint = null;
            Held = null;
        }
    }
}
