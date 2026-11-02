using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔이 어떤 관절 각도에 있을 때 장애물과 겹치는지 따진다. 팔을 움직이지 않고 순기구학(KinematicChain)으로 링크 위치를 구해
    /// 링크마다 붙은 상자(LinkVolume)를 거기에 놓고 장애물 질의(IObstacleQuery)로 겹침을 본다.
    /// 팔 베이스가 어디에 서 있는지는 호출할 때 준다. 서 보기 전에 "저기 서 있다면"을 따질 때도 같은 코드로 따진다.
    /// 팔 링크끼리의 겹침(자기 충돌)은 따지지 않는다. 씬 없이 시험할 수 있도록 장애물 질의는 주입받는다.
    /// </summary>
    public sealed class ArmCollisionChecker
    {
        private readonly KinematicChain _chain;
        private readonly IReadOnlyList<LinkVolume> _volumes;
        private readonly IObstacleQuery _obstacles;
        private readonly float _clearanceM;
        private readonly int _skipBaseLinks;
        private readonly IReadOnlyList<string> _linkNames;
        private readonly Pose[] _linkPoses;
        private readonly float[] _pathAngles;

        // 한 구간을 이보다 잘게 나누지 않는다(관절이 크게 움직여도 계산이 끝없이 늘지 않게).
        private const int MaxPathSteps = 40;

        /// <param name="clearanceM">상자 각 면을 이만큼 키워 따진다(음수면 줄인다).</param>
        /// <param name="skipBaseLinks">베이스에서 가까운 링크 몇 개를 따지지 않을지. 베이스에 붙어 항상 닿아 있는 링크를 뺀다.</param>
        /// <param name="linkNames">관절 링크 이름(결과 설명에 쓴다). 없으면 번호로 적는다.</param>
        public ArmCollisionChecker(
            KinematicChain chain,
            IReadOnlyList<LinkVolume> volumes,
            IObstacleQuery obstacles,
            float clearanceM,
            int skipBaseLinks,
            IReadOnlyList<string> linkNames)
        {
            _chain = chain ?? throw new ArgumentNullException(nameof(chain));
            _volumes = volumes ?? throw new ArgumentNullException(nameof(volumes));
            _obstacles = obstacles ?? throw new ArgumentNullException(nameof(obstacles));
            _clearanceM = clearanceM;
            _skipBaseLinks = Mathf.Max(0, skipBaseLinks);
            _linkNames = linkNames;
            _linkPoses = new Pose[chain.JointCount];
            _pathAngles = new float[chain.JointCount];
        }

        public bool Collides(float[] anglesRad, Pose armBaseWorld)
        {
            return TryFindCollision(anglesRad, armBaseWorld, out _);
        }

        /// <param name="anglesRad">관절 각도(rad). 관절 수와 같은 길이.</param>
        /// <param name="armBaseWorld">팔 베이스 링크의 월드 자세. 팔이 서 있는 자리다.</param>
        /// <param name="description">겹쳤으면 "링크 ↔ 장애물" 설명. 아니면 빈 글자.</param>
        public bool TryFindCollision(float[] anglesRad, Pose armBaseWorld, out string description)
        {
            description = string.Empty;
            if (anglesRad == null || anglesRad.Length < _chain.JointCount)
            {
                return false;
            }

            _chain.ForwardLinks(anglesRad, _linkPoses);

            for (int i = 0; i < _volumes.Count; i++)
            {
                LinkVolume volume = _volumes[i];
                if (volume.LinkIndex < _skipBaseLinks || volume.LinkIndex >= _linkPoses.Length)
                {
                    continue;
                }

                Pose link = _linkPoses[volume.LinkIndex];
                Quaternion linkRotation = armBaseWorld.rotation * link.rotation;
                Vector3 linkPosition = armBaseWorld.position + armBaseWorld.rotation * link.position;

                Vector3 center = linkPosition + linkRotation * volume.LocalCenter;
                Quaternion rotation = linkRotation * volume.LocalRotation;
                Vector3 half = Vector3.Max(volume.HalfExtents + Vector3.one * _clearanceM, Vector3.one * 0.001f);

                if (_obstacles.Overlaps(center, half, rotation, out string obstacle))
                {
                    description = $"{LinkName(volume.LinkIndex)} ↔ {obstacle}";
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 관절이 from에서 to로 같은 비율로 움직이는 도중에 어디서든 겹치면 true. 팔이 그 길을 지나가는 동안 선반 모서리를 쓸고 가는지 본다.
        /// 끝점(to)은 따지고 출발점(from)은 따지지 않는다(출발점은 이미 서 있는 자세이거나 앞 구간의 끝점으로 따졌다).
        /// </summary>
        /// <param name="maxStepRad">한 번에 보는 관절 변화의 최대 크기(rad). 작을수록 촘촘히 본다.</param>
        /// <param name="description">겹쳤으면 "링크 ↔ 장애물(경로 몇 %)" 설명. 아니면 빈 글자.</param>
        public bool PathCollides(float[] fromRad, float[] toRad, Pose armBaseWorld, float maxStepRad, out string description)
        {
            description = string.Empty;
            if (fromRad == null || toRad == null || fromRad.Length < _chain.JointCount || toRad.Length < _chain.JointCount)
            {
                return false;
            }

            float largest = 0f;
            for (int i = 0; i < _chain.JointCount; i++)
            {
                largest = Mathf.Max(largest, Mathf.Abs(toRad[i] - fromRad[i]));
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(largest / Mathf.Max(maxStepRad, 0.01f)), 1, MaxPathSteps);
            for (int step = 1; step <= steps; step++)
            {
                float t = (float)step / steps;
                for (int i = 0; i < _chain.JointCount; i++)
                {
                    _pathAngles[i] = Mathf.Lerp(fromRad[i], toRad[i], t);
                }

                if (TryFindCollision(_pathAngles, armBaseWorld, out string found))
                {
                    description = $"{found}(경로 {t * 100f:0}%)";
                    return true;
                }
            }

            return false;
        }

        private string LinkName(int index)
        {
            return _linkNames != null && index < _linkNames.Count ? _linkNames[index] : $"링크 {index}";
        }
    }
}
