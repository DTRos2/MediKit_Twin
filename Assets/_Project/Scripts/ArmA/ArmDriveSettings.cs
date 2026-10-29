using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 관절 구동 설정. URDF Importer가 만든 팔은 기본값 그대로면 루트가 고정되지 않고 구동(stiffness)이 0이라 쓰러진다.
    /// 로봇마다 값만 다르고 적용 방법은 같아서 데이터와 적용 코드를 한 클래스로 묶었다.
    /// </summary>
    [Serializable]
    public class ArmDriveSettings
    {
        [Header("루트")]
        [Tooltip("articulation 루트(base_link)를 고정한다. 끄면 팔 전체가 중력으로 떨어진다.")]
        [SerializeField] private bool _fixRoot = true;

        [Header("회전 관절")]
        [SerializeField] private float _revoluteStiffness = 10000f;
        [SerializeField] private float _revoluteDamping = 100f;
        [Tooltip("URDF의 effort보다 크게 둔다. 작으면 자세 유지 중 중력을 못 버틴다.")]
        [SerializeField] private float _revoluteForceLimit = 1000f;

        [Header("직선 관절 (그리퍼)")]
        [SerializeField] private float _prismaticStiffness = 10000f;
        [SerializeField] private float _prismaticDamping = 100f;
        [SerializeField] private float _prismaticForceLimit = 100f;

        [Header("공통")]
        [SerializeField] private float _jointFriction = 10f;
        [SerializeField] private float _damping = 10f;
        [Tooltip("긴 관절 체인이 떨리지 않게 솔버 반복 횟수를 올린다.")]
        [SerializeField, Min(1)] private int _solverIterations = 64;
        [SerializeField, Min(1)] private int _solverVelocityIterations = 16;

        public void Apply(ArticulationBody[] bodies)
        {
            foreach (ArticulationBody body in bodies)
            {
                body.solverIterations = _solverIterations;
                body.solverVelocityIterations = _solverVelocityIterations;

                if (body.isRoot)
                {
                    body.immovable = _fixRoot;
                    continue;
                }

                body.jointFriction = _jointFriction;
                body.linearDamping = _damping;
                body.angularDamping = _damping;

                switch (body.jointType)
                {
                    case ArticulationJointType.RevoluteJoint:
                        body.xDrive = BuildDrive(body.xDrive, _revoluteStiffness, _revoluteDamping, _revoluteForceLimit);
                        break;
                    case ArticulationJointType.PrismaticJoint:
                        body.xDrive = BuildDrive(body.xDrive, _prismaticStiffness, _prismaticDamping, _prismaticForceLimit);
                        break;
                }
            }
        }

        // target과 한계(lower/upper)는 임포터가 URDF에서 넣은 값을 그대로 둔다.
        private static ArticulationDrive BuildDrive(ArticulationDrive drive, float stiffness, float damping, float forceLimit)
        {
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            return drive;
        }
    }
}
