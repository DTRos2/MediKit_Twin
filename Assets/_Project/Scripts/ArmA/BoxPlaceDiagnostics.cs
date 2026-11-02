using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 박스에 놓는 동안 "미리 가늠한 것과 실제로 하는 것이 같은가"를 가리는 글을 만든다. 읽기만 하고 아무것도 움직이지 않으며,
    /// 언제 부를지는 BoxPlaceRoutine이 정한다. 세 가지를 따진다.
    ///  1. 쥔 물건 중심이 도구에서 얼마나 떨어져 있는가: 멀수록 도구 방향이 달라질 때 도구 자리가 많이 달라진다.
    ///  2. 가늠에 쓴 도구 자리(기본 방향 기준)와 찾은 방향으로 다시 계산한 도구 자리의 차이, 그리고 두 자리 각각의 IK 결과.
    ///  3. 물건이 손에서 미끄러졌는지(지점마다 재서 어느 구간인지), 놓기 직전 물건 중심이 놓으려던 자리에서 얼마나 벗어났는지.
    /// IK 질의는 주입받으므로 씬 없이 시험한다.
    /// </summary>
    public sealed class BoxPlaceDiagnostics
    {
        private const float MetersToCm = 100f;
        private const float MetersToMm = 1000f;

        // PlacePath.Positions의 순서와 같다.
        private static readonly string[] PointNames = { "슬롯 위", "슬롯", "들어올림" };

        private readonly Func<Pose, IkResult> _probe;

        /// <param name="probe">월드 기준 도구 자세 → 지금 관절 자세에서 푼 IK 결과. 목표와 풀이 상태는 건드리지 않아야 한다.</param>
        public BoxPlaceDiagnostics(Func<Pose, IkResult> probe)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
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

        private static string Cm(float meters)
        {
            return (meters * MetersToCm).ToString("0.0");
        }
    }
}
