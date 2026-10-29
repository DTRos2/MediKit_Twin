namespace MediKitTwin.Common
{
    /// <summary>역기구학 풀이 결과. Converged가 false여도 resultRad에는 가장 가까운 해가 들어 있다.</summary>
    public readonly struct IkResult
    {
        public readonly bool Converged;
        public readonly float PositionError;
        public readonly float RotationErrorDeg;
        public readonly int Iterations;

        public IkResult(bool converged, float positionError, float rotationErrorDeg, int iterations)
        {
            Converged = converged;
            PositionError = positionError;
            RotationErrorDeg = rotationErrorDeg;
            Iterations = iterations;
        }
    }
}
