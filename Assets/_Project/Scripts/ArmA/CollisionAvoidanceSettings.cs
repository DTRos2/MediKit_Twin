using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 링크가 선반·박스 같은 장애물에 닿지 않는 자세를 고르는 방식의 설정.
    /// 팔 링크를 감싸는 상자(링크 충돌체의 경계)로 장애물과 겹치는지 따진다. 상자는 실제 모양보다 크므로 좁은 칸 안에서는 닿지 않는데도 닿는다고 볼 수 있다.
    /// 기본은 겹치는 자세로는 가지 않는다(RequireClear). 그 대신 서는 자리를 뒤로 물리거나 옆으로 옮기고 도구 방향을 바꿔 겹치지 않는 자세를 찾는다.
    /// </summary>
    [Serializable]
    public sealed class CollisionAvoidanceSettings
    {
        [Tooltip("끄면 장애물과 겹치는지 따지지 않는다.")]
        [SerializeField] private bool _enabled = true;
        [Tooltip("켜면 장애물과 겹치지 않는 자세만 쓴다. 서는 자리와 도구 방향을 다 바꿔도 없으면 움직이지 않고 실패로 끝낸다. 끄면 겹치는 자세라도 경고를 남기고 간다(팔이 선반에 부딪혀 물리 엔진이 불안해질 수 있다).")]
        [SerializeField] private bool _requireClear = true;
        [Tooltip("링크 상자를 이만큼(m) 키워 따진다. 양수면 여유를 더 두고, 음수면 줄여서 좁은 칸에서도 덜 걸리게 한다.")]
        [SerializeField, Range(-0.03f, 0.05f)] private float _clearanceM;
        [Tooltip("베이스에서 가까운 링크 몇 개를 따지지 않을지. 첫 링크(어깨)는 베이스에 붙어 항상 닿아 있다.")]
        [SerializeField, Min(0)] private int _skipBaseLinks = 1;
        [Tooltip("장애물로 볼 레이어. 로봇 자신, 쥔 물건, 베이스에 붙은 것은 이 값과 상관없이 장애물로 보지 않는다.")]
        [SerializeField] private LayerMask _obstacleLayers = ~0;
        [Tooltip("겹치지 않는 방향이 아직 안 나왔을 때, 닿는 방향을 처음 찾은 뒤에도 이만큼 더 후보를 확인해 본다.")]
        [SerializeField, Min(0)] private int _lookaheadCandidates = 30;

        [Header("서는 자리 — 베이스")]
        [Tooltip("켜면 박스 곁에 서기 전에, 그 자리에 선 팔 베이스(고정 베이스 링크)가 컨베이어 레일 같은 장애물에 파고드는지 따져 파고들면 뒤로 물러나 선다. " +
                 "파고든 채 서면 첫 관절이 홈 자세에서 튀어 나가 팔을 접지 못한다.")]
        [SerializeField] private bool _avoidBaseOverlap = true;
        [Tooltip("켜면 팔 베이스 링크뿐 아니라 팔을 싣고 다니는 차체(바퀴·궤도·받침)도 레일 같은 장애물에 파고드는지 따진다. " +
                 "차체는 팔 베이스보다 앞뒤로 커서 베이스가 닿지 않아도 차체는 레일을 파고들 수 있다. 끄면 팔 베이스 링크만 따진다.")]
        [SerializeField] private bool _avoidCarrierOverlap = true;
        [Tooltip("차체 밑면보다 윗면이 이만큼(m)까지 높은 충돌체는 차체가 올라선 바닥으로 보고 장애물에서 뺀다. 바닥은 차체 상자와 늘 맞닿아 있어서 빼지 않으면 어디에 서도 겹친다.")]
        [SerializeField, Range(0f, 0.1f)] private float _groundToleranceM = 0.03f;
        [Tooltip("팔 베이스 상자를 이만큼(m) 키워 따진다. 도착 오차로 조금 어긋나 서도 파고들지 않을 여유다.")]
        [SerializeField, Range(0f, 0.05f)] private float _baseMarginM = 0.01f;
        [Tooltip("파고들지 않는 자리를 찾을 때 한 번에 물러나는 거리(m).")]
        [SerializeField, Min(0.001f)] private float _baseRetreatStepM = 0.01f;
        [Tooltip("파고들지 않는 자리를 찾으려고 물러나 볼 최대 거리(m). 넘어도 없으면 원래 자리에 서고 로그로 알린다.")]
        [SerializeField, Min(0f)] private float _maxBaseRetreatM = 0.15f;

        public bool Enabled => _enabled;

        public bool AvoidBaseOverlap => _avoidBaseOverlap;

        public bool AvoidCarrierOverlap => _avoidCarrierOverlap;

        public float GroundToleranceM => _groundToleranceM;

        public float BaseMarginM => _baseMarginM;

        public float BaseRetreatStepM => _baseRetreatStepM;

        public float MaxBaseRetreatM => _maxBaseRetreatM;

        public float ClearanceM => _clearanceM;

        public int SkipBaseLinks => _skipBaseLinks;

        public int ObstacleLayers => _obstacleLayers.value;

        public ClearancePolicy ToPolicy()
        {
            return new ClearancePolicy(_requireClear, _lookaheadCandidates);
        }
    }
}
