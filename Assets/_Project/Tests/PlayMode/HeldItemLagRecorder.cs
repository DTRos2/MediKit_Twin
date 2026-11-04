#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 물건을 쥐고 옮기는 동안, 물건이 도구 끝에 붙어 있는 자리에서 화면에 얼마나 어긋나 보이는지 프레임마다 잰다. 읽기만 하며 키팅에는 손대지 않는다.
    /// "쥐고 돌면 물건이 뒤늦게 따라와 분리된 것처럼 보인다"는 말이 사실인지, 사실이라면 베이스가 돌 때인지 팔이 움직일 때인지 가리려고 만들었다.
    /// 어긋남은 쥔 지 잠시 뒤(손가락이 가라앉은 뒤)의 도구 기준 위치를 바탕으로 삼아, 그 뒤 도구 기준 위치가 얼마나 변했는지(mm)로 적는다.
    /// 쥔 채로 고정되어 있다면 이 값은 0에 가깝다.
    /// 재는 시점은 그 프레임의 마지막(LateUpdate, 실행 순서 맨 끝)이라 카메라가 그리는 자세와 같다.
    /// 환경 변수 MEDIKIT_E2E_LAG_TRACE=1일 때만 시험이 결과를 로그에 남긴다.
    /// </summary>
    public sealed class HeldItemLagRecorder
    {
        public const string EnableVariable = "MEDIKIT_E2E_LAG_TRACE";

        // 도구가 이 속도(m/초)와 각속도(도/초) 미만으로 이 시간(초) 이상 가만히 있으면 물건이 자리를 잡은 것으로 보고,
        // 그때의 도구 기준 위치를 바탕으로 삼는다. 움직이는 중에 바탕을 잡으면 이미 어긋난 자리가 바탕이 되어 어긋남이 줄어 보인다.
        private const float StillMps = 0.02f;
        private const float StillDegPerSec = 5f;
        private const float StillSettleSeconds = 0.3f;

        // 이 각속도(도/초) 이상이면 도구가 돌고 있다고 본다.
        private const float TurningDegPerSec = 20f;

        // 이 속도(m/초) 이상이면 도구가 옮겨 가고 있다고 본다.
        private const float MovingMps = 0.05f;

        // 이만큼(mm) 이상 어긋난 프레임은 눈에 띄는 어긋남으로 센다.
        private const float VisibleLagMm = 5f;

        private const int WorstFrameCount = 8;
        private const float MetersToMm = 1000f;

        private readonly JointGripper _gripper;
        private readonly ArmController _arm;
        private readonly List<Frame> _worst = new List<Frame>();
        private readonly Bucket _turning = new Bucket("도구가 도는 중");
        private readonly Bucket _moving = new Bucket("도구가 옮겨 가는 중");
        private readonly Bucket _still = new Bucket("도구가 거의 멈춤");

        private Graspable _item;
        private float _stillSince;
        private bool _wasStill;
        private bool _hasBaseline;
        private Vector3 _baselineInTool;
        private Quaternion _baselineRotationInTool;
        private bool _hasPrevious;
        private float _previousTime;
        private Vector3 _previousTool;
        private Quaternion _previousToolRotation;

        public HeldItemLagRecorder(JointGripper gripper, ArmController arm)
        {
            _gripper = gripper;
            _arm = arm;
            LateUpdateRelay.Create(Sample);
        }

        public static bool IsEnabled => Environment.GetEnvironmentVariable(EnableVariable) == "1";

        /// <summary>쥔 뒤 눈에 띄게(VisibleLagMm 이상) 어긋난 프레임 수.</summary>
        public int VisibleLagFrames => _turning.VisibleFrames + _moving.VisibleFrames + _still.VisibleFrames;

        /// <summary>쥔 뒤 가장 크게 어긋난 값(mm).</summary>
        public float MaxLagMm => Mathf.Max(_turning.MaxLagMm, _moving.MaxLagMm, _still.MaxLagMm);

        public string Describe()
        {
            var text = new StringBuilder();
            text.AppendLine(Invariant($"  가장 크게 어긋난 값 {MaxLagMm:0.0}mm, {VisibleLagMm:0}mm 이상 어긋난 프레임 {VisibleLagFrames}개"));
            text.AppendLine(_turning.Describe());
            text.AppendLine(_moving.Describe());
            text.AppendLine(_still.Describe());
            text.AppendLine("  가장 크게 어긋난 프레임:");
            foreach (Frame frame in _worst)
            {
                text.AppendLine(Invariant(
                    $"    t={frame.Time:0.00}s 어긋남 {frame.LagMm:0.0}mm(기울기 {frame.TiltDeg:0.0}°), 도구 {frame.ToolSpeedMps:0.00}m/s {frame.ToolTurnDegPerSec:0}°/s"));
            }

            return text.ToString();
        }

        private void Sample()
        {
            if (_gripper == null || _arm == null || !_arm.IsReady || !_gripper.IsHolding || _gripper.HeldObject == null)
            {
                _item = null;
                _hasBaseline = false;
                _hasPrevious = false;
                return;
            }

            Pose tool = _arm.ToolPose;
            if (_gripper.HeldObject != _item)
            {
                _item = _gripper.HeldObject;
                _wasStill = false;
                _hasBaseline = false;
            }

            Transform itemTransform = _item.transform;
            Quaternion inverseTool = Quaternion.Inverse(tool.rotation);
            Vector3 inTool = inverseTool * (itemTransform.position - tool.position);
            Quaternion rotationInTool = inverseTool * itemTransform.rotation;

            float dt = Time.time - _previousTime;
            float speed = 0f;
            float turn = 0f;
            if (_hasPrevious && dt > 0f)
            {
                speed = Vector3.Distance(tool.position, _previousTool) / dt;
                turn = Quaternion.Angle(tool.rotation, _previousToolRotation) / dt;
            }

            _hasPrevious = true;
            _previousTime = Time.time;
            _previousTool = tool.position;
            _previousToolRotation = tool.rotation;

            if (speed < StillMps && turn < StillDegPerSec)
            {
                if (!_wasStill)
                {
                    _stillSince = Time.time;
                }

                _wasStill = true;
                if (Time.time - _stillSince >= StillSettleSeconds)
                {
                    _hasBaseline = true;
                    _baselineInTool = inTool;
                    _baselineRotationInTool = rotationInTool;
                }
            }
            else
            {
                _wasStill = false;
            }

            if (!_hasBaseline)
            {
                return;
            }

            float lagMm = Vector3.Distance(inTool, _baselineInTool) * MetersToMm;
            float tiltDeg = Quaternion.Angle(rotationInTool, _baselineRotationInTool);
            Bucket bucket = turn >= TurningDegPerSec ? _turning : speed >= MovingMps ? _moving : _still;
            bucket.Add(lagMm, VisibleLagMm);
            Remember(new Frame(Time.time, lagMm, tiltDeg, speed, turn));
        }

        private void Remember(Frame frame)
        {
            _worst.Add(frame);
            _worst.Sort((a, b) => b.LagMm.CompareTo(a.LagMm));
            if (_worst.Count > WorstFrameCount)
            {
                _worst.RemoveAt(_worst.Count - 1);
            }
        }

        private static string Invariant(FormattableString value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private readonly struct Frame
        {
            public Frame(float time, float lagMm, float tiltDeg, float toolSpeedMps, float toolTurnDegPerSec)
            {
                Time = time;
                LagMm = lagMm;
                TiltDeg = tiltDeg;
                ToolSpeedMps = toolSpeedMps;
                ToolTurnDegPerSec = toolTurnDegPerSec;
            }

            public float Time { get; }

            public float LagMm { get; }

            public float TiltDeg { get; }

            public float ToolSpeedMps { get; }

            public float ToolTurnDegPerSec { get; }
        }

        private sealed class Bucket
        {
            private readonly string _label;
            private int _count;
            private int _visible;
            private float _sum;

            public Bucket(string label)
            {
                _label = label;
            }

            public int VisibleFrames => _visible;

            public float MaxLagMm { get; private set; }

            public void Add(float lagMm, float visibleMm)
            {
                _count++;
                _sum += lagMm;
                MaxLagMm = Mathf.Max(MaxLagMm, lagMm);
                _visible += lagMm >= visibleMm ? 1 : 0;
            }

            public string Describe()
            {
                return _count == 0
                    ? Invariant($"  {_label}: 프레임 없음")
                    : Invariant($"  {_label}: 프레임 {_count}개, 평균 어긋남 {_sum / _count:0.0}mm, 최대 {MaxLagMm:0.0}mm, 눈에 띄는 프레임 {_visible}개");
            }
        }
    }

    /// <summary>실행 순서 맨 끝의 LateUpdate에서 한 번 부르는 중계. 카메라가 그리기 직전의 자세를 재려고 쓴다.</summary>
    [DefaultExecutionOrder(32000)]
    internal sealed class LateUpdateRelay : MonoBehaviour
    {
        private Action _callback;

        public static void Create(Action callback)
        {
            var host = new GameObject("LateUpdateRelay") { hideFlags = HideFlags.HideAndDontSave };
            host.AddComponent<LateUpdateRelay>()._callback = callback;
        }

        private void LateUpdate()
        {
            _callback?.Invoke();
        }
    }
}
#endif
