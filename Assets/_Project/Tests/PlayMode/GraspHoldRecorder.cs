using System;
using System.Collections.Generic;
using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 물건을 쥔 순간과 쥐고 있는 동안의 손가락·물건·도구 움직임을 적는다. 읽기만 하며 키팅에는 손대지 않는다.
    /// 두 가지를 가리려고 만들었다.
    ///  1) 쥔 순간 물건이 두 손가락 패드 사이에 있는지, 패드보다 뒤(손목 쪽)에 있는지. 도구 끝점 기준 좌표로 물건과 패드를 나란히 적는다.
    ///  2) 쥐고 있는 동안(놓은 직후 포함) 무엇이 떠는지. 도구, 팔 베이스, 물건(도구 기준), 손가락 관절을 같은 창으로 재서 어느 쪽이 흔들리는지 가른다.
    /// 흔들림은 연속한 세 표본의 2차 차분(p[i+1] - 2p[i] + p[i-1])으로 잰다. 일정한 속도로 움직이면 0이고 진동하면 커진다.
    /// 환경 변수 MEDIKIT_E2E_GRASP_TRACE=1일 때만 시험이 결과를 로그에 남긴다.
    /// </summary>
    public sealed class GraspHoldRecorder
    {
        public const string EnableVariable = "MEDIKIT_E2E_GRASP_TRACE";

        // 쥔 뒤 이만큼 지나서 한 번 더 적는다(물건이 EnablePhysics로 풀린 뒤 미끄러졌는지 보려는 것).
        private const float LaterSnapshotSeconds = 0.4f;
        // 놓은 뒤에도 이만큼은 흔들림을 잰다(놓을 때 손가락이 떠는지 보려는 것).
        private const float AfterReleaseSeconds = 1.5f;
        private const float WindowSeconds = 0.5f;
        private const int MinWindowSamples = 5;
        // 놓기 직전 프레임별로 적을 개수. 50fps에서 0.6초.
        private const int RecentFrameCount = 31;
        // 물건이 도구 기준으로 이만큼(mm) 퍼진 창은 프레임별로 풀어 적는다. 몇 번까지만 적는다.
        private const float LargeItemSpanMm = 30f;
        private const int MaxLargeSpanDumps = 4;

        private readonly JointGripper _gripper;
        private readonly ArmController _arm;
        private readonly StringBuilder _lines = new StringBuilder();
        private readonly List<Vector3> _toolPositions = new List<Vector3>();
        private readonly List<Vector3> _basePositions = new List<Vector3>();
        private readonly List<Vector3> _itemInTool = new List<Vector3>();
        private readonly List<float[]> _fingerAngles = new List<float[]>();
        private readonly List<FrameSample> _recentFrames = new List<FrameSample>();
        private ArticulationBody[] _fingerJoints;
        private float[] _openValues;
        private float[] _closedValues;
        private Graspable _item;
        private bool _wasHolding;
        private bool _laterSnapshotDone;
        private int _largeSpanDumps;
        private float _heldSince;
        private float _releasedAt = float.NegativeInfinity;
        private float _windowStart;

        public GraspHoldRecorder(JointGripper gripper, ArmController arm)
        {
            _gripper = gripper;
            _arm = arm;
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
                _item = _gripper.HeldObject;
                _heldSince = simSeconds;
                _laterSnapshotDone = false;
                StartWindow(simSeconds);
                Snapshot(simSeconds, "쥠");
            }
            else if (!isHolding && _wasHolding)
            {
                _releasedAt = simSeconds;
                FlushWindow(simSeconds, "놓기 직전");
                DumpRecentFrames();
                Snapshot(simSeconds, "놓음");
                StartWindow(simSeconds);
            }

            _wasHolding = isHolding;
            bool isTracking = isHolding || simSeconds - _releasedAt <= AfterReleaseSeconds;
            if (!isTracking)
            {
                return;
            }

            if (isHolding && !_laterSnapshotDone && simSeconds - _heldSince >= LaterSnapshotSeconds)
            {
                _laterSnapshotDone = true;
                Snapshot(simSeconds, $"쥔 뒤 {LaterSnapshotSeconds:0.0}초");
                // 쥔 직후 손가락이 목표를 지키는지, 넘어가는지 보려고 그동안의 프레임을 풀어 적는다.
                DumpRecentFrames();
            }

            AddSample();
            if (simSeconds - _windowStart >= WindowSeconds)
            {
                FlushWindow(simSeconds, isHolding ? "쥐는 중" : "놓은 뒤");
                StartWindow(simSeconds);
            }
        }

        // 프로필에 적힌 손가락 관절을 이름으로 찾는다. 팔이 준비된 뒤 한 번만 한다.
        private void EnsureFingerJoints()
        {
            if (_fingerJoints != null)
            {
                return;
            }

            IReadOnlyList<ArmProfileSO.GripperJointEntry> entries = _arm.GripperJoints;
            ArticulationBody[] all = _arm.RobotRoot.GetComponentsInChildren<ArticulationBody>();
            _fingerJoints = new ArticulationBody[entries.Count];
            _openValues = new float[entries.Count];
            _closedValues = new float[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                _openValues[i] = entries[i].OpenValue;
                _closedValues[i] = entries[i].ClosedValue;
                foreach (ArticulationBody body in all)
                {
                    if (body.name == entries[i].LinkName)
                    {
                        _fingerJoints[i] = body;
                        break;
                    }
                }
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

            if (_item != null)
            {
                _itemInTool.Add(Quaternion.Inverse(tool.rotation) * (_item.Center - tool.position));
            }

            var angles = new float[_fingerJoints.Length];
            for (int i = 0; i < angles.Length; i++)
            {
                angles[i] = _fingerJoints[i] != null ? _fingerJoints[i].jointPosition[0] * DisplayScale(_fingerJoints[i]) : float.NaN;
            }

            _fingerAngles.Add(angles);
            _recentFrames.Add(new FrameSample(Time.time, tool.position, _itemInTool.Count > 0 ? _itemInTool[_itemInTool.Count - 1] : Vector3.zero, angles));
            if (_recentFrames.Count > RecentFrameCount)
            {
                _recentFrames.RemoveAt(0);
            }
        }

        // 직선 관절은 m를 mm로, 회전 관절은 rad를 도로 바꾸는 배율. 관절 종류가 달라도 숫자를 같은 눈으로 읽게 한다.
        private static float DisplayScale(ArticulationBody joint)
        {
            return joint.jointType == ArticulationJointType.RevoluteJoint ? Mathf.Rad2Deg : 1000f;
        }

        // 놓기 직전 마지막 몇 프레임을 프레임마다 적는다. 진동이면 부호가 번갈아 나오고, 부드러운 움직임이면 한 방향으로 이어진다.
        private void DumpRecentFrames()
        {
            _lines.AppendLine("    놓기 직전 프레임별(도구 월드 위치 변화 mm · 물건의 도구 기준 위치 mm · 손가락 관절 위치 mm 또는 °):");
            for (int i = 1; i < _recentFrames.Count; i++)
            {
                FrameSample now = _recentFrames[i];
                Vector3 step = (now.Tool - _recentFrames[i - 1].Tool) * 1000f;
                Vector3 item = now.ItemInTool * 1000f;
                _lines.AppendLine(
                    $"      [{now.Time:0.00}] 변화 ({step.x:+0.0;-0.0;0.0}, {step.y:+0.0;-0.0;0.0}, {step.z:+0.0;-0.0;0.0}) · " +
                    $"물건 ({item.x:+0.0;-0.0;0.0}, {item.y:+0.0;-0.0;0.0}, {item.z:+0.0;-0.0;0.0}) · 손가락 {string.Join(" ", Array.ConvertAll(now.Fingers, v => v.ToString("0.0")))}");
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

        private void FlushWindow(float simSeconds, string label)
        {
            if (_toolPositions.Count < MinWindowSamples)
            {
                return;
            }

            _lines.AppendLine(
                $"  [{simSeconds:0.00}s] {label} {_toolPositions.Count}표본 · 도구 흔들림 {Jitter(_toolPositions)} · 팔 베이스 흔들림 {Jitter(_basePositions)} · " +
                $"물건의 도구 기준 위치 범위(mm) {Range(_itemInTool)} · 손가락 관절 이동 범위(직선 mm, 회전 °) {FingerRange()}");

            // 도구는 조용한데 물건만 크게 움직인 창은 프레임별로 풀어 적는다. 로그가 길어지지 않게 몇 번까지만 적는다.
            Vector3 itemSpan = Span(_itemInTool);
            bool isLarge = Mathf.Max(itemSpan.x, itemSpan.y, itemSpan.z) >= LargeItemSpanMm;
            if (isLarge && _largeSpanDumps < MaxLargeSpanDumps)
            {
                _largeSpanDumps++;
                DumpRecentFrames();
            }
        }

        // 2차 차분의 제곱평균 제곱근과 최댓값(mm). 월드 좌표라 팔이 가속하는 구간도 조금 잡히지만 진동은 훨씬 크게 나온다.
        private static string Jitter(List<Vector3> positions)
        {
            if (positions.Count < 3)
            {
                return "-";
            }

            double sumSquares = 0;
            float max = 0f;
            for (int i = 1; i < positions.Count - 1; i++)
            {
                float magnitude = (positions[i + 1] - 2f * positions[i] + positions[i - 1]).magnitude * 1000f;
                sumSquares += magnitude * magnitude;
                max = Mathf.Max(max, magnitude);
            }

            double rms = Math.Sqrt(sumSquares / (positions.Count - 2));
            return $"{rms:0.00}(최대 {max:0.00})mm";
        }

        private static string Range(List<Vector3> values)
        {
            if (values.Count == 0)
            {
                return "-";
            }

            Vector3 span = Span(values);
            return $"({span.x:0.0}, {span.y:0.0}, {span.z:0.0})";
        }

        // 값들이 축마다 얼마나 퍼졌는지(mm). 비어 있으면 0.
        private static Vector3 Span(List<Vector3> values)
        {
            if (values.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 min = values[0];
            Vector3 max = values[0];
            foreach (Vector3 value in values)
            {
                min = Vector3.Min(min, value);
                max = Vector3.Max(max, value);
            }

            return (max - min) * 1000f;
        }

        private string FingerRange()
        {
            var parts = new List<string>();
            for (int i = 0; i < _fingerJoints.Length; i++)
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                foreach (float[] sample in _fingerAngles)
                {
                    min = Mathf.Min(min, sample[i]);
                    max = Mathf.Max(max, sample[i]);
                }

                // 직선 관절은 mm, 회전 관절은 도로 적는다.
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
                _lines.AppendLine($"    {finger.transform.parent?.name}/{finger.name} ({finger.GetType().Name}) {extent.Describe()}{Penetration(finger)}{OtherOverlaps(finger)}");
            }

            for (int i = 0; i < _fingerJoints.Length; i++)
            {
                ArticulationBody joint = _fingerJoints[i];
                if (joint == null)
                {
                    continue;
                }

                float span = _closedValues[i] - _openValues[i];
                float ratio = Mathf.Abs(span) > 1e-6f ? (joint.jointPosition[0] - _openValues[i]) / span : float.NaN;
                // 구동 목표는 직선 관절이면 m, 회전 관절이면 도다. 현재 위치는 직선 m, 회전 rad이라 같은 단위로 맞춰 적는다.
                bool revolute = joint.jointType == ArticulationJointType.RevoluteJoint;
                string unit = revolute ? "°" : "mm";
                float targetShown = joint.xDrive.target * (revolute ? 1f : 1000f);
                _lines.AppendLine(
                    $"    관절 {joint.name}: 목표 {targetShown:0.0}{unit}, 현재 {joint.jointPosition[0] * DisplayScale(joint):0.0}{unit}, 닫힘 비율 {ratio:0.00} · " +
                    $"구동 강성 {joint.xDrive.stiffness:0} 감쇠 {joint.xDrive.damping:0} 힘한계 {joint.xDrive.forceLimit:0} 종류 {joint.xDrive.driveType}, " +
                    $"관절에 걸린 힘 {joint.jointForce[0]:0.0}, 속도 {joint.jointVelocity[0] * DisplayScale(joint):0.0}{unit}/s, 질량 {joint.mass:0.000}kg, 한계 {joint.xDrive.lowerLimit * 1000f:0.0}~{joint.xDrive.upperLimit * 1000f:0.0}");
            }
        }

        // 이 손가락 충돌체와 겹친 다른 충돌체(물건 제외)를 이름과 깊이(mm)로 적는다. 손가락이 목표보다 더 닫히는 것이 무엇에 밀려서인지 찾는 데 쓴다.
        // 볼록하지 않은 메시는 깊이를 계산할 수 없어 이름만 적는다.
        private string OtherOverlaps(Collider finger)
        {
            Bounds bounds = finger.bounds;
            Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            var parts = new List<string>();

            foreach (Collider other in hits)
            {
                bool isSelfOrItem = other == finger || other.transform.IsChildOf(_item.transform);
                if (isSelfOrItem)
                {
                    continue;
                }

                bool canMeasure = !(finger is MeshCollider fingerMesh && !fingerMesh.convex) && !(other is MeshCollider otherMesh && !otherMesh.convex);
                if (!canMeasure)
                {
                    parts.Add($"{other.transform.parent?.name}/{other.name}(깊이 미상)");
                    continue;
                }

                bool overlaps = Physics.ComputePenetration(
                    finger, finger.transform.position, finger.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 _, out float distance);
                if (overlaps && distance > 0.0003f)
                {
                    parts.Add($"{other.transform.parent?.name}/{other.name} {distance * 1000f:0.0}mm");
                }
            }

            return parts.Count == 0 ? string.Empty : $" · 겹친 것: {string.Join(", ", parts)}";
        }

        // 물건 상자와 이 손가락 충돌체가 겹쳐 있으면 겹친 깊이(mm)를 붙인다. 볼록하지 않은 메시는 계산할 수 없어 건너뛴다.
        private string Penetration(Collider finger)
        {
            Collider box = _item.GetComponent<BoxCollider>();
            if (box == null || (finger is MeshCollider mesh && !mesh.convex))
            {
                return string.Empty;
            }

            bool overlaps = Physics.ComputePenetration(
                box, box.transform.position, box.transform.rotation,
                finger, finger.transform.position, finger.transform.rotation,
                out Vector3 _, out float distance);
            return overlaps ? $" · 물건과 겹침 {distance * 1000f:0.0}mm" : string.Empty;
        }
    }
}
