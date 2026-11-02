using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 사람이 직접 찾아 알려 준(teach) 선반 칸 하나의 집는 자세. 어느 칸인지, ArmA(루트)가 선 자리와 방향, 그때의 팔 베이스 자리, 쥘 때의 도구 끝점 자세(월드),
    /// 그때의 관절 각도(URDF 기준, 도)를 담는다. JSON으로 저장하므로 사람이 읽고 고칠 수 있는 단위(도, 미터)를 쓴다.
    /// 도구 끝점 자세로 집으러 가고, 관절 각도는 다시 닿았을 때 같은 자세로 닿았는지 견주는 데 쓴다.
    /// 값만 담는 클래스라 씬 없이 시험한다.
    /// </summary>
    [Serializable]
    public sealed class TaughtPickPose
    {
        [SerializeField] private string _slotId = string.Empty;
        [SerializeField] private float _rootX;
        [SerializeField] private float _rootZ;
        [SerializeField] private float _rootYawDeg;
        [SerializeField] private float _armBaseX;
        [SerializeField] private float _armBaseZ;
        [SerializeField] private Vector3 _toolPosition;
        [SerializeField] private Quaternion _toolRotation = Quaternion.identity;
        [SerializeField] private float[] _jointDeg = Array.Empty<float>();

        /// <summary>선반 칸 ID(예: A-1).</summary>
        public string SlotId => _slotId;

        /// <summary>ArmA(굴러가는 베이스 루트)가 서 있던 바닥 자리(월드). 높이는 0이다.</summary>
        public Vector3 RootPosition => new Vector3(_rootX, 0f, _rootZ);

        /// <summary>팔 베이스가 서야 할 바닥 자리(월드). 높이는 0이다. 루트 자리와 방향이 정해지면 따라서 정해지는 값이다.</summary>
        public Vector3 ArmBasePosition => new Vector3(_armBaseX, 0f, _armBaseZ);

        /// <summary>그 자리에 섰을 때 베이스 루트가 보는 방향(월드 Y축 기준 도).</summary>
        public float RootYawDeg => _rootYawDeg;

        /// <summary>물건을 쥘 때의 도구 끝점 자세(월드).</summary>
        public Pose ToolPose => new Pose(_toolPosition, _toolRotation);

        /// <summary>쥘 때의 관절 각도(rad, URDF 기준). 읽을 때마다 새 배열을 준다.</summary>
        public float[] JointsRad()
        {
            var rad = new float[_jointDeg.Length];
            for (int i = 0; i < rad.Length; i++)
            {
                rad[i] = _jointDeg[i] * Mathf.Deg2Rad;
            }

            return rad;
        }

        /// <param name="rootWorld">베이스 루트(ArmA)의 지금 월드 위치. 수평 성분만 쓴다.</param>
        /// <param name="rootYawDeg">베이스 루트의 지금 방향(도).</param>
        /// <param name="armBaseWorld">팔 베이스(링크)의 지금 월드 위치. 수평 성분만 쓴다.</param>
        /// <param name="toolWorld">도구 끝점의 지금 월드 자세.</param>
        /// <param name="jointsRad">지금 관절 각도(rad, URDF 기준).</param>
        public static TaughtPickPose Capture(
            string slotId, Vector3 rootWorld, float rootYawDeg, Vector3 armBaseWorld, Pose toolWorld, IReadOnlyList<float> jointsRad)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new ArgumentException("칸 ID가 비어 있다.", nameof(slotId));
            }

            if (jointsRad == null)
            {
                throw new ArgumentNullException(nameof(jointsRad));
            }

            var jointDeg = new float[jointsRad.Count];
            for (int i = 0; i < jointDeg.Length; i++)
            {
                jointDeg[i] = jointsRad[i] * Mathf.Rad2Deg;
            }

            return new TaughtPickPose
            {
                _slotId = PickSlotKey.Normalize(slotId),
                _rootX = rootWorld.x,
                _rootZ = rootWorld.z,
                _rootYawDeg = rootYawDeg,
                _armBaseX = armBaseWorld.x,
                _armBaseZ = armBaseWorld.z,
                _toolPosition = toolWorld.position,
                _toolRotation = toolWorld.rotation,
                _jointDeg = jointDeg
            };
        }
    }
}
