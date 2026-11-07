using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 감쇠 최소제곱(DLS) 자코비안 방식 역기구학. 자코비안은 순기구학의 수치 미분으로 구해서
    /// 좌표계 방향(Unity는 왼손 좌표계)이나 관절 배치에 대한 가정이 필요 없다.
    /// 회전 관절 N개의 어느 직렬 체인에도 쓸 수 있다.
    /// </summary>
    public sealed class DlsInverseKinematics : IInverseKinematics
    {
        private const int TaskDimension = 6;
        private const float DifferenceStepRad = 1e-3f;

        private readonly KinematicChain _chain;
        private readonly DlsIkSettings _settings;
        private readonly double[,] _jacobian;
        private readonly double[,] _normal = new double[TaskDimension, TaskDimension];
        private readonly double[] _error = new double[TaskDimension];
        private readonly double[] _solved = new double[TaskDimension];
        private readonly float[] _step;
        private readonly double[] _inverseCost;

        public DlsInverseKinematics(KinematicChain chain, DlsIkSettings settings)
        {
            _chain = chain;
            _settings = settings;
            _jacobian = new double[TaskDimension, chain.JointCount];
            _step = new float[chain.JointCount];

            _inverseCost = new double[chain.JointCount];
            for (int i = 0; i < _inverseCost.Length; i++)
            {
                _inverseCost[i] = 1.0 / settings.JointCost(i, chain.JointCount);
            }
        }

        public IkResult Solve(Pose targetInBase, float[] seedRad, float[] resultRad)
        {
            int count = _chain.JointCount;
            Array.Copy(seedRad, resultRad, count);
            _chain.ClampToLimits(resultRad);

            float positionError = 0f;
            float rotationErrorDeg = 0f;

            for (int iteration = 0; iteration < _settings.MaxIterations; iteration++)
            {
                Pose current = _chain.Forward(resultRad);
                Vector3 positionDelta = targetInBase.position - current.position;
                Vector3 rotationDelta = ToRotationVector(targetInBase.rotation * Quaternion.Inverse(current.rotation));

                positionError = positionDelta.magnitude;
                rotationErrorDeg = rotationDelta.magnitude * Mathf.Rad2Deg;

                if (positionError < _settings.PositionToleranceM && rotationErrorDeg < _settings.RotationToleranceDeg)
                {
                    return new IkResult(true, positionError, rotationErrorDeg, iteration);
                }

                BuildJacobian(resultRad, current);
                FillError(positionDelta, rotationDelta);
                SolveStep(count);
                ApplyStep(resultRad, count);
            }

            return new IkResult(false, positionError, rotationErrorDeg, _settings.MaxIterations);
        }

        // 열 i는 관절 i를 아주 조금 움직였을 때 도구 끝점의 위치 변화와 회전 변화다.
        private void BuildJacobian(float[] anglesRad, Pose current)
        {
            Quaternion inverseCurrent = Quaternion.Inverse(current.rotation);
            float rotationWeight = _settings.RotationWeight;

            for (int i = 0; i < _chain.JointCount; i++)
            {
                float original = anglesRad[i];
                anglesRad[i] = original + DifferenceStepRad;
                Pose moved = _chain.Forward(anglesRad);
                anglesRad[i] = original;

                Vector3 positionRate = (moved.position - current.position) / DifferenceStepRad;
                Vector3 rotationRate = ToRotationVector(moved.rotation * inverseCurrent) / DifferenceStepRad;

                _jacobian[0, i] = positionRate.x;
                _jacobian[1, i] = positionRate.y;
                _jacobian[2, i] = positionRate.z;
                _jacobian[3, i] = rotationRate.x * rotationWeight;
                _jacobian[4, i] = rotationRate.y * rotationWeight;
                _jacobian[5, i] = rotationRate.z * rotationWeight;
            }
        }

        private void FillError(Vector3 positionDelta, Vector3 rotationDelta)
        {
            float rotationWeight = _settings.RotationWeight;
            _error[0] = positionDelta.x;
            _error[1] = positionDelta.y;
            _error[2] = positionDelta.z;
            _error[3] = rotationDelta.x * rotationWeight;
            _error[4] = rotationDelta.y * rotationWeight;
            _error[5] = rotationDelta.z * rotationWeight;
        }

        // 관절 변화량 dq = W^-1 Jt (J W^-1 Jt + damping^2 I)^-1 e. W는 관절별 비용이라, 비용이 큰 관절은 덜 움직인다.
        private void SolveStep(int count)
        {
            double lambdaSquared = (double)_settings.Damping * _settings.Damping;

            for (int row = 0; row < TaskDimension; row++)
            {
                for (int column = 0; column < TaskDimension; column++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < count; k++)
                    {
                        sum += _jacobian[row, k] * _jacobian[column, k] * _inverseCost[k];
                    }

                    _normal[row, column] = sum + (row == column ? lambdaSquared : 0.0);
                }

                _solved[row] = _error[row];
            }

            SolveLinearSystem(_normal, _solved);

            float largest = 0f;
            for (int i = 0; i < count; i++)
            {
                double sum = 0.0;
                for (int row = 0; row < TaskDimension; row++)
                {
                    sum += _jacobian[row, i] * _solved[row];
                }

                _step[i] = (float)(sum * _inverseCost[i]);
                largest = Mathf.Max(largest, Mathf.Abs(_step[i]));
            }

            // 한 번에 크게 움직이면 선형 근사가 깨져서 발산하므로, 가장 큰 관절 기준으로 전체를 줄인다.
            float maxStepRad = _settings.MaxStepDeg * Mathf.Deg2Rad;
            if (largest > maxStepRad)
            {
                float scale = maxStepRad / largest;
                for (int i = 0; i < count; i++)
                {
                    _step[i] *= scale;
                }
            }
        }

        private void ApplyStep(float[] anglesRad, int count)
        {
            for (int i = 0; i < count; i++)
            {
                anglesRad[i] += _step[i];
            }

            _chain.ClampToLimits(anglesRad);
        }

        // 쿼터니언 오차를 (회전축 * 각도) 벡터로 바꾼다. 항상 짧은 쪽 회전을 고른다.
        private static Vector3 ToRotationVector(Quaternion rotation)
        {
            float w = rotation.w;
            Vector3 xyz = new Vector3(rotation.x, rotation.y, rotation.z);
            if (w < 0f)
            {
                w = -w;
                xyz = -xyz;
            }

            float sine = xyz.magnitude;
            if (sine < 1e-6f)
            {
                return 2f * xyz;
            }

            float angle = 2f * Mathf.Atan2(sine, w);
            return xyz / sine * angle;
        }

        // 가우스 소거법(부분 피벗). matrix와 rightHandSide를 덮어쓰고 해를 rightHandSide에 넣는다.
        private static void SolveLinearSystem(double[,] matrix, double[] rightHandSide)
        {
            int n = rightHandSide.Length;
            for (int pivot = 0; pivot < n; pivot++)
            {
                int best = pivot;
                for (int row = pivot + 1; row < n; row++)
                {
                    if (Math.Abs(matrix[row, pivot]) > Math.Abs(matrix[best, pivot]))
                    {
                        best = row;
                    }
                }

                if (best != pivot)
                {
                    for (int column = 0; column < n; column++)
                    {
                        (matrix[pivot, column], matrix[best, column]) = (matrix[best, column], matrix[pivot, column]);
                    }

                    (rightHandSide[pivot], rightHandSide[best]) = (rightHandSide[best], rightHandSide[pivot]);
                }

                double diagonal = matrix[pivot, pivot];
                for (int row = pivot + 1; row < n; row++)
                {
                    double factor = matrix[row, pivot] / diagonal;
                    for (int column = pivot; column < n; column++)
                    {
                        matrix[row, column] -= factor * matrix[pivot, column];
                    }

                    rightHandSide[row] -= factor * rightHandSide[pivot];
                }
            }

            for (int row = n - 1; row >= 0; row--)
            {
                double sum = rightHandSide[row];
                for (int column = row + 1; column < n; column++)
                {
                    sum -= matrix[row, column] * rightHandSide[column];
                }

                rightHandSide[row] = sum / matrix[row, row];
            }
        }
    }
}
