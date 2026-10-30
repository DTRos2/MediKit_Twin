using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건 복제본을 도구 끝점에 붙여 쥔 것처럼 보여 주는 방식(SRS 2.5: 스크립트 부착). 물리 충돌과 조인트를 쓰지 않아
    /// 안정적이고 선반이나 박스의 콜라이더 설정에 영향받지 않는다. 그리퍼는 눈으로 보이게 열고 닫기만 한다.
    /// </summary>
    public sealed class ScriptAttachHolder : MonoBehaviour, IItemHolder
    {
        [Tooltip("그리퍼(IGripper 구현체, 보통 JointGripper).")]
        [SerializeField] private MonoBehaviour _gripper;
        [Tooltip("도구 끝점의 현재 자세를 읽는 쪽(IArmState 구현체, 보통 ArmController). 쥔 물건이 따라가는 기준이다.")]
        [SerializeField] private MonoBehaviour _armState;
        [Tooltip("그리퍼가 닫히거나 열리기를 기다리는 시간(초). 시뮬레이션 시간이다.")]
        [SerializeField, Min(0.05f)] private float _gripperSettleSeconds = 0.9f;

        private IGripper _grip;
        private IArmState _state;
        private CarriedItem _carried;

        private void Awake()
        {
            _grip = _gripper as IGripper;
            _state = _armState as IArmState;
            if (_grip == null || _state == null)
            {
                Debug.LogError($"{name}: Gripper(IGripper)와 Arm State(IArmState)를 지정해야 한다.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            _carried?.Follow();
        }

        public void OpenGripper()
        {
            _grip.Open();
        }

        public IEnumerator Grasp(ShelfPickItem item, Action<bool> done)
        {
            _carried = CarriedItem.Grab(item.Prototype, () => _state.ToolPose);
            _grip.Close();
            yield return new WaitForSeconds(_gripperSettleSeconds);
            done(true);
        }

        public IEnumerator Release(ItemPlaceRequest request)
        {
            _grip.Open();
            _carried.PlaceAt(request.WorldPose, request.Parent);
            _carried = null;
            yield return new WaitForSeconds(_gripperSettleSeconds);
        }

        public void Discard()
        {
            _carried?.Destroy();
            _carried = null;
        }
    }
}
