using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 박스에 놓는 동안 "미리 가늠한 것과 실제로 하는 것이 같은가"를 가리는 글을 만든다. 읽기만 하고 아무것도 움직이지 않으며,
    /// 언제 부를지는 BoxPlaceRoutine이 정한다. 네 가지를 따진다.
    ///  1. 쥔 물건 중심이 도구에서 얼마나 떨어져 있는가: 멀수록 도구 방향이 달라질 때 도구 자리가 많이 달라진다.
    ///  2. 가늠에 쓴 도구 자리(기본 방향 기준)와 찾은 방향으로 다시 계산한 도구 자리의 차이, 그리고 두 자리 각각의 IK 결과.
    ///  3. 물건이 손에서 미끄러졌는지(지점마다 재서 어느 구간인지), 놓기 직전 물건 중심이 놓으려던 자리에서 얼마나 벗어났는지.
    ///  4. 팔 베이스를 옮기는 동안 물건이 언제 밀리기 시작했는지(스텝마다 잰 요약과 추이): 베이스가 움직이는 중인지, 멈춘 뒤인지, 그때 속도는 얼마인지.
    /// IK 질의는 주입받으므로 씬 없이 시험한다.
    /// </summary>
    public sealed class BoxPlaceDiagnostics
    {
        private const float MetersToCm = 100f;
        private const float MetersToMm = 1000f;

        /// <summary>정하지 않았을 때 쓰는 값: 추이 줄에 적는 스텝의 최대 개수. 넘으면 일정한 간격으로 줄인다.</summary>
        public const int DefaultMaxTraceEntries = 100;

        /// <summary>정하지 않았을 때 쓰는 값: 이만큼(도) 이상 돌았을 때만 추이에 회전 속도를 적는다.</summary>
        public const float DefaultMinYawTravelToTraceDeg = 0.5f;

        // PlacePath.Positions의 순서와 같다.
        private static readonly string[] PointNames = { "슬롯 위", "슬롯", "들어올림" };

        private readonly Func<Pose, IkResult> _probe;
        private readonly int _maxTraceEntries;
        private readonly float _minYawTravelToTraceDeg;

        /// <param name="probe">월드 기준 도구 자세 → 지금 관절 자세에서 푼 IK 결과. 목표와 풀이 상태는 건드리지 않아야 한다.</param>
        /// <param name="maxTraceEntries">추이 줄에 적는 스텝의 최대 개수(1 이상). 넘으면 일정한 간격으로 줄인다.</param>
        /// <param name="minYawTravelToTraceDeg">
        /// 이만큼(도) 이상 돌았을 때만 추이에 회전 속도를 적는다. 돌지 않은 이동의 회전 속도는 전부 0이라 줄만 길어진다.
        /// </param>
        public BoxPlaceDiagnostics(
            Func<Pose, IkResult> probe,
            int maxTraceEntries = DefaultMaxTraceEntries,
            float minYawTravelToTraceDeg = DefaultMinYawTravelToTraceDeg)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _maxTraceEntries = Mathf.Max(1, maxTraceEntries);
            _minYawTravelToTraceDeg = minYawTravelToTraceDeg;
        }

        /// <summary>쥔 물건 중심이 도구 기준으로 어디에 있고 얼마나 먼지.</summary>
        public string DescribeHeldFrame(HeldItemFrame frame)
        {
            Vector3 center = frame.CenterInTool;
            return $"쥔 물건 중심은 도구 기준 ({Cm(center.x)}, {Cm(center.y)}, {Cm(center.z)})cm에 있고 거리는 {Cm(center.magnitude)}cm다.";
        }

        /// <summary>
        /// 가늠에 쓴 도구 자리(tested)와 찾은 방향으로 다시 계산한 도구 자리(executed)가 지점마다 얼마나 다른지, 그리고
        /// 찾은 방향 rotation으로 각 자리를 풀면 어떻게 되는지(IK).
        /// </summary>
        public string DescribePlan(PlacePath tested, PlacePath executed, Quaternion rotation)
        {
            Vector3[] testedPositions = tested.Positions;
            Vector3[] executedPositions = executed.Positions;

            var shifts = new string[testedPositions.Length];
            for (int i = 0; i < shifts.Length; i++)
            {
                shifts[i] = $"{PointNames[i]} {Cm(Vector3.Distance(testedPositions[i], executedPositions[i]))}cm";
            }

            return $"가늠에 쓴 도구 자리와 실행할 도구 자리의 차이 — {string.Join(", ", shifts)}. " +
                   $"가늠한 자리로 풀면 [{DescribeReach(testedPositions, rotation)}], 실행할 자리로 풀면 [{DescribeReach(executedPositions, rotation)}]";
        }

        /// <summary>
        /// 한 지점(label)에서 잰 쥔 물건의 미끄러짐: 직전 지점 뒤로 얼마, 처음 잰 뒤로 얼마(축별 값 포함) 움직이고 돌았는지.
        /// 직전 지점 값이 큰 지점이 미끄러진 구간의 끝이다.
        /// </summary>
        public string DescribeSlipMark(string label, SlipMark mark)
        {
            Vector3 total = mark.SinceFirstInTool;
            return $"미끄러짐 [{label}] 직전 지점 뒤로 {Cm(mark.SinceLastInTool.magnitude)}cm {mark.SinceLastDeg:0.0}도, " +
                   $"처음 잰 뒤로 도구 기준 ({Cm(total.x)}, {Cm(total.y)}, {Cm(total.z)})cm({Cm(total.magnitude)}cm) {mark.SinceFirstDeg:0.0}도";
        }

        /// <summary>놓기 직전의 물건 중심이 놓으려던 자리에서 벗어난 정도와, 도구가 목표에서 벗어난 정도.</summary>
        public string DescribeRelease(Vector3 expectedItemCenter, Vector3 itemCenter, Vector3 toolTarget, Vector3 tool)
        {
            Vector3 itemOff = itemCenter - expectedItemCenter;
            return $"물건 중심이 놓으려던 자리에서 ({Cm(itemOff.x)}, {Cm(itemOff.y)}, {Cm(itemOff.z)})cm({Cm(itemOff.magnitude)}cm) 벗어나 있고, " +
                   $"도구는 목표에서 {Cm(Vector3.Distance(toolTarget, tool))}cm 벗어나 있다.";
        }

        // 각 자리를 풀었을 때의 수렴 여부와 오차. 오차는 mm로 적는다(수렴 기준이 2mm라 cm로는 구별할 수 없다).
        private string DescribeReach(Vector3[] positions, Quaternion rotation)
        {
            var parts = new string[positions.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                IkResult ik = _probe(new Pose(positions[i], rotation));
                string verdict = ik.Converged ? "수렴" : "수렴 못함";
                parts[i] = $"{PointNames[i]}: {verdict} 위치 {ik.PositionError * MetersToMm:0.0}mm/회전 {ik.RotationErrorDeg:0.0}도";
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// 팔 베이스를 한 번 옮기는 동안의 요약: 베이스가 얼마나 어떻게 움직였는지, 쥔 물건이 얼마나 밀렸는지, 기준 거리를 처음 넘은 때가
        /// 베이스가 움직이는 중이었는지 멈춘 뒤였는지. arrived가 false이면 도착하기 전에 끝난 이동이다(실패, 중단).
        /// </summary>
        public string DescribeBaseMotion(string label, BaseMotionSummary summary, bool arrived, float onsetSlipM)
        {
            string ending = arrived ? "도착" : "도착하지 못하고 끝남";
            string motion = summary.HasMotion
                ? $"{Cm(summary.DistanceM)}cm 이동 {summary.YawTravelDeg:0}도 회전(최고 {summary.MaxSpeedMps:0.00}m/s, {summary.MaxYawRateDegPerSec:0}도/초)"
                : $"베이스는 움직이지 않았다({Cm(summary.DistanceM)}cm, {summary.YawTravelDeg:0.0}도)";

            Vector3 end = summary.SlipAtEndInTool;
            string slip = $"쥔 물건 밀림 최대 {Cm(summary.MaxSlipM)}cm, 끝 {Cm(summary.SlipAtEndM)}cm " +
                          $"도구 기준 ({Cm(end.x)}, {Cm(end.y)}, {Cm(end.z)})cm {summary.SlipAtEndDeg:0.0}도";
            if (summary.HasMotion)
            {
                slip += $", 베이스가 마지막으로 움직인 때 {Cm(summary.SlipAtLastMotionM)}cm";
            }

            return $"베이스 이동 [{label}] {ending}, {summary.DurationSeconds:0.00}초 동안 {motion}. {slip}. " +
                   $"{DescribeSlipOnset(summary, onsetSlipM)}";
        }

        /// <summary>
        /// 같은 이동의 스텝별 추이: 베이스 속도, (돌았으면) 회전 속도, 쥔 물건 밀림. 스텝이 많으면 일정한 간격으로 줄여 적는다.
        /// 속도가 오르내리는 모양과 밀림이 커지는 시점을 나란히 볼 수 있다.
        /// </summary>
        public string DescribeBaseMotionTrace(string label, BaseMotionSummary summary)
        {
            int stride = Mathf.Max(1, Mathf.CeilToInt(summary.SampleCount / (float)_maxTraceEntries));
            string interval = stride > 1 ? $", {stride}스텝마다" : string.Empty;
            string text = $"베이스 이동 [{label}] 추이({summary.SampleCount}스텝, 간격 {summary.StepSeconds:0.000}초{interval}) " +
                          $"속도(m/s) [{Series(summary.SpeedsMps, stride, "0.00", 1f)}]";

            if (summary.YawTravelDeg >= _minYawTravelToTraceDeg)
            {
                text += $", 회전(도/초) [{Series(summary.YawRatesDegPerSec, stride, "0", 1f)}]";
            }

            return text + $", 밀림(cm) [{Series(summary.SlipsM, stride, "0.0", MetersToCm)}]";
        }

        private static string DescribeSlipOnset(BaseMotionSummary summary, float onsetSlipM)
        {
            if (!summary.HasOnset)
            {
                return $"밀림이 {Cm(onsetSlipM)}cm를 넘지 않았다.";
            }

            string when;
            if (summary.OnsetStepsSinceMotion < 0)
            {
                when = "베이스가 움직이기 전";
            }
            else if (summary.OnsetWhileMoving)
            {
                when = "베이스가 움직이는 중";
            }
            else
            {
                when = $"베이스가 멈추고 {summary.OnsetStepsSinceMotion}스텝 뒤";
            }

            return $"밀림이 {Cm(onsetSlipM)}cm를 처음 넘은 때는 이동 시작 {summary.OnsetSeconds:0.00}초 뒤, {when}" +
                   $"(속도 {summary.OnsetSpeedMps:0.00}m/s, 회전 {summary.OnsetYawRateDegPerSec:0}도/초, 그때까지 {Cm(summary.OnsetDistanceM)}cm 이동).";
        }

        // stride 간격으로 값을 뽑아 적는다. 마지막 값은 간격과 상관없이 넣는다(끝에서 어떻게 됐는지가 중요하다).
        private static string Series(IReadOnlyList<float> values, int stride, string format, float scale)
        {
            var parts = new List<string>();
            for (int i = 0; i < values.Count; i += stride)
            {
                parts.Add((values[i] * scale).ToString(format));
            }

            int last = values.Count - 1;
            if (last >= 0 && last % stride != 0)
            {
                parts.Add((values[last] * scale).ToString(format));
            }

            return string.Join(" ", parts);
        }

        private static string Cm(float meters)
        {
            return (meters * MetersToCm).ToString("0.0");
        }
    }
}
