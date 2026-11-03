using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    // 카메라 인식과 충돌 시험이 함께 쓰는 가짜들. 씬 없이 시험할 수 있게 카메라, 인식기, 풀이, 장애물을 흉내 낸다.

    internal sealed class FakeCameraView : IToolCameraView
    {
        internal bool Ready = true;
        internal Pose At = new Pose(Vector3.zero, Quaternion.identity);
        internal PinholeCamera View = new PinholeCamera(90f, 1f, 0.05f, 3f);

        public bool IsReady => Ready;
        public Texture Image => null;
        public Pose CameraPose => At;
        public PinholeCamera Model => View;
        public ItemObservation LastObservation { get; private set; } = ItemObservation.None;
        public int ObservationCount { get; private set; }

        internal void Record(ItemObservation observation)
        {
            LastObservation = observation;
            ObservationCount++;
        }
    }

    internal sealed class FakeEstimator : IItemPoseEstimator
    {
        internal ItemObservation Next = ItemObservation.None;
        internal int Calls;

        public ItemObservation Observe(ShelfPickItem item)
        {
            Calls++;
            return Next;
        }
    }

    // 도구 자세의 x 좌표를 첫 관절 각도로 삼아 푸는 가짜 풀이. 풀이 요청 횟수와 마지막 시드를 남긴다.
    internal sealed class FakeSeededSolver : ISeededToolSolver
    {
        // 도구 방향이 기본(회전 없음)에서 이만큼(도)보다 더 돌아가 있으면 풀지 못한다고 답한다. 숙인 자세만 못 푸는 경우를 흉내 낸다.
        private const float UntiltedToleranceDeg = 0.5f;

        internal bool Solves = true;
        internal bool RefusesTilted;
        internal int Calls;
        internal IReadOnlyList<float> LastSeed;

        public bool TrySolveFrom(Pose worldPose, IReadOnlyList<float> seedRad, float[] solutionRad)
        {
            Calls++;
            LastSeed = seedRad;
            if (!Solves)
            {
                return false;
            }

            if (RefusesTilted && Quaternion.Angle(worldPose.rotation, Quaternion.identity) > UntiltedToleranceDeg)
            {
                return false;
            }

            for (int i = 0; i < solutionRad.Length; i++)
            {
                solutionRad[i] = i == 0 ? worldPose.position.x : 0f;
            }

            return true;
        }
    }

    // 중심이 어떤 구 안에 들어가면 겹친다고 답하는 가짜 장애물. 질의에 쓰인 상자 크기를 남긴다.
    internal sealed class FakeObstacles : IObstacleQuery
    {
        internal readonly List<(Vector3 Center, float Radius, string Name)> Spheres = new List<(Vector3, float, string)>();
        internal Vector3 LastHalfExtents;
        internal int Queries;

        public bool Overlaps(Vector3 center, Vector3 halfExtents, Quaternion rotation, out string obstacleName)
        {
            Queries++;
            LastHalfExtents = halfExtents;
            foreach ((Vector3 sphereCenter, float radius, string name) in Spheres)
            {
                if (Vector3.Distance(center, sphereCenter) < radius)
                {
                    obstacleName = name;
                    return true;
                }
            }

            obstacleName = string.Empty;
            return false;
        }
    }

    internal static class PerceptionTestData
    {
        // 두 관절이 Z축으로 도는 평면 팔: 링크0은 원점, 링크1은 링크0에서 +X로 1m. 도구 끝점은 링크1에서 +X로 0.5m.
        internal static KinematicChain TwoLinkArm()
        {
            var joints = new[]
            {
                new KinematicChain.JointSpec(Vector3.zero, Quaternion.identity, Vector3.forward, Vector3.zero, -Mathf.PI, Mathf.PI),
                new KinematicChain.JointSpec(Vector3.right, Quaternion.identity, Vector3.forward, Vector3.zero, -Mathf.PI, Mathf.PI)
            };
            return new KinematicChain(joints, new Vector3(0.5f, 0f, 0f), Quaternion.identity);
        }

        // 링크1에 붙은 상자: 링크 로컬에서 +X로 0.5m 떨어진 곳이 중심, 길이 1m의 막대.
        internal static LinkVolume Link1Bar() => new LinkVolume(1, new Vector3(0.5f, 0f, 0f), Quaternion.identity, new Vector3(0.5f, 0.1f, 0.1f));

        internal static LinkVolume Link0Block() => new LinkVolume(0, Vector3.zero, Quaternion.identity, new Vector3(0.1f, 0.1f, 0.1f));

        internal static ShelfPickItem Item(Vector3 position) =>
            new ShelfPickItem(new Pose(position, Quaternion.identity), Vector3.back, null, "A-1");

        // 값을 주지 않은 항목은 설정 클래스가 정한 기본값을 따른다(기울임 0, 마무리 2회 등).
        internal static RecognitionSettings Recognition(
            bool require, float min = 0.004f, float max = 0.03f, float fingerDrop = 0f,
            float tiltDeg = 0f, float settleTolerance = 0.005f, float maxSettle = 0.04f, int settleAttempts = 2)
        {
            System.Globalization.CultureInfo c = System.Globalization.CultureInfo.InvariantCulture;
            string json = "{\"_enabled\":true,\"_requireRecognition\":" + (require ? "true" : "false")
                + ",\"_noiseSigmaM\":0,\"_noiseSeed\":1,\"_minCorrectionM\":" + min.ToString(c)
                + ",\"_maxShiftM\":" + max.ToString(c)
                + ",\"_fingerDropBelowToolM\":" + fingerDrop.ToString(c)
                + ",\"_graspTiltDeg\":" + tiltDeg.ToString(c)
                + ",\"_settleAttempts\":" + settleAttempts.ToString(c)
                + ",\"_settleToleranceM\":" + settleTolerance.ToString(c)
                + ",\"_maxSettleM\":" + maxSettle.ToString(c) + "}";
            return JsonUtility.FromJson<RecognitionSettings>(json);
        }
    }
}
