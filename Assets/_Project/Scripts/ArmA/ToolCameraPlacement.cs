using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 도구 끝 카메라의 자리를 정한다(도구 끝점 기준 좌표, +Z가 물건 쪽). 장면을 읽지 않는 계산이라 단독으로 시험할 수 있다.
    /// 그리퍼 가운데에 달 때(AtHandCenter): 몸통 옆·위아래 한가운데 앞끝에 놓고 앞을 본다(위로 들어 올리는 각도를 줄 수 있다).
    /// 손 몸통 옆면에 달 때: 몸통 크기(도구 끝점 좌표)에서 옆면 한가운데, 앞쪽 끝 가까이에 놓고,
    /// 도구 끝점 앞쪽 가운데(손가락 사이 앞)를 보게 비튼다. 몸통이 시야를 가리지 않고 손가락과 그 앞 물건이 함께 보인다.
    /// </summary>
    public static class ToolCameraPlacement
    {
        /// <param name="hasBody">몸통 크기를 잰 결과가 있는지. 없으면 설정한 위치와 방향으로 돌아간다.</param>
        /// <param name="body">도구 끝점 좌표로 잰 몸통 크기.</param>
        public static Pose Resolve(ToolCameraSettings settings, bool hasBody, Bounds body)
        {
            if (settings.Mount == ToolCameraMount.Manual || !hasBody)
            {
                return new Pose(settings.LocalPosition, settings.LocalRotation);
            }

            if (settings.Mount == ToolCameraMount.HandCenter)
            {
                return AtHandCenter(body, settings.ForwardFraction, settings.Nudge, settings.PitchUpDeg);
            }

            return OnHandSide(
                body,
                settings.Mount == ToolCameraMount.HandRightSide,
                settings.SurfaceOffsetM,
                settings.ForwardFraction,
                settings.LookAheadM,
                settings.Nudge);
        }

        /// <summary>
        /// 그리퍼 가운데: 몸통의 옆(X)과 위아래(Y) 한가운데에 놓고 도구 끝점과 같은 쪽(+Z)을 똑바로 보게 한다.
        /// 두 손가락 사이 한가운데에서 앞을 보는 자리다. 몸통은 카메라 뒤에 있어 시야를 가리지 않는다.
        /// </summary>
        /// <param name="forwardFraction">몸통 뒤끝(0)에서 앞끝(1) 사이 어디에 놓을지. 1이면 몸통 앞끝이다.</param>
        /// <param name="nudge">자리를 잡은 뒤 도구 끝점 좌표로 더 옮길 양.</param>
        /// <param name="pitchUpDeg">카메라를 위(+Y)로 들어 올리는 각도. 0이면 도구 끝점과 같은 쪽을 똑바로 본다. 유니티는 X축 양의 회전이 아래를 보므로 음수로 돌린다.</param>
        public static Pose AtHandCenter(Bounds body, float forwardFraction, Vector3 nudge, float pitchUpDeg = 0f)
        {
            var position = new Vector3(
                body.center.x,
                body.center.y,
                Mathf.Lerp(body.min.z, body.max.z, Mathf.Clamp01(forwardFraction))) + nudge;

            return new Pose(position, Quaternion.Euler(-pitchUpDeg, 0f, 0f));
        }

        /// <param name="rightSide">참이면 +X 옆면, 아니면 −X 옆면.</param>
        /// <param name="surfaceOffsetM">옆면에서 바깥으로 띄우는 거리.</param>
        /// <param name="forwardFraction">몸통 뒤끝(0)에서 앞끝(1) 사이 어디에 놓을지.</param>
        /// <param name="lookAheadM">도구 끝점에서 앞으로 이만큼 떨어진 축 위의 점을 본다.</param>
        /// <param name="nudge">자리를 잡은 뒤 도구 끝점 좌표로 더 옮길 양.</param>
        public static Pose OnHandSide(Bounds body, bool rightSide, float surfaceOffsetM, float forwardFraction, float lookAheadM, Vector3 nudge)
        {
            float sign = rightSide ? 1f : -1f;
            float face = rightSide ? body.max.x : body.min.x;

            var position = new Vector3(
                face + sign * surfaceOffsetM,
                body.center.y,
                Mathf.Lerp(body.min.z, body.max.z, Mathf.Clamp01(forwardFraction))) + nudge;

            Vector3 toTarget = new Vector3(0f, 0f, lookAheadM) - position;
            Quaternion rotation = toTarget.sqrMagnitude > 1e-8f
                ? Quaternion.LookRotation(toTarget, Vector3.up)
                : Quaternion.identity;

            return new Pose(position, rotation);
        }
    }
}
