using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 카메라 인식과 장애물 회피를 이송 순서에 끼워 넣는 자리. 순서 객체들은 씬이 만들어질 때 조립되지만, 카메라와 충돌 상자는 로봇이 물리적으로
    /// 준비된 뒤에야 만들 수 있다. 그래서 먼저 빈 채로 순서 객체에 건네 두고, 준비되면 채운다(Attach).
    /// 채우기 전에는 아무것도 고치지 않고 모든 자세를 "겹치지 않음"으로 답해, 인식과 회피가 없는 때와 똑같이 동작한다.
    /// </summary>
    public sealed class ArmPerception : IGraspRefiner, IGraspSettler, IGraspListener
    {
        private IGraspRefiner _refiner;
        private Func<Pose, bool> _isClear;
        private Func<Pose, Pose, bool> _isClearAtBase;
        private Func<GraspTargets, bool> _isRouteClear;
        private Func<GraspTargets, string> _describeRoute;
        private Func<Pose, string> _describeBaseCollision;

        /// <summary>인식이나 회피가 채워졌으면 true.</summary>
        public bool IsAttached { get; private set; }

        /// <param name="refiner">물건 앞에서 쥐는 자리를 고치는 쪽. 인식을 쓰지 않으면 null.</param>
        /// <param name="isClear">지금 서 있는 자리에서 월드 기준 도구 자세가 장애물과 겹치지 않으면 true. 회피를 쓰지 않으면 null.</param>
        /// <param name="isClearAtBase">(지금 베이스 기준으로 옮긴 도구 자세, 팔이 서 있을 자리) → 겹치지 않으면 true. 회피를 쓰지 않으면 null.</param>
        /// <param name="isRouteClear">지금 자세에서 접근·집기·후퇴로 움직이는 길(자세 사이 포함)이 겹치지 않으면 true. 회피를 쓰지 않으면 null.</param>
        /// <param name="describeRoute">그 길이 어디서 겹치는지 설명한다. 필요 없으면 null.</param>
        /// <param name="describeBaseCollision">팔 베이스가 그 자세로 서 있을 때 겹치는 장애물 설명(겹치지 않으면 빈 글자). 베이스 겹침을 따지지 않으면 null.</param>
        public void Attach(
            IGraspRefiner refiner,
            Func<Pose, bool> isClear,
            Func<Pose, Pose, bool> isClearAtBase,
            Func<GraspTargets, bool> isRouteClear = null,
            Func<GraspTargets, string> describeRoute = null,
            Func<Pose, string> describeBaseCollision = null)
        {
            _refiner = refiner;
            _isClear = isClear;
            _isClearAtBase = isClearAtBase;
            _isRouteClear = isRouteClear;
            _describeRoute = describeRoute;
            _describeBaseCollision = describeBaseCollision;
            IsAttached = true;
        }

        public GraspRefinement Refine(ItemPickRequest request, GraspTargets planned)
        {
            return _refiner != null
                ? _refiner.Refine(request, planned)
                : GraspRefinement.Keep(planned, string.Empty);
        }

        // 쥐는 자리 마무리는 인식을 채운 refiner가 할 수 있을 때만 한다. 채우기 전이거나 할 수 없으면 아무것도 하지 않는다.
        public int SettleAttempts => _refiner is IGraspSettler settler ? settler.SettleAttempts : 0;

        public float SettleWaitSeconds => _refiner is IGraspSettler settler ? settler.SettleWaitSeconds : 0f;

        public GraspRefinement Settle(ItemPickRequest request, GraspTargets current)
        {
            return _refiner is IGraspSettler settler
                ? settler.Settle(request, current)
                : GraspRefinement.Keep(current, string.Empty);
        }

        // 쥔 것을 알아듣는 것은 인식을 채운 refiner가 할 수 있을 때만이다. 채우기 전이거나 들을 수 없으면 아무것도 하지 않는다.
        public void ItemGrasped(ItemPickRequest request)
        {
            (_refiner as IGraspListener)?.ItemGrasped(request);
        }

        /// <summary>지금 서 있는 자리에서 이 도구 자세가 장애물과 겹치지 않으면 true. 회피를 쓰지 않으면 항상 true.</summary>
        public bool IsClear(Pose worldTool)
        {
            return _isClear == null || _isClear(worldTool);
        }

        /// <summary>지금 자세에서 접근·집기·후퇴로 움직이는 동안 팔이 한 번도 겹치지 않으면 true. 회피를 쓰지 않으면 항상 true.</summary>
        public bool IsRouteClear(GraspTargets targets)
        {
            return _isRouteClear == null || _isRouteClear(targets);
        }

        /// <summary>
        /// 같은 도구 방향으로 positions(접근, 집기, 후퇴처럼 세 곳)를 차례로 지나는 길이 지금 자세에서부터 겹치지 않으면 true.
        /// 세 곳이 아니면 곳마다 자세로 따진다. 회피를 쓰지 않으면 항상 true.
        /// </summary>
        public bool IsClearAlong(Quaternion rotation, IReadOnlyList<Vector3> positions)
        {
            if (_isRouteClear != null && positions.Count == 3)
            {
                return _isRouteClear(new GraspTargets(rotation, positions[0], positions[1], positions[2], null));
            }

            foreach (Vector3 position in positions)
            {
                if (!IsClear(new Pose(position, rotation)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// IsClearAlong이 거짓일 때 그 길이 어떤 링크와 어떤 장애물에서 겹치는지 설명한다.
        /// 세 곳을 지나는 길만 설명할 수 있다. 그 밖이거나 겹치지 않거나 회피를 쓰지 않으면 빈 글자.
        /// </summary>
        public string DescribeClearAlong(Quaternion rotation, IReadOnlyList<Vector3> positions)
        {
            if (_describeRoute == null || positions.Count != 3)
            {
                return string.Empty;
            }

            return _describeRoute(new GraspTargets(rotation, positions[0], positions[1], positions[2], null));
        }

        /// <summary>그 길이 어디서 겹치는지 설명한다. 설명할 수 없으면 빈 글자.</summary>
        public string DescribeRouteCollision(GraspTargets targets)
        {
            return _describeRoute != null ? _describeRoute(targets) : string.Empty;
        }

        /// <summary>팔 베이스가 이 자세로 서 있을 때 베이스 링크가 겹치는 장애물을 설명한다. 겹치지 않거나 따질 수 없으면 빈 글자.</summary>
        public string DescribeBaseCollision(Pose armBaseWorld)
        {
            return _describeBaseCollision != null ? _describeBaseCollision(armBaseWorld) : string.Empty;
        }

        /// <summary>서 보기 전에 가늠할 때의 겹침 판정. 회피를 쓰지 않으면 항상 true.</summary>
        public bool IsClearAtBase(Pose relocatedTool, Pose armBaseWorld)
        {
            return _isClearAtBase == null || _isClearAtBase(relocatedTool, armBaseWorld);
        }
    }
}
