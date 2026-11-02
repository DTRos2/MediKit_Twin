using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스가 어떤 자세로 서 있을 때 베이스 링크가 장애물과 겹치는지 따진다. 팔 링크는 ArmCollisionChecker가 따지지만 베이스 링크는
    /// 거기서 빠져 있어서, 베이스가 컨베이어 레일 같은 장애물에 파고드는 자리에 서도 알 수 없었다. 파고든 채 서면 물리 엔진이 베이스를
    /// 밀어내는 힘에 첫 관절이 홈 자세에서 튀어 나가 팔을 접지 못한다. 장애물 질의는 주입받으므로 씬 없이 시험한다.
    /// </summary>
    public sealed class BaseClearanceChecker
    {
        // 여유를 뺀 결과가 0 이하가 되지 않게 하는 상자 반폭의 최소값(m). 반폭 0인 상자는 겹침 질의가 성립하지 않는다.
        private const float MinHalfExtentM = 0.001f;

        private readonly IReadOnlyList<LinkVolume> _volumes;
        private readonly IObstacleQuery _obstacles;
        private readonly float _marginM;

        /// <param name="volumes">베이스 링크 기준 충돌 상자(BaseVolumeBuilder).</param>
        /// <param name="marginM">상자 각 면을 이만큼 키워 따진다. 도착 오차로 조금 어긋나 서도 파고들지 않을 여유다.</param>
        public BaseClearanceChecker(IReadOnlyList<LinkVolume> volumes, IObstacleQuery obstacles, float marginM)
        {
            _volumes = volumes ?? throw new ArgumentNullException(nameof(volumes));
            _obstacles = obstacles ?? throw new ArgumentNullException(nameof(obstacles));
            _marginM = marginM;
        }

        /// <param name="armBaseWorld">팔 베이스 링크가 서 있을 월드 자세.</param>
        /// <param name="description">겹쳤으면 "팔 베이스 ↔ 장애물" 설명. 아니면 빈 글자.</param>
        public bool TryFindCollision(Pose armBaseWorld, out string description)
        {
            description = string.Empty;

            foreach (LinkVolume volume in _volumes)
            {
                Vector3 center = armBaseWorld.position + armBaseWorld.rotation * volume.LocalCenter;
                Quaternion rotation = armBaseWorld.rotation * volume.LocalRotation;
                Vector3 half = Vector3.Max(volume.HalfExtents + Vector3.one * _marginM, Vector3.one * MinHalfExtentM);

                if (_obstacles.Overlaps(center, half, rotation, out string obstacle))
                {
                    description = $"팔 베이스 ↔ {obstacle}";
                    return true;
                }
            }

            return false;
        }
    }
}
