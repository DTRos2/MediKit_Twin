using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물리로 물건을 쥐는 방식. 손가락이 실제로 물건에 닿아 멈추고, 그 상태에서 물건을 손에 붙여 들고 간다.
    ///  1) 그리퍼가 열릴 때까지 기다린 뒤, 선반 물건의 복제본(콜라이더, 리지드바디, Graspable)을 물건 자리에 고정해 둔다.
    ///     고정해 두는 것은 손가락이 닫힐 때 물건이 밀려나지 않고 손가락을 막아 세우게 하기 위해서다.
    ///  2) 물건은 손가락이 놓인 레이어(로봇 팔) 외의 레이어와 부딪히지 않게 한다. 들고 가다 선반, 박스에 걸려 로봇이 끌려가는 일을 막는다.
    ///     충돌체 짝을 걸지 않고 충돌체의 제외 레이어로 정한다(ColliderLayerExclusion: 짝을 수십 개 걸면 팔 B 구동 쓰기에서 PhysX가 죽는다).
    ///  3) 그리퍼를 닫는다. 기존 JointGripper가 닫은 뒤 도구 끝점 반경 안의 Graspable을 고정 조인트로 붙인다(마찰에 의존하지 않는 안정 파지).
    ///  4) 손가락이 물건에 걸려 멈췄는지(IGripContact) 확인한다. 끝까지 닫혔다면 손가락 사이에 물건이 없는 것이므로 실패로 알린다.
    ///  5) 붙은 것을 확인한 뒤에야 물건을 물리로 풀고 중력을 켠다. 붙기 전에 켜면 손가락 사이로 떨어진다.
    ///     풀기 직전에 손가락과의 충돌을 끈다. 손가락은 닫을 때 이미 물건에 막혀 멈췄고, 이후는 조인트가 붙잡는다.
    /// 놓을 때는 박스와의 충돌을 되살리고 그리퍼를 열어, 물건이 손에서 풀려난 자세 그대로 중력으로 떨어져 바닥판과 이미 놓인 물건에 걸려 멈추게 한다(PlacedItemLanding).
    /// 멈춘 자리에서 고정해 박스의 자식으로 둔다. 슬롯 자리로 옮겨 놓지 않으므로 쥔 자세가 어긋났거나 박스에 걸리면 그 결과가 그대로 드러난다.
    /// 실패하면 이유를 LastFailureReason에 남기고 복제본을 지운다.
    /// </summary>
    public sealed class PhysicsGraspHolder : MonoBehaviour, IItemHolder, IHeldItemSource
    {
        [Tooltip("그리퍼(IGripper 구현체, 보통 JointGripper). 닫으면 반경 안의 Graspable을 조인트로 붙인다. IGripContact도 구현하면 손가락 접촉을 확인한다.")]
        [SerializeField] private MonoBehaviour _gripper;

        [Header("쥐기")]
        [Tooltip("쥐기 전에 그리퍼가 열리기를 기다리는 최대 시간(초). 넘으면 열리지 않은 채 진행한다(닫을 때 손가락 사이에 물건이 안 들어갈 수 있다).")]
        [SerializeField, Min(0.1f)] private float _openWaitTimeoutSeconds = 2f;
        [Tooltip("그리퍼를 닫은 뒤 물건을 쥘 때까지 기다리는 최대 시간(초). 그리퍼가 닫히는 시간과 안정 시간보다 길어야 한다.")]
        [SerializeField, Min(0.5f)] private float _graspTimeoutSeconds = 3f;
        [Tooltip("복제한 물건의 질량(kg).")]
        [SerializeField, Min(0.001f)] private float _itemMassKg = 0.05f;

        [Header("놓기")]
        [Tooltip("그리퍼가 열리기를 기다리는 시간(초). 시뮬레이션 시간이다. 물건이 내려앉는 시간이 이보다 길면 내려앉은 뒤 바로 진행한다.")]
        [SerializeField, Min(0.05f)] private float _releaseSettleSeconds = 0.9f;
        [Tooltip("놓은 물건이 멈췄는지 보기 전에 최소로 기다리는 시간(초). 조인트가 풀리고 떨어지기 시작하기까지의 순간을 멈춤으로 오해하지 않게 한다.")]
        [SerializeField, Min(0.05f)] private float _landMinSeconds = 0.3f;
        [Tooltip("놓은 물건이 멈추기를 기다리는 최대 시간(초). 넘으면 그 자리에서 고정한다.")]
        [SerializeField, Min(0.2f)] private float _landTimeoutSeconds = 2.5f;
        [Tooltip("이 속도(m/s) 이하이면 멈춘 것으로 본다.")]
        [SerializeField, Min(0f)] private float _restSpeedMps = 0.01f;
        [Tooltip("이 회전 속도(rad/s) 이하이면 멈춘 것으로 본다.")]
        [SerializeField, Min(0f)] private float _restSpinRadPerSec = 0.2f;
        [Tooltip("멈췄다고 보려면 속도가 연달아 이 횟수만큼 기준 이하여야 한다. 바닥에 닿아 튀기 직전의 한순간을 멈춤으로 오해하지 않게 한다.")]
        [SerializeField, Min(1)] private int _restSampleCount = 5;

        private IGripper _grip;
        private IGripContact _contact;
        private GraspableItemFactory _factory;
        private PlacedItemLanding _landing;
        private readonly GraspClosingTrace _trace = new GraspClosingTrace();
        private Graspable _held;
        private Quaternion _restingRotation = Quaternion.identity;

        public string LastFailureReason { get; private set; } = string.Empty;

        /// <summary>지금까지 놓은 물건 중 시간 안에 멈추지 못했거나 박스 영역 밖에 있던 것의 수. 시험이 놓기가 깔끔했는지 판정하는 데 쓴다.</summary>
        public int UnsettledPlacementCount { get; private set; }

        /// <summary>지금까지 놓은 물건이 선반에 놓여 있던 때보다 가장 많이 기울어 멈춘 각도(도).</summary>
        public float MaxPlacementTiltDeg { get; private set; }

        public bool TryGetHeldItem(out HeldItem held)
        {
            bool isHolding = _held != null && _grip != null && _grip.IsHolding;
            held = isHolding ? new HeldItem(_held.Center, _held.transform.rotation, _restingRotation) : default;
            return isHolding;
        }

        private void Awake()
        {
            _grip = _gripper as IGripper;
            if (_grip == null)
            {
                Debug.LogError($"{name}: Gripper(IGripper)를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _contact = _gripper as IGripContact;
            if (_contact == null)
            {
                Debug.LogWarning($"{name}: Gripper가 IGripContact를 구현하지 않아 손가락 접촉을 확인하지 못하고, 물건을 다른 충돌체에서 떼어 놓지도 못한다.", this);
            }

            _factory = new GraspableItemFactory(_itemMassKg);
            _landing = new PlacedItemLanding(new RestDetector(_restSpeedMps, _restSpinRadPerSec, _restSampleCount), _landMinSeconds, _landTimeoutSeconds);
        }

        public void OpenGripper()
        {
            _grip.Open();
        }

        public IEnumerator Grasp(ShelfPickItem item, Action<bool> done)
        {
            LastFailureReason = string.Empty;

            yield return WaitUntilOpen();

            _held = _factory.Create(item.Prototype, item.Pose, startKinematic: true);
            // 선반에 얹혀 있던 방향을 기억해 둔다. 박스에 놓을 때 물건이 같은 방향으로 놓이게 도구 방향을 잡는 기준이다.
            _restingRotation = item.Pose.rotation;
            KeepOnlyArmCollisionWhileClosing();
            _trace.Reset();
            _grip.Close();

            float elapsed = 0f;
            while (!_grip.IsHolding && elapsed < _graspTimeoutSeconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                _trace.Sample(_contact?.FingerColliders, _held.Colliders, _grip.Opening);
            }

            string reason = PhysicalGraspVerdict.FailureReason(
                _grip.IsHolding, _contact != null, _contact != null && _contact.IsBlockedByObject);
            if (_contact != null)
            {
                // 얇은 물건은 손가락이 기준에 아슬아슬하게 못 미치거나 넘으므로, 판정이 얼마나 여유 있었는지 남긴다.
                Debug.Log(
                    $"{name}: 쥐기 마침 — {item.Prototype.name}: 손가락 닫힘 {_contact.LeastClosedRatio:0.000}" +
                    $"(기준 {_contact.FullyClosedRatio:0.00} 이상이면 끝까지 닫혀 물건이 없다고 본다), 쥠 {_grip.IsHolding}", this);
            }

            if (reason.Length > 0)
            {
                LastFailureReason = $"{reason} — {DescribeDistance()}";
                Debug.LogWarning($"{name}: 물리로 쥐지 못했다 — {LastFailureReason}", this);
                Debug.LogWarning($"{name}: 쥐기 실패 진단 — {_trace.Describe(_contact?.FingerColliders, _held.Colliders)}", this);
                _grip.Open();
                DestroyHeld();
                done(false);
                yield break;
            }

            IgnoreEverythingWhileHeld();
            _held.EnablePhysics(useGravity: true);
            done(true);
        }

        public IEnumerator Release(ItemPlaceRequest request)
        {
            float started = Time.time;
            Graspable item = _held;
            _held = null;

            // 부딪힘은 조인트가 풀리기 전에 맞춘다. 풀리는 순간부터 물건은 이미 박스에 걸리고 손가락에는 걸리지 않아야 한다.
            if (item != null)
            {
                _landing.PrepareCollisions(item, _contact?.FingerColliders);
            }

            // 열면 JointGripper가 조인트를 푼다. 물건은 슬롯으로 옮기지 않고 풀린 자세 그대로 내려앉힌다.
            _grip.Open();

            if (item != null)
            {
                yield return _landing.Land(item, request.Parent, request.WorldPose.position, _restingRotation);
                WarnIfNotSettled(item);
                WarnIfBounced(item);
                MaxPlacementTiltDeg = Mathf.Max(MaxPlacementTiltDeg, _landing.TiltDeg);
                Debug.Log(
                    $"{name}: 놓은 물건 {item.name}이(가) {_landing.SettleSeconds:0.00}초 만에 멈췄다. " +
                    $"슬롯 자리에서 가로로 {_landing.SlotOffsetM * 100f:0.0}cm 벗어났고, 선반에 놓여 있던 때보다 {_landing.TiltDeg:0}° 기울었다.", this);
            }

            // 그리퍼가 다 열릴 시간은 내려앉는 시간과 겹치므로, 모자란 만큼만 더 기다린다.
            float remaining = _releaseSettleSeconds - (Time.time - started);
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }
        }

        public void Discard()
        {
            if (_held == null)
            {
                return;
            }

            _grip.Open();
            DestroyHeld();
        }

        // 시간 안에 멈추지 못했거나 박스 밖으로 나갔으면 알린다. 슬롯으로 옮겨 놓지 않으므로 이런 일이 조용히 지나가면 안 된다.
        private void WarnIfNotSettled(Graspable item)
        {
            if (_landing.CameToRest && _landing.IsInsideBox)
            {
                return;
            }

            UnsettledPlacementCount++;

            string reason = !_landing.IsInsideBox
                ? "박스 영역 밖에 있다(튕겨 나갔거나 바닥판을 뚫었다)"
                : $"{_landTimeoutSeconds:0.0}초 안에 멈추지 않아 그 자리에서 고정했다";
            Debug.LogWarning($"{name}: 놓은 물건 {item.name}이(가) {reason}. 풀린 자리에서 {_landing.TravelM:0.000}m 움직였다.", this);
        }

        // 중력만으로는 낼 수 없는 속도로 튀었으면 무엇과 겹치며 튀었는지 알린다. 박스 안에 멈췄더라도 놓는 방식을 고쳐야 한다는 신호다.
        private void WarnIfBounced(Graspable item)
        {
            LandingTrace trace = _landing.Trace;
            if (trace == null || !trace.HasSpike)
            {
                return;
            }

            Debug.LogWarning($"{name}: 놓은 물건 {item.name}이(가) 놓자마자 튀었다. {trace.Describe()}", this);
        }

        // 실패한 이유를 가리려면 물건이 쥐는 기준점에서 얼마나 떨어져 있었는지 알아야 한다. 도구 끝점이 물건에서 먼 것인지, 손가락이 어긋난 것인지 가른다.
        private string DescribeDistance()
        {
            if (_contact == null || _held == null)
            {
                return "도구 끝점과 물건 사이 거리를 알 수 없다";
            }

            float distance = Vector3.Distance(_held.Center, _contact.GraspCenter);
            return $"물건 중심이 도구 끝점에서 {distance:0.000}m 떨어져 있다(쥐는 반경 {_contact.GraspRadius:0.000}m)";
        }

        // 닫기 전에 손가락이 열려 있어야 물건이 손가락 사이에 들어온다. 열리는 명령이 끝나기를 잠시 기다린다.
        private IEnumerator WaitUntilOpen()
        {
            float elapsed = 0f;
            while (_grip.Opening < 1f && elapsed < _openWaitTimeoutSeconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }

        // 쥔 뒤에는 조인트가 물건을 붙잡으므로 아무것과도 부딪히지 않게 한다. 손가락과의 충돌을 켜 두면 손가락이 물건을 누르는 힘과
        // 조인트가 붙잡는 힘이 맞서서 팔이 가속할 때마다 물건이 손가락 사이에서 튀고 손가락이 안쪽으로 밀렸다 돌아온다(프레임 단위로 확인).
        // 닫을 때 손가락이 물건에 막혀 멈추는 것은 이 시점 전에 이미 끝났다. 놓을 때는 PlacedItemLanding이 어차피 손가락과 떼어 놓는다.
        private void IgnoreEverythingWhileHeld()
        {
            if (_contact == null)
            {
                return;
            }

            ColliderLayerExclusion.Exclude(_held.Colliders, ColliderLayerExclusion.AllLayers);
        }

        // 닫는 동안 물건은 손가락이 놓인 레이어(로봇 팔)와만 부딪히고, 선반과 박스 같은 나머지 레이어와는 부딪히지 않게 한다.
        // 손가락 정보를 모르면 건드리지 않는다. 짝이 아니라 레이어로 정하므로, 이 시점 이후에 새로 생긴 충돌체(예: 나중에 나오는 박스)에도 똑같이 적용된다.
        // 같은 팔 레이어의 손바닥 같은 다른 링크도 이 동안은 부딪힐 수 있지만, 물건은 손가락 사이(도구 끝점)에 있어 닿지 않는다.
        private void KeepOnlyArmCollisionWhileClosing()
        {
            if (_contact == null)
            {
                return;
            }

            ColliderLayerExclusion.ExcludeAllExcept(_held.Colliders, ColliderLayerExclusion.LayersOf(_contact.FingerColliders));
        }

        private void DestroyHeld()
        {
            if (_held != null)
            {
                Destroy(_held.gameObject);
            }

            _held = null;
        }
    }
}
