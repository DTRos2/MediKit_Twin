using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 선반 물건 원본을 물리로 쥘 수 있는 복제본으로 만든다. 선반의 원본에는 콜라이더와 리지드바디가 없으므로
    /// 복제본에 메시 크기에 맞는 BoxCollider, Rigidbody, Graspable을 붙인다.
    /// 쥐기 전에는 중력을 꺼서 제자리에 있게 하고, 쥔 뒤에 켠다.
    /// </summary>
    public sealed class GraspableItemFactory
    {
        private readonly float _massKg;

        public GraspableItemFactory(float massKg)
        {
            _massKg = massKg;
        }

        public Graspable Create(GameObject prototype, Pose pose)
        {
            GameObject clone = Object.Instantiate(prototype, pose.position, pose.rotation);
            clone.SetActive(true);

            // 선반 원본은 빌더가 콜라이더를 지워 두었다. 겹쳐 붙지 않도록 혹시 남은 콜라이더도 걷어낸다.
            foreach (Collider existing in clone.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(existing);
            }

            var box = clone.AddComponent<BoxCollider>();
            MeshFilter meshFilter = clone.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // 컴포넌트를 스크립트로 붙이면 크기가 자동으로 맞춰지지 않으므로 메시 경계로 직접 맞춘다.
                box.center = meshFilter.sharedMesh.bounds.center;
                box.size = meshFilter.sharedMesh.bounds.size;
            }

            var body = clone.AddComponent<Rigidbody>();
            body.mass = _massKg;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            return clone.AddComponent<Graspable>();
        }
    }
}
