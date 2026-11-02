using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서는 자리 계산을 감싸서, 그 자리에 선 팔 베이스가 장애물에 파고들면 물러나 서도록 고친다.
    /// 서는 자리는 "슬롯에서 정해진 거리" 같은 규칙으로만 정해 두었기 때문에 베이스의 크기와 주변의 레일, 벽을 보지 못한다.
    /// 물러나는 방향은 서는 자리 함수 스스로 알려 준다: 같은 옆 보정에서 물러나는 거리를 조금(DirectionProbeM) 늘린 자리로 가는 방향이다.
    /// 그래서 박스든 선반이든 서는 방향을 따로 받지 않는다. 베이스의 지금 상태를 알 수 없거나 겹침을 따질 수 없으면 서는 자리를 그대로 둔다.
    /// </summary>
    public sealed class BaseStandClearance
    {
        // 물러나는 방향을 알아내려고 물러나는 거리를 이만큼(m) 늘려 본다. 방향만 쓰므로 0이 아니기만 하면 되고,
        // 서는 자리 함수가 그 거리에서 값을 돌려주는 범위 안이어야 한다.
        private const float DirectionProbeM = 1f;

        private readonly IArmBaseStateSource _baseState;
        private readonly Func<Pose, string> _describeBaseCollision;
        private readonly float _stepM;
        private readonly float _maxRetreatM;
        private readonly Action<string> _log;
        private readonly HashSet<string> _reported = new HashSet<string>();

        /// <param name="baseState">팔 베이스의 지금 자세를 알려 주는 쪽. 서 있을 자세를 미리 구하는 데 쓴다.</param>
        /// <param name="describeBaseCollision">팔 베이스가 그 자세로 서 있을 때 겹치는 장애물 설명. 겹치지 않거나 따질 수 없으면 빈 글자.</param>
        /// <param name="stepM">한 번에 물러나는 거리(m).</param>
        /// <param name="maxRetreatM">물러나 볼 최대 거리(m).</param>
        /// <param name="log">진행을 알리는 쪽. 필요 없으면 null.</param>
        public BaseStandClearance(
            IArmBaseStateSource baseState, Func<Pose, string> describeBaseCollision, float stepM, float maxRetreatM, Action<string> log = null)
        {
            _baseState = baseState;
            _describeBaseCollision = describeBaseCollision ?? throw new ArgumentNullException(nameof(describeBaseCollision));
            _stepM = stepM;
            _maxRetreatM = maxRetreatM;
            _log = log;
        }

        /// <param name="standFor">(물러나는 거리, 비키는 거리)에 해당하는 서는 자리. 이동하지 않는 팔이면 null을 돌려준다.</param>
        /// <param name="facingYawDeg">서는 자리에서 바라볼 방향(도). 정하지 않았으면 지금 방향 그대로 선다고 본다.</param>
        /// <param name="label">로그에 붙일 이름(예: 박스).</param>
        /// <returns>서는 자리를 고친 함수. 고칠 수 없는 상황이면 standFor 그대로.</returns>
        public Func<float, float, Vector3?> Apply(Func<float, float, Vector3?> standFor, float? facingYawDeg, string label)
        {
            if (standFor == null || _baseState == null || !_baseState.TryGetState(out Pose armBase, out float rootYawDeg))
            {
                return standFor;
            }

            float facing = facingYawDeg ?? rootYawDeg;
            string Describe(Vector3 stand) => _describeBaseCollision(ArmBasePosePredictor.Predict(armBase, rootYawDeg, stand, facing));
            var retreat = new StandRetreat(stand => string.IsNullOrEmpty(Describe(stand)), _stepM, _maxRetreatM);

            return (extra, lateral) =>
            {
                Vector3? stand = standFor(extra, lateral);
                if (!stand.HasValue)
                {
                    return stand;
                }

                Vector3? further = standFor(extra + DirectionProbeM, lateral);
                Vector3 away = further.HasValue ? further.Value - stand.Value : Vector3.zero;

                if (!retreat.TryFind(stand.Value, away, out Vector3 cleared, out float retreatedM))
                {
                    Report(label, stand.Value, $"{label}: 서는 자리에서 팔 베이스가 {Describe(stand.Value)}와(과) 겹치지만 {_maxRetreatM * 100f:0}cm 물러나도 피할 자리가 없어 그대로 선다.");
                    return stand;
                }

                if (retreatedM > 0f)
                {
                    Report(label, stand.Value, $"{label}: 서는 자리에서 팔 베이스가 {Describe(stand.Value)}와(과) 겹쳐 {retreatedM * 100f:0}cm 물러나 선다.");
                }

                return cleared;
            };
        }

        // 같은 자리에서 같은 말을 되풀이하지 않는다(서는 자리는 가늠할 때와 이동할 때 여러 번 물어본다).
        private void Report(string label, Vector3 stand, string message)
        {
            if (_log != null && _reported.Add($"{label}|{stand.x:0.000}|{stand.z:0.000}"))
            {
                _log(message);
            }
        }
    }
}
