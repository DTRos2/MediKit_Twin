using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건을 놓은 직후의 움직임을 적어 둔다: 시간마다 위치, 속도, 겹친 충돌체.
    /// 박스로 놓은 물건이 튕겨 나갔을 때, 무엇과 겹치며 속도가 튀었는지 가리려는 기록이다.
    /// 중력으로만 떨어지면 속도가 낮으므로(낙하 높이에서 정해진다), 그보다 높은 기준을 넘으면 물리 엔진이 물건을 밀어낸 것으로 본다.
    /// 어떻게 겹침을 알아내는지는 주입받아, 이 객체는 물리 엔진 없이 기록과 글 만들기만 맡는다.
    /// </summary>
    public sealed class LandingTrace
    {
        // 글에 싣는 줄 수의 상한. 더 길면 로그가 읽기 어렵다.
        private const int MaxReportedLines = 10;

        private readonly struct Entry
        {
            public Entry(float seconds, Vector3 position, float speedMps, string contacts)
            {
                Seconds = seconds;
                Position = position;
                SpeedMps = speedMps;
                Contacts = contacts;
            }

            public float Seconds { get; }

            public Vector3 Position { get; }

            public float SpeedMps { get; }

            public string Contacts { get; }
        }

        private readonly float _spikeSpeedMps;
        private readonly float _watchSeconds;
        private readonly Func<string> _describeContacts;
        private readonly List<Entry> _entries = new List<Entry>();

        /// <param name="spikeSpeedMps">이 속도(m/s) 이상이면 중력만으로는 낼 수 없는 속도로 보고 튀었다고 판정한다.</param>
        /// <param name="watchSeconds">놓은 뒤 이 시간(초) 동안만 겹침을 조회해 적는다. 속도는 끝까지 센다.</param>
        /// <param name="describeContacts">지금 물건이 겹친 충돌체를 적은 글을 돌려주는 쪽. 없으면 겹침을 적지 않는다.</param>
        public LandingTrace(float spikeSpeedMps, float watchSeconds, Func<string> describeContacts)
        {
            _spikeSpeedMps = Mathf.Max(0f, spikeSpeedMps);
            _watchSeconds = Mathf.Max(0f, watchSeconds);
            _describeContacts = describeContacts;
        }

        /// <summary>기준 속도 이상으로 튄 적이 있으면 true.</summary>
        public bool HasSpike { get; private set; }

        /// <summary>지금까지 잰 가장 빠른 속도(m/s).</summary>
        public float PeakSpeedMps { get; private set; }

        /// <summary>가장 빠른 속도가 나온 때(놓은 뒤 초).</summary>
        public float PeakAtSeconds { get; private set; }

        /// <summary>놓는 순간을 적는다. 이때 이미 겹쳐 있으면 풀려나자마자 밀려나므로 가장 먼저 볼 단서다.</summary>
        public void Begin(Vector3 position)
        {
            _entries.Clear();
            HasSpike = false;
            PeakSpeedMps = 0f;
            PeakAtSeconds = 0f;
            _entries.Add(new Entry(0f, position, 0f, Contacts()));
        }

        /// <param name="position">기록에 남길 위치. 호출하는 쪽이 박스 기준으로 바꿔 넘기면 슬롯 좌표와 견줄 수 있다.</param>
        public void Sample(float elapsedSeconds, Vector3 position, float speedMps)
        {
            if (speedMps > PeakSpeedMps)
            {
                PeakSpeedMps = speedMps;
                PeakAtSeconds = elapsedSeconds;
            }

            if (speedMps >= _spikeSpeedMps)
            {
                HasSpike = true;
            }

            if (elapsedSeconds <= _watchSeconds)
            {
                _entries.Add(new Entry(elapsedSeconds, position, speedMps, Contacts()));
            }
        }

        /// <summary>겹침이 있었거나 빨랐던 순간만 골라 적은 글.</summary>
        public string Describe()
        {
            var text = new StringBuilder();
            text.Append($"최고 속도 {PeakSpeedMps:0.0}m/s({PeakAtSeconds:0.00}초). ");

            int lines = 0;
            foreach (Entry entry in _entries)
            {
                bool isInteresting = entry.Contacts.Length > 0 || entry.SpeedMps >= _spikeSpeedMps * 0.5f;
                if (!isInteresting)
                {
                    continue;
                }

                if (lines == MaxReportedLines)
                {
                    text.Append(" …");
                    break;
                }

                Vector3 p = entry.Position;
                text.Append(
                    $"[{entry.Seconds:0.00}초 박스 안 ({p.x * 100f:0.0}, {p.y * 100f:0.0}, {p.z * 100f:0.0})cm 속도 {entry.SpeedMps:0.0}m/s " +
                    $"겹침: {(entry.Contacts.Length > 0 ? entry.Contacts : "없음")}] ");
                lines++;
            }

            if (lines == 0)
            {
                text.Append($"처음 {_watchSeconds:0.0#}초 동안 겹친 곳은 없었다.");
            }

            return text.ToString().TrimEnd();
        }

        private string Contacts()
        {
            return _describeContacts != null ? _describeContacts() ?? string.Empty : string.Empty;
        }
    }
}
