using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥐고 온 물건을 박스에 물리로 내려앉힌다. 놓은 물건이 슬롯 자리로 순간이동하지 않고, 그리퍼가 열려 손에서 풀려난 자세 그대로
    /// 중력으로 떨어져 바닥판에 닿고 이미 놓인 물건에 걸려 멈추게 한다. 멈춘 자리에서 고정해 박스의 자식으로 둔다.
    /// 멈춘 뒤에 고정하는 것은 박스를 컨베이어로 치울 때 박스가 부모 변환으로 움직이는데, 물리로 움직이는 자식이 그와 다투지 않게 하기 위해서다.
    /// 어떻게 쥐는지(PhysicsGraspHolder)와 분리해, 쥐는 방식이 바뀌어도 내려앉히는 방법은 그대로 쓴다.
    /// </summary>
    public sealed class PlacedItemLanding
    {
        // 중력만으로 떨어지면 낙하 높이 20cm에서도 약 2m/s다. 이보다 빠르면 물리 엔진이 겹침을 풀며 물건을 밀어낸 것으로 본다.
        private const float SpikeSpeedMps = 3f;

        // 놓은 뒤 이 시간(초) 동안의 겹침만 적는다. 튕기는 것은 풀려난 직후에 일어난다.
        private const float TraceWatchSeconds = 0.5f;

        private readonly RestDetector _rest;
        private readonly float _minSeconds;
        private readonly float _timeoutSeconds;

        /// <param name="rest">멈췄는지 판정하는 객체.</param>
        /// <param name="minSeconds">최소로 기다리는 시간(초). 그리퍼 조인트가 풀리는 데 한 프레임이 걸려, 그 사이의 속도 0을 멈춘 것으로 오해하지 않게 한다.</param>
        /// <param name="timeoutSeconds">이 시간(초) 안에 멈추지 않으면 기다림을 끝내고 그 자리에서 고정한다.</param>
        public PlacedItemLanding(RestDetector rest, float minSeconds, float timeoutSeconds)
        {
            _rest = rest ?? throw new ArgumentNullException(nameof(rest));
            _minSeconds = Mathf.Max(0f, minSeconds);
            _timeoutSeconds = Mathf.Max(_minSeconds, timeoutSeconds);
        }

        /// <summary>마지막으로 내려앉힌 물건이 풀려난 뒤의 움직임 기록. 아직 내려앉힌 물건이 없으면 null이다.</summary>
        public LandingTrace Trace { get; private set; }

        /// <summary>마지막으로 내려앉힌 물건이 시간 안에 멈췄으면 true. 못 멈추고 시간이 다 되어 고정했으면 false.</summary>
        public bool CameToRest { get; private set; }

        /// <summary>마지막으로 내려앉힌 물건의 중심이 박스 안(바닥판과 벽이 감싸는 영역)에 있으면 true. 튕겨 나갔거나 바닥을 뚫었으면 false.</summary>
        public bool IsInsideBox { get; private set; }

        /// <summary>마지막으로 내려앉힌 물건이 풀려난 자리에서 멈춘 자리까지 움직인 거리(m).</summary>
        public float TravelM { get; private set; }

        /// <summary>마지막으로 내려앉힌 물건의 중심이 목표 슬롯 자리에서 가로(수평)로 벗어난 거리(m). 쥔 자세가 어긋났거나 박스에 걸려 밀린 만큼이다.</summary>
        public float SlotOffsetM { get; private set; }

        /// <summary>마지막으로 내려앉힌 물건이 풀려난 뒤 멈추기까지 걸린 시간(초). 멈추지 못했으면 기다린 시간 전체다.</summary>
        public float SettleSeconds { get; private set; }

        /// <summary>마지막으로 내려앉힌 물건이 선반에 놓여 있던 때보다 기울어 멈춘 각도(도). 0이면 선반에서와 같은 면이 위를 향한다(RestingTilt).</summary>
        public float TiltDeg { get; private set; }

        /// <summary>
        /// 놓기 전에 부딪힘을 맞춘다. 쥐고 오는 동안은 물건이 박스를 포함한 모든 충돌체와 떨어져 있으므로,
        /// 박스 아래의 충돌체(바닥판, 벽, 이미 놓인 물건)와는 다시 부딪히게 하고, 손가락과는 떨어지게 한다.
        /// 손가락과 떨어지게 하는 것은 그리퍼가 열리며 손가락이 물건을 쳐서 날리거나 들어올릴 때 물건을 끌고 가지 않게 하기 위해서다.
        /// </summary>
        /// <param name="fingers">손가락 충돌체. 모르면 null.</param>
        public void PrepareCollisions(Graspable item, Transform box, IReadOnlyList<Collider> fingers)
        {
            Collider[] own = item.Colliders;
            CollisionIsolation.Restore(own, box.GetComponentsInChildren<Collider>());

            if (fingers != null)
            {
                CollisionIsolation.Isolate(own, fingers, null);
            }
        }

        /// <summary>
        /// 물건을 물리로 풀어 멈출 때까지 기다린 뒤 고정해 parent의 자식으로 둔다. 그리퍼를 연 직후에 부른다.
        /// 결과는 CameToRest, IsInsideBox, TravelM, SlotOffsetM, SettleSeconds, TiltDeg에 남는다.
        /// </summary>
        /// <param name="slotWorldPosition">원래 놓으려던 슬롯 자리. 물건을 여기로 옮기지는 않고, 멈춘 자리가 여기서 얼마나 벗어났는지 재는 데만 쓴다.</param>
        /// <param name="restingRotation">물건이 선반에 놓여 있던 회전. 멈춘 물건이 그때보다 얼마나 기울었는지 재는 기준이다.</param>
        public IEnumerator Land(Graspable item, Transform parent, Vector3 slotWorldPosition, Quaternion restingRotation)
        {
            Vector3 released = item.transform.position;
            item.EnablePhysics(useGravity: true);
            _rest.Reset();

            var overlaps = new ItemOverlapDescriber(item);
            Trace = new LandingTrace(SpikeSpeedMps, TraceWatchSeconds, overlaps.Describe);
            Trace.Begin(parent.InverseTransformPoint(released));

            float elapsed = 0f;
            CameToRest = false;
            while (elapsed < _timeoutSeconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;

                float speed = item.Body.linearVelocity.magnitude;
                Trace.Sample(elapsed, parent.InverseTransformPoint(item.transform.position), speed);

                bool isStill = _rest.Update(speed, item.Body.angularVelocity.magnitude);
                if (elapsed >= _minSeconds && isStill)
                {
                    CameToRest = true;
                    break;
                }
            }

            // 켜 둔 채 속도를 0으로 만든 뒤 고정한다. 고정한 물체에 속도를 쓰면 경고가 난다.
            item.Body.linearVelocity = Vector3.zero;
            item.Body.angularVelocity = Vector3.zero;
            item.Body.isKinematic = true;

            Vector3 center = item.Center;
            IsInsideBox = BoxFootprint.Contains(parent, center);
            TravelM = Vector3.Distance(released, item.transform.position);
            SettleSeconds = elapsed;
            SlotOffsetM = new Vector2(center.x - slotWorldPosition.x, center.z - slotWorldPosition.z).magnitude;
            TiltDeg = RestingTilt.Degrees(restingRotation, item.transform.rotation, parent.up);
            item.transform.SetParent(parent, true);
        }
    }
}
