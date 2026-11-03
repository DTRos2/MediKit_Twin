using System;
using System.Collections.Generic;
using System.Text;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 물건을 쥔 순간과 쥐고 있는 동안의 손가락·물건·도구 움직임을 적는다. 읽기만 하며 키팅에는 손대지 않는다.
    /// 두 가지를 가리려고 만들었다.
    ///  1) 쥔 순간 물건이 두 손가락 패드 사이에 있는지, 패드보다 뒤(손목 쪽)에 있는지. 도구 끝점 기준 좌표로 물건과 패드를 나란히 적는다.
    ///  2) 쥐고 있는 동안(놓은 직후 포함) 무엇이 떠는지. 도구, 팔 베이스, 물건(도구 기준), 손가락 관절을 같은 창으로 재서 어느 쪽이 흔들리는지 가른다.
    /// 흔들림 계산은 PositionSeries, 겹침 글은 FingerContactReporter, 관절 글은 FingerJointDescriber, 적는 시점과 길이는 GraspRecorderOptions가 맡고
    /// 이 클래스는 언제 적을지(쥠/놓음/창 끝)와 프레임 버퍼만 맡는다.
    /// 환경 변수 MEDIKIT_E2E_GRASP_TRACE=1일 때만 시험이 결과를 로그에 남긴다.
    /// </summary>
    public sealed class GraspHoldRecorder
    {
        public const string EnableVariable = "MEDIKIT_E2E_GRASP_TRACE";

        private const float MetersToMm = 1000f;

        private readonly JointGripper _gripper;
        private readonly ArmController _arm;
        private readonly GraspRecorderOptions _options;
        private readonly StringBuilder _lines = new StringBuilder();
        private readonly PositionSeries _toolPositions = new PositionSeries();
        private readonly PositionSeries _basePositions = new PositionSeries();
        private readonly PositionSeries _itemInTool = new PositionSeries();
        private readonly List<float[]> _fingerAngles = new List<float[]>();
        private readonly List<FrameSample> _recentFrames = new List<FrameSample>();
        private FingerJointSet _fingers;
        private FingerJointDescriber _fingerDescriber;
        private FingerContactReporter _contacts;
        private bool _fingerLookupDone;
        private Graspable _item;
        private bool _wasHolding;
        private bool _laterSnapshotDone;
        private int _largeSpanDumps;
        private float _heldSince;
        private float _releasedAt = float.NegativeInfinity;
        private float _windowStart;

        public GraspHoldRecorder(JointGripper gripper, ArmController arm, GraspRecorderOptions options = null)
        {
            _gripper = gripper;
            _arm = arm;
            _options = options ?? new GraspRecorderOptions();
        }

        public static bool IsEnabled => Environment.GetEnvironmentVariable(EnableVariable) == "1";

        public string Lines => _lines.ToString();

        public void Sample(float simSeconds)
        {
            if (_gripper == null || _arm == null || !_arm.IsReady)
            {
                return;
            }

            EnsureFingerJoints();
            bool isHolding = _gripper.IsHolding;

            if (isHolding && !_wasHolding)
            {
                BeginHold(simSeconds);
            }
            else if (!isHolding && _wasHolding)
            {
                EndHold(simSeconds);
            }

            _wasHolding = isHolding;
            bool isTracking = isHolding || simSeconds - _releasedAt <= _options.AfterReleaseSeconds;
            if (!isTracking)
            {
                return;
            }

            if (isHolding && !_laterSnapshotDone && simSeconds - _heldSince >= _options.LaterSnapshotSeconds)
            {
                _laterSnapshotDone = true;
                Snapshot(simSeconds, $"쥔 뒤 {_options.LaterSnapshotSeconds:0.0}초");
                // 쥔 직후 손가락이 목표를 지키는지, 넘어가는지 보려고 그동안의 프레임을 풀어 적는다.
                DumpRecentFrames();
            }

            AddSample();
            if (simSeconds - _windowStart >= _options.WindowSeconds)
            {
                FlushWindow(simSeconds, isHolding ? "쥐는 중" : "놓은 뒤");
                StartWindow(simSeconds);
            }
        }

        private void BeginHold(float simSeconds)
        {
            _item = _gripper.HeldObject;
            _contacts = new FingerContactReporter(_item, _options.MinReportedOverlapM);
            _heldSince = simSeconds;
            _laterSnapshotDone = false;
            StartWindow(simSeconds);
            Snapshot(simSeconds, "쥠");
        }

        private void EndHold(float simSeconds)
        {
            _releasedAt = simSeconds;
            FlushWindow(simSeconds, "놓기 직전");
            DumpRecentFrames();
            Snapshot(simSeconds, "놓음");
            StartWindow(simSeconds);
        }

        // 프로필에 적힌 손가락 관절을 팔이 준비된 뒤 한 번만 찾는다. 못 찾으면 이유를 한 줄 적고 손가락 없이 나머지만 잰다.
        private void EnsureFingerJoints()
        {
            if (_fingerLookupDone)
            {
                return;
            }

            _fingerLookupDone = true;
            if (FingerJointSet.TryCreate(_arm, out _fingers, out string error))
            {
                _fingerDescriber = new FingerJointDescriber(_fingers);
            }
            else
            {
                _lines.AppendLine($"  손가락 관절을 찾지 못해 손가락 기록은 건너뛴다: {error}");
            }
        }

        private void StartWindow(float simSeconds)
        {
            _windowStart = simSeconds;
            _toolPositions.Clear();
            _basePositions.Clear();
            _itemInTool.Clear();
            _fingerAngles.Clear();
        }

        private void AddSample()
        {
            Pose tool = _arm.ToolPose;
            _toolPositions.Add(tool.position);
            _basePositions.Add(_arm.BaseWorldPosition);

            Vector3 itemInTool = Vector3.zero;
            if (_item != null)
            {
                itemInTool = Quaternion.Inverse(tool.rotation) * (_item.Center - tool.position);
                _itemInTool.Add(itemInTool);
            }

            float[] angles = _fingerDescriber != null ? _fingerDescriber.ReadDisplayPositions() : Array.Empty<float>();
            _fingerAngles.Add(angles);
            _recentFrames.Add(new FrameSample(Time.time, tool.position, itemInTool, angles));
            if (_recentFrames.Count > _options.RecentFrameCount)
            {
                _recentFrames.RemoveAt(0);
            }
        }

        // 놓기 직전 마지막 몇 프레임을 프레임마다 적는다. 진동이면 부호가 번갈아 나오고, 부드러운 움직임이면 한 방향으로 이어진다.
        private void DumpRecentFrames()
        {
            _lines.AppendLine("    놓기 직전 프레임별(도구 월드 위치 변화 mm · 물건의 도구 기준 위치 mm · 손가락 관절 위치 mm 또는 °):");
            for (int i = 1; i < _recentFrames.Count; i++)
            {
                FrameSample now = _recentFrames[i];
                Vector3 step = (now.Tool - _recentFrames[i - 1].Tool) * MetersToMm;
                Vector3 item = now.ItemInTool * MetersToMm;
                _lines.AppendLine(
                    $"      [{now.Time:0.00}] 변화 ({step.x:+0.0;-0.0;0.0}, {step.y:+0.0;-0.0;0.0}, {step.z:+0.0;-0.0;0.0}) · " +
                    $"물건 ({item.x:+0.0;-0.0;0.0}, {item.y:+0.0;-0.0;0.0}, {item.z:+0.0;-0.0;0.0}) · 손가락 {string.Join(" ", Array.ConvertAll(now.Fingers, v => v.ToString("0.0")))}");
            }
        }

        private void FlushWindow(float simSeconds, string label)
        {
            if (_toolPositions.Count < _options.MinWindowSamples)
            {
                return;
            }

            _lines.AppendLine(
                $"  [{simSeconds:0.00}s] {label} {_toolPositions.Count}표본 · 도구 흔들림 {FormatJitter(_toolPositions.Jitter())} · " +
                $"팔 베이스 흔들림 {FormatJitter(_basePositions.Jitter())} · 물건의 도구 기준 위치 범위(mm) {FormatSpan(_itemInTool)} · " +
                $"손가락 관절 이동 범위(직선 mm, 회전 °) {FingerRange()}");

            // 도구는 조용한데 물건만 크게 움직인 창은 프레임별로 풀어 적는다. 로그가 길어지지 않게 몇 번까지만 적는다.
            Vector3 itemSpan = _itemInTool.Span() * MetersToMm;
            bool isLarge = Mathf.Max(itemSpan.x, itemSpan.y, itemSpan.z) >= _options.LargeItemSpanMm;
            if (isLarge && _largeSpanDumps < _options.MaxLargeSpanDumps)
            {
                _largeSpanDumps++;
                DumpRecentFrames();
            }
        }

        // 2차 차분의 제곱평균 제곱근과 최댓값(mm). 월드 좌표라 팔이 가속하는 구간도 조금 잡히지만 진동은 훨씬 크게 나온다.
        private static string FormatJitter(JitterStats jitter)
        {
            return jitter.HasValue ? $"{jitter.RmsM * MetersToMm:0.00}(최대 {jitter.MaxM * MetersToMm:0.00})mm" : "-";
        }

        private static string FormatSpan(PositionSeries series)
        {
            if (series.Count == 0)
            {
                return "-";
            }

            Vector3 span = series.Span() * MetersToMm;
            return $"({span.x:0.0}, {span.y:0.0}, {span.z:0.0})";
        }

        // 창 안에서 관절마다 얼마나 움직였는지. 직선 관절은 mm, 회전 관절은 도다.
        private string FingerRange()
        {
            if (_fingerDescriber == null)
            {
                return "-";
            }

            var parts = new List<string>();
            for (int i = 0; i < _fingerDescriber.Count; i++)
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                foreach (float[] sample in _fingerAngles)
                {
                    min = Mathf.Min(min, sample[i]);
                    max = Mathf.Max(max, sample[i]);
                }

                parts.Add($"{max - min:0.0}");
            }

            return string.Join(" ", parts);
        }

        // 물건과 패드(손가락 끝)를 도구 끝점 기준 좌표로 나란히 적고, 손가락 관절이 얼마나 닫혔는지와 겹침 깊이를 적는다.
        private void Snapshot(float simSeconds, string label)
        {
            if (_item == null)
            {
                return;
            }

            Pose tool = _arm.ToolPose;
            var itemExtent = new ToolFrameExtent(tool);
            foreach (Collider collider in _item.Colliders)
            {
                itemExtent.Include(ColliderPoints.Of(collider));
            }

            _lines.AppendLine($"[{simSeconds:0.00}s] {label}: 막힘 판정 {_gripper.IsBlockedByObject}, 도구 끝점 ({tool.position.x:0.000},{tool.position.y:0.000},{tool.position.z:0.000})");
            _lines.AppendLine($"    물건(도구 기준) {itemExtent.Describe()}");

            foreach (Collider finger in _gripper.FingerColliders)
            {
                var extent = new ToolFrameExtent(tool);
                extent.Include(ColliderPoints.Of(finger));
                _lines.AppendLine(
                    $"    {finger.transform.parent?.name}/{finger.name} ({finger.GetType().Name}) {extent.Describe()}" +
                    $"{_contacts.Penetration(finger)}{_contacts.OtherOverlaps(finger)}");
            }

            if (_fingerDescriber == null)
            {
                return;
            }

            for (int i = 0; i < _fingerDescriber.Count; i++)
            {
                _lines.AppendLine($"    {_fingerDescriber.Describe(i)}");
            }
        }

        private readonly struct FrameSample
        {
            public FrameSample(float time, Vector3 tool, Vector3 itemInTool, float[] fingers)
            {
                Time = time;
                Tool = tool;
                ItemInTool = itemInTool;
                Fingers = fingers;
            }

            public float Time { get; }

            public Vector3 Tool { get; }

            public Vector3 ItemInTool { get; }

            public float[] Fingers { get; }
        }
    }
}
