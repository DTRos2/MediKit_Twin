using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 씬에 import된 ArticulationBody 체인에서 KinematicChain을 만든다.
    /// 현재 자세에서 각 링크의 실제 변환을 읽고 관절 각도만큼 되돌려, 각도 0 기준의 상수 변환을 얻는다.
    /// 그래서 어느 자세에서 만들어도 같은 결과가 나오고, 로봇 종류(FR5, UR3)별 코드가 필요 없다.
    /// 현재 각도는 호출하는 쪽이 준다(물리 엔진이 아직 준비되지 않았거나 기준이 밀린 경우를 호출하는 쪽이 판단한다).
    /// </summary>
    public static class KinematicChainBuilder
    {
        public static KinematicChain Build(
            Transform baseLink,
            IReadOnlyList<ArticulationBody> joints,
            Transform toolLink,
            Vector3 toolPositionInToolLink,
            Quaternion toolRotationInToolLink,
            IReadOnlyList<float> currentAnglesRad)
        {
            var specs = new KinematicChain.JointSpec[joints.Count];
            Vector3 previousPosition = baseLink.position;
            Quaternion previousRotation = baseLink.rotation;

            for (int i = 0; i < joints.Count; i++)
            {
                ArticulationBody body = joints[i];
                if (body.jointType != ArticulationJointType.RevoluteJoint)
                {
                    throw new System.InvalidOperationException($"{body.name}은(는) 회전 관절이 아니다. KinematicChain은 회전 관절만 지원한다.");
                }

                Transform link = body.transform;
                Vector3 axis = (body.anchorRotation * Vector3.right).normalized;
                Vector3 pivot = body.anchorPosition;
                Quaternion spin = Quaternion.AngleAxis(currentAnglesRad[i] * Mathf.Rad2Deg, axis);

                Quaternion inversePrevious = Quaternion.Inverse(previousRotation);
                Quaternion localRotation = inversePrevious * link.rotation * Quaternion.Inverse(spin);
                Quaternion before = previousRotation * localRotation;
                Vector3 localPosition = inversePrevious * (link.position - previousPosition - before * (pivot - spin * pivot));

                specs[i] = new KinematicChain.JointSpec(
                    localPosition, localRotation, axis, pivot,
                    ArticulationJointLimits.LowerRad(body), ArticulationJointLimits.UpperRad(body));

                previousPosition = link.position;
                previousRotation = link.rotation;
            }

            Vector3 toolWorldPosition = toolLink.TransformPoint(toolPositionInToolLink);
            Quaternion toolWorldRotation = toolLink.rotation * toolRotationInToolLink;
            Quaternion inverseLast = Quaternion.Inverse(previousRotation);

            return new KinematicChain(
                specs,
                inverseLast * (toolWorldPosition - previousPosition),
                inverseLast * toolWorldRotation);
        }
    }
}
