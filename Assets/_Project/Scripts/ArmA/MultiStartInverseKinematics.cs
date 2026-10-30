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
    /// </summary>
    public sealed class MultiStartInverseKinematics : IInverseKinematics
    {
        private readonly IInverseKinematics _inner;
        private readonly KinematicChain _chain;
        private readonly int _restarts;
        private readonly int _seed;
        private readonly float[] _candidateSeed;
        private readonly float[] _candidate;

        public MultiStartInverseKinematics(IInverseKinematics inner, KinematicChain chain, int restarts, int seed)
        {
            _inner = inner;
            _chain = chain;
            _restarts = Math.Max(restarts, 0);
            _seed = seed;
            _candidateSeed = new float[chain.JointCount];
            _candidate = new float[chain.JointCount];
        }

        public IkResult Solve(Pose targetInBase, float[] seedRad, float[] resultRad)
        {
            IkResult best = _inner.Solve(targetInBase, seedRad, resultRad);
            if (best.Converged)
            {
                return best;
            }

            var random = new System.Random(_seed);
            for (int attempt = 0; attempt < _restarts; attempt++)
            {
                FillRandomSeed(random, _candidateSeed);
                IkResult candidate = _inner.Solve(targetInBase, _candidateSeed, _candidate);

                if (candidate.Converged || IsCloser(candidate, best))
                {
                    Array.Copy(_candidate, resultRad, _candidate.Length);
                    best = candidate;
                    if (best.Converged)
                    {
                        break;
                    }
                }
            }

            return best;
        }

        private void FillRandomSeed(System.Random random, float[] seed)
        {
            for (int i = 0; i < seed.Length; i++)
            {
                KinematicChain.JointSpec joint = _chain.GetJoint(i);
                seed[i] = joint.LowerRad + (float)random.NextDouble() * (joint.UpperRad - joint.LowerRad);
            }
        }

        // 수렴하지 못했을 때는 위치 오차가 더 작은 쪽을 가까운 해로 본다.
        private static bool IsCloser(IkResult candidate, IkResult best)
        {
            return candidate.PositionError < best.PositionError;
        }
    }
}
