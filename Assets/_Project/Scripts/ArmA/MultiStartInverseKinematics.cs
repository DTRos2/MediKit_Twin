using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 다른 IK를 감싸서, 수렴하지 못하면 관절 한계 안의 무작위 자세에서 다시 푼다.
    /// 국소 해(local minimum)에 빠진 경우를 건너뛰어 성공률을 올린다. 풀이 방법(DLS, 해석해)은 안쪽 IK가 정하므로
    /// 어떤 IInverseKinematics에도 씌울 수 있다. 풀 때마다 같은 시드로 난수를 다시 시작하므로 같은 입력이면 언제 풀어도 같은 결과가 나온다.
    /// (난수를 이어서 쓰면 사전 확인(Probe)에서 수렴한 목표가 실제 이동에서는 다른 재시작 자세를 뽑아 실패한다.)
    /// maxJointTravelRad가 0보다 크면 현재 자세에서 어느 관절이든 그 각도보다 더 돌아야 하는 해는 쓰지 않는다.
    /// 팔이 뒤집힌 먼 자세로 가다가 선반에 걸리거나 시간 안에 닿지 못하는 것을 막기 위해서다. 이때는 현재 자세에 가까운
    /// 시작점부터 차례로 멀어지며 풀고, 수렴한 해 중 가장 덜 움직이는 것을 고른다.
    /// </summary>
    public sealed class MultiStartInverseKinematics : IInverseKinematics
    {
        private readonly IInverseKinematics _inner;
        private readonly KinematicChain _chain;
        private readonly int _restarts;
        private readonly int _seed;
        private readonly float _maxJointTravelRad;
        private readonly float[] _candidateSeed;
        private readonly float[] _candidate;

        /// <param name="maxJointTravelRad">현재 자세에서 한 관절이 움직일 수 있는 최대 각도(rad). 0 이하면 제한하지 않는다.</param>
        public MultiStartInverseKinematics(
            IInverseKinematics inner, KinematicChain chain, int restarts, int seed, float maxJointTravelRad = 0f)
        {
            _inner = inner;
            _chain = chain;
            _restarts = Math.Max(restarts, 0);
            _seed = seed;
            _maxJointTravelRad = maxJointTravelRad;
            _candidateSeed = new float[chain.JointCount];
            _candidate = new float[chain.JointCount];
        }

        private bool IsTravelLimited => _maxJointTravelRad > 0f;

        public IkResult Solve(Pose targetInBase, float[] seedRad, float[] resultRad)
        {
            IkResult best = _inner.Solve(targetInBase, seedRad, resultRad);
            if (best.Converged && IsWithinTravel(seedRad, resultRad))
            {
                return best;
            }

            var random = new System.Random(_seed);
            bool hasNearSolution = false;
            float nearestTravel = float.MaxValue;
            IkResult nearest = best;

            for (int attempt = 0; attempt < _restarts; attempt++)
            {
                FillStartPose(random, seedRad, attempt, _candidateSeed);
                IkResult candidate = _inner.Solve(targetInBase, _candidateSeed, _candidate);

                if (!IsTravelLimited)
                {
                    if (candidate.Converged || IsCloser(candidate, best))
                    {
                        Array.Copy(_candidate, resultRad, _candidate.Length);
                        best = candidate;
                        if (best.Converged)
                        {
                            break;
                        }
                    }

                    continue;
                }

                if (candidate.Converged)
                {
                    float travel = LargestTravel(seedRad, _candidate);
                    if (travel <= _maxJointTravelRad && travel < nearestTravel)
                    {
                        Array.Copy(_candidate, resultRad, _candidate.Length);
                        nearestTravel = travel;
                        nearest = candidate;
                        hasNearSolution = true;
                    }
                }
                else if (!hasNearSolution && !best.Converged && IsCloser(candidate, best))
                {
                    Array.Copy(_candidate, resultRad, _candidate.Length);
                    best = candidate;
                }
            }

            if (!IsTravelLimited)
            {
                return best;
            }

            // 먼 해만 있으면 수렴하지 못한 것으로 돌려준다. 먼 해를 쓰면 팔이 뒤집히기 때문이다.
            return hasNearSolution
                ? nearest
                : new IkResult(false, best.PositionError, best.RotationErrorDeg, best.Iterations);
        }

        // 제한이 없으면 관절 한계 안의 무작위 자세, 있으면 현재 자세에서 시작해 시도마다 무작위 자세 쪽으로 조금씩 멀어진다.
        private void FillStartPose(System.Random random, float[] seedRad, int attempt, float[] start)
        {
            float blend = IsTravelLimited ? (attempt + 1f) / _restarts : 1f;

            for (int i = 0; i < start.Length; i++)
            {
                KinematicChain.JointSpec joint = _chain.GetJoint(i);
                float randomAngle = joint.LowerRad + (float)random.NextDouble() * (joint.UpperRad - joint.LowerRad);
                start[i] = Mathf.Lerp(seedRad[i], randomAngle, blend);
            }
        }

        private bool IsWithinTravel(float[] seedRad, float[] solutionRad)
        {
            return !IsTravelLimited || LargestTravel(seedRad, solutionRad) <= _maxJointTravelRad;
        }

        private static float LargestTravel(float[] fromRad, float[] toRad)
        {
            float largest = 0f;
            for (int i = 0; i < fromRad.Length; i++)
            {
                largest = Mathf.Max(largest, Mathf.Abs(toRad[i] - fromRad[i]));
            }

            return largest;
        }

        // 수렴하지 못했을 때는 위치 오차가 더 작은 쪽을 가까운 해로 본다.
        private static bool IsCloser(IkResult candidate, IkResult best)
        {
            return candidate.PositionError < best.PositionError;
        }
    }
}
