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
    /// 수렴한 해가 없으면 제한 안에서 오차가 가장 작은 미수렴 풀이를 돌려준다(Converged=false). 오차를 느슨하게 받아 주는 쪽
    /// (IkSolveAcceptance)이 쓸 수 있게 하려는 것이다. 제한 안의 풀이가 하나도 없으면 먼 풀이가 결과 배열에 남으므로,
    /// 미수렴 풀이를 쓰는 쪽은 관절 이동량(JointTravel)을 반드시 확인해야 한다.
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

            // 수렴하지는 못했지만 관절 이동 제한 안에 있는 풀이 가운데 오차가 가장 작은 것. 오차 허용을 느슨하게 받는 쪽이
            // 쓸 수 있도록 남긴다. 제한을 넘는 풀이는 오차가 작아도 뒤집힌 자세이므로 여기에 넣지 않는다.
            bool hasApproximate = IsTravelLimited && !best.Converged && IsWithinTravel(seedRad, resultRad);
            IkResult approximate = best;

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

                float travel = JointTravel.Largest(seedRad, _candidate);
                bool isWithinTravel = travel <= _maxJointTravelRad;

                if (candidate.Converged)
                {
                    if (isWithinTravel && travel < nearestTravel)
                    {
                        Array.Copy(_candidate, resultRad, _candidate.Length);
                        nearestTravel = travel;
                        nearest = candidate;
                        hasNearSolution = true;
                    }
                }
                else if (isWithinTravel && !hasNearSolution && (!hasApproximate || IsCloser(candidate, approximate)))
                {
                    Array.Copy(_candidate, resultRad, _candidate.Length);
                    approximate = candidate;
                    hasApproximate = true;
                }
                else if (!isWithinTravel && !hasNearSolution && !hasApproximate && !best.Converged && IsCloser(candidate, best))
                {
                    // 쓸 수 있는 풀이가 하나도 없을 때 실패 원인을 설명하려고, 가장 가까웠던 먼 풀이의 오차를 남긴다.
                    Array.Copy(_candidate, resultRad, _candidate.Length);
                    best = candidate;
                }
            }

            if (!IsTravelLimited)
            {
                return best;
            }

            // 먼 해만 있으면 수렴하지 못한 것으로 돌려준다. 먼 해를 쓰면 팔이 뒤집히기 때문이다.
            if (hasNearSolution)
            {
                return nearest;
            }

            IkResult closest = hasApproximate ? approximate : best;
            return new IkResult(false, closest.PositionError, closest.RotationErrorDeg, closest.Iterations);
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
            return !IsTravelLimited || JointTravel.Largest(seedRad, solutionRad) <= _maxJointTravelRad;
        }

        // 수렴하지 못했을 때는 위치 오차가 더 작은 쪽을 가까운 해로 본다.
        private static bool IsCloser(IkResult candidate, IkResult best)
        {
            return candidate.PositionError < best.PositionError;
        }
    }
}
