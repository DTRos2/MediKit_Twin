#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using MediKitTwin.Pusher;
using UnityEngine;
using PusherStation = MediKitTwin.Pusher.Pusher;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 푸셔가 민 박스가 어디로 가는지 적는다. 읽기만 하며 푸셔와 불량 박스에는 손대지 않는다.
    /// 박스가 불량 박스에 못 들어갈 때 "안 밀렸는지, 밀렸는데 모자랐는지, 닿았다가 튕겼는지"를 가리려고 만들었다.
    /// 푸셔가 박스에 표시(RejectedKit)를 붙인 순간부터 일정한 간격으로 박스 위치·속도, 푸셔 헤드 위치, 불량 박스 영역과의 관계를 한 줄씩 남긴다.
    /// </summary>
    internal sealed class PushedBoxTrace
    {
        // 한 줄씩 적는 간격(시뮬레이션 초). 푸는 데 몇 초 걸리므로 이 정도면 움직임의 모양이 보인다.
        private const float SampleIntervalSeconds = 0.25f;
        private const string HeadName = "PushHead";
        // 표시가 붙은 뒤 이 시간(초)까지만 적는다. 박스가 끝내 못 들어가도 기록이 끝없이 늘지 않게 한다.
        private const float MaxTraceSeconds = 12f;

        private readonly Collider _binZone;
        private readonly Transform _head;
        private readonly List<string> _lines = new List<string>();
        private float _firstSeen = float.NaN;
        private float _lastSample = float.NegativeInfinity;

        public PushedBoxTrace(RejectBin bin, PusherStation pusher)
        {
            _binZone = bin != null ? bin.GetComponent<Collider>() : null;
            _head = pusher != null ? pusher.transform.Find(HeadName) : null;
        }

        public bool HasTrace => _lines.Count > 0;

        public void Sample(float simSeconds)
        {
            var marker = Object.FindAnyObjectByType<RejectedKit>();
            if (marker == null)
            {
                return;
            }

            if (float.IsNaN(_firstSeen))
            {
                _firstSeen = simSeconds;
            }

            if (simSeconds - _firstSeen > MaxTraceSeconds || simSeconds - _lastSample < SampleIntervalSeconds)
            {
                return;
            }

            _lastSample = simSeconds;
            _lines.Add(Describe(marker, simSeconds));
        }

        public string Dump()
        {
            string zone = _binZone != null ? $"불량 박스 영역 {_binZone.bounds.min:F2} ~ {_binZone.bounds.max:F2}" : "불량 박스 영역을 찾지 못했다";
            return $"푸셔가 민 박스의 궤적({zone}):\n{string.Join("\n", _lines)}";
        }

        private string Describe(RejectedKit marker, float simSeconds)
        {
            Transform box = marker.transform;
            var text = new StringBuilder();
            text.Append($"  {simSeconds - _firstSeen,5:0.00}초 박스 {box.position:F2}");

            var body = marker.GetComponent<Rigidbody>();
            if (body != null)
            {
                text.Append($" 속도 {body.linearVelocity:F2} {(body.isKinematic ? "(키네마틱)" : string.Empty)}");
            }

            if (_head != null)
            {
                text.Append($" 헤드 {_head.position:F2}");
            }

            if (_binZone != null)
            {
                text.Append(_binZone.bounds.Contains(box.position) ? " [영역 안]" : $" [영역까지 {Vector3.Distance(_binZone.ClosestPoint(box.position), box.position):0.00}m]");
            }

            text.Append($" 부모 {(box.parent != null ? box.parent.name : "없음")}");
            return text.ToString();
        }
    }
}
#endif
