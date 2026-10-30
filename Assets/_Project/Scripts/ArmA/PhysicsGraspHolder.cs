using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물리로 물건을 쥐는 방식. 선반 물건의 복제본에 콜라이더, 리지드바디, Graspable을 붙여 두고 그리퍼를 닫으면,
    /// 기존 JointGripper가 TCP 반경 안의 Graspable을 고정 조인트로 붙인다(마찰에 의존하지 않는 안정 파지).
    /// 닫은 뒤 시간 안에 쥐지 못하면(IsHolding이 false) 실패로 알린다. 놓을 때는 조인트를 풀고 슬롯 자리에 고정해
    /// 박스의 자식으로 둔다. 박스의 콜라이더 설정과 무관하게 슬롯에 정확히 놓이게 하기 위해서다.
    /// </summary>
    public sealed class PhysicsGraspHolder : MonoBehaviour, IItemHolder
    {
        [Tooltip("그리퍼(IGripper 구현체, 보통 JointGripper). 닫으면 반경 안의 Graspable을 조인트로 붙인다.")]
        [SerializeField] private MonoBehaviour _gripper;

        [Header("쥐기")]
        [Tooltip("그리퍼를 닫은 뒤 물건을 쥘 때까지 기다리는 최대 시간(초). 그리퍼가 닫히는 시간과 안정 시간보다 길어야 한다.")]
        [SerializeField, Min(0.5f)] private float _graspTimeoutSeconds = 3f;
        [Tooltip("복제한 물건의 질량(kg).")]
        [SerializeField, Min(0.001f)] private float _itemMassKg = 0.05f;

        [Header("놓기")]
        [Tooltip("그리퍼가 열리기를 기다리는 시간(초). 시뮬레이션 시간이다.")]
        [SerializeField, Min(0.05f)] private float _releaseSettleSeconds = 0.9f;

        private IGripper _grip;
        private GraspableItemFactory _factory;
        private Graspable _held;

        private void Awake()
        {
            _grip = _gripper as IGripper;
            if (_grip == null)
            {
                Debug.LogError($"{name}: Gripper(IGripper)를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _factory = new GraspableItemFactory(_itemMassKg);
        }

        public void OpenGripper()
        {
            _grip.Open();
        }

        public IEnumerator Grasp(ShelfPickItem item, Action<bool> done)
        {
            _held = _factory.Create(item.Prototype, item.Pose);
            _grip.Close();

            float elapsed = 0f;
            while (!_grip.IsHolding && elapsed < _graspTimeoutSeconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            if (!_grip.IsHolding)
            {
                _grip.Open();
                DestroyHeld();
                done(false);
                yield break;
            }

            // 조인트로 붙은 뒤에야 중력을 켠다. 붙기 전에 켜면 손가락 사이로 떨어진다.
            _held.Body.useGravity = true;
            done(true);
        }

        public IEnumerator Release(ItemPlaceRequest request)
        {
            // 열면 JointGripper가 조인트를 푼다. 그 즉시 슬롯 자리에 고정해 박스의 자식으로 둔다.
            _grip.Open();

            if (_held != null)
            {
                _held.Body.isKinematic = true;
                _held.transform.SetPositionAndRotation(request.WorldPose.position, request.WorldPose.rotation);
                _held.transform.SetParent(request.Parent, true);
                _held = null;
            }

            yield return new WaitForSeconds(_releaseSettleSeconds);
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
