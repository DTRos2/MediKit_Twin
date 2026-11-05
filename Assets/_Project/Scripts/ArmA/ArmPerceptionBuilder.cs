using System;
using System.Collections.Generic;
using System.Text;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 준비된 로봇에 카메라, 물건 인식기, 장애물 회피를 만들어 ArmPerception에 채운다. 씬의 충돌체와 링크를 읽는 일을 한곳에 모았다.
    ///  - 카메라: 도구 링크에 달고, 시뮬레이션 인식기를 만들어 보고 결과를 카메라 화면에 남기게 감싼다.
    ///  - 장애물 회피: 링크 충돌체로 충돌 상자를 만들고, 지금 서 있는 자리용 판정(지금 자세에서 출발)과 가늠용 판정(접힌 자세에서 출발)을 만든다.
    /// 장애물로 보지 않는 충돌체: 로봇 자신, 쥔 물건, 팔 베이스 가까이에 붙은 것(굴러가는 베이스의 몸체 등. 팔이 항상 닿아 있다).
    /// </summary>
    public static class ArmPerceptionBuilder
    {
        // 팔 베이스 링크 가까이(이 반경 안)에 있는 충돌체는 팔이 올라탄 베이스로 보고 장애물에서 뺀다.
        private const float MountSearchRadiusM = 0.2f;

        /// <param name="solver">도구 자세를 관절 각도로 푸는 쪽. 없으면 장애물 회피와 관절 길 고치기를 쓰지 못한다.</param>
        /// <param name="stowSource">팔을 접는 자세를 알려 주는 쪽. 없으면 가늠할 때도 지금 자세에서 출발한다.</param>
        /// <param name="log">진행을 알리는 쪽.</param>
        public static void Attach(
            ArmPerception perception,
            IArmModel model,
            IGripperHost host,
            ISeededToolSolver solver,
            IArmStowPoseSource stowSource,
            ToolCameraSettings cameraSettings,
            RecognitionSettings recognitionSettings,
            CollisionAvoidanceSettings collisionSettings,
            Action<string> log)
        {
            Transform robotRoot = host.RobotRoot;
            HashSet<Collider> mounts = FindMounts(model.BaseLink, robotRoot);
            Predicate<Collider> isIgnored = collider => IsRobotPart(collider, robotRoot) || mounts.Contains(collider);

            ArmClearanceProbe current = null;
            ArmClearanceProbe stowed = null;
            Func<Pose, string> describeBase = null;
            if (collisionSettings.Enabled)
            {
                CreateClearanceProbes(model, robotRoot, solver, stowSource, collisionSettings, isIgnored, log, out current, out stowed, out describeBase);
            }

            IGraspRefiner refiner = null;
            if (cameraSettings.Enabled)
            {
                refiner = CreateRefiner(host, solver, cameraSettings, recognitionSettings, current, isIgnored, log);
            }

            perception.Attach(
                refiner,
                current != null ? (Func<Pose, bool>)(pose => current.IsClear(pose)) : null,
                stowed != null ? (Func<Pose, Pose, bool>)((target, armBase) => stowed.IsClear(target, armBase)) : null,
                current != null ? (Func<GraspTargets, bool>)(targets => current.IsClear(targets)) : null,
                current != null ? (Func<GraspTargets, string>)(targets => current.DescribeCollision(targets)) : null,
                describeBase);

            log($"카메라 {(cameraSettings.Enabled ? "켬" : "끔")}, 물건 인식 {(refiner != null ? "켬" : "끔")}, 장애물 회피 {(current != null ? "켬" : "끔")}.");
        }

        // 팔 베이스 링크 가까이의 충돌체(로봇 자신 제외)를 모은다. 팔이 올라탄 베이스로 보고 장애물에서 뺀다.
        private static HashSet<Collider> FindMounts(Transform baseLink, Transform robotRoot)
        {
            var mounts = new HashSet<Collider>();
            Collider[] nearby = Physics.OverlapSphere(baseLink.position, MountSearchRadiusM, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in nearby)
            {
                if (!IsRobotPart(collider, robotRoot))
                {
                    mounts.Add(collider);
                }
            }

            return mounts;
        }

        private static bool IsRobotPart(Collider collider, Transform robotRoot)
        {
            return collider.attachedArticulationBody != null
                || collider.transform.IsChildOf(robotRoot)
                || collider.GetComponentInParent<Graspable>() != null;
        }

        private static void CreateClearanceProbes(
            IArmModel model,
            Transform robotRoot,
            ISeededToolSolver solver,
            IArmStowPoseSource stowSource,
            CollisionAvoidanceSettings settings,
            Predicate<Collider> isIgnored,
            Action<string> log,
            out ArmClearanceProbe current,
            out ArmClearanceProbe stowed,
            out Func<Pose, string> describeBase)
        {
            current = null;
            stowed = null;
            describeBase = null;

            var links = model as IArmLinkSource;
            if (links == null || solver == null || model.Chain == null)
            {
                log("팔 링크 정보나 관절 풀이가 없어 장애물 회피를 쓰지 않는다.");
                return;
            }

            IReadOnlyList<ArticulationBody> bodies = links.JointBodies;
            List<LinkVolume> volumes = LinkVolumeBuilder.Build(robotRoot, bodies);
            if (volumes.Count == 0)
            {
                log("팔 링크에 붙은 충돌체가 없어 충돌 상자를 만들지 못했다. 장애물 회피를 쓰지 않는다.");
                return;
            }

            var linkNames = new string[bodies.Count];
            for (int i = 0; i < linkNames.Length; i++)
            {
                linkNames[i] = bodies[i].name;
            }

            var checker = new ArmCollisionChecker(
                model.Chain,
                volumes,
                new PhysicsObstacleQuery(settings.ObstacleLayers, isIgnored),
                settings.ClearanceM,
                settings.SkipBaseLinks,
                linkNames);

            int jointCount = model.JointNames.Count;
            var currentJoints = new float[jointCount];
            var seedBuffer = new float[jointCount];
            Func<Pose> armBaseNow = () => new Pose(model.BaseLink.position, model.BaseLink.rotation);

            current = new ArmClearanceProbe(solver, () => ReadCurrent(model, currentJoints), checker, armBaseNow, jointCount);
            stowed = new ArmClearanceProbe(solver, () => StowOrCurrent(model, stowSource, seedBuffer), checker, armBaseNow, jointCount);

            log(DescribeVolumes(volumes, linkNames, settings));
            describeBase = CreateBaseCheck(model, bodies, settings, isIgnored, log);
        }

        // 베이스 링크가 서 있는 자리에서 장애물과 겹치는지 묻는 함수. 팔 링크의 상자는 베이스 링크를 빼고 만들어서 따로 만든다.
        private static Func<Pose, string> CreateBaseCheck(
            IArmModel model, IReadOnlyList<ArticulationBody> bodies, CollisionAvoidanceSettings settings, Predicate<Collider> isIgnored, Action<string> log)
        {
            if (!settings.AvoidBaseOverlap)
            {
                return null;
            }

            List<LinkVolume> baseVolumes = BaseVolumeBuilder.Build(model.BaseLink, bodies);
            if (baseVolumes.Count == 0)
            {
                log("팔 베이스 링크에 붙은 충돌체가 없어 베이스가 장애물에 파고드는지는 따지지 않는다.");
                return null;
            }

            var baseChecker = new BaseClearanceChecker(baseVolumes, new PhysicsObstacleQuery(settings.ObstacleLayers, isIgnored), settings.BaseMarginM);
            log($"팔 베이스 충돌 상자 {baseVolumes.Count}개를 만들었다. 여유 {settings.BaseMarginM * 100f:0.0}cm.");
            return pose => baseChecker.TryFindCollision(pose, out string description) ? description : string.Empty;
        }

        private static float[] ReadCurrent(IArmModel model, float[] buffer)
        {
            model.GetJointPositions(buffer);
            return buffer;
        }

        // 서 보기 전에 가늠할 때는 접힌 자세에서 출발한다(실제로 서 있는 자리에서 방향을 찾을 때도 접힌 자세에서 출발하기 때문이다).
        private static float[] StowOrCurrent(IArmModel model, IArmStowPoseSource stowSource, float[] buffer)
        {
            if (stowSource != null && stowSource.TryGetStowPose(out float[] stow) && stow != null && stow.Length == buffer.Length)
            {
                return stow;
            }

            return ReadCurrent(model, buffer);
        }

        private static string DescribeVolumes(IReadOnlyList<LinkVolume> volumes, IReadOnlyList<string> linkNames, CollisionAvoidanceSettings settings)
        {
            var perLink = new int[linkNames.Count];
            foreach (LinkVolume volume in volumes)
            {
                if (volume.LinkIndex < perLink.Length)
                {
                    perLink[volume.LinkIndex]++;
                }
            }

            var text = new StringBuilder();
            for (int i = 0; i < perLink.Length; i++)
            {
                text.Append(i == 0 ? string.Empty : ", ").Append(linkNames[i]).Append(' ').Append(perLink[i]);
            }

            return $"충돌 상자 {volumes.Count}개를 만들었다(링크별: {text}). 여유 {settings.ClearanceM * 100f:0.0}cm, 베이스 쪽 {settings.SkipBaseLinks}개 링크는 따지지 않는다.";
        }

        private static IGraspRefiner CreateRefiner(
            IGripperHost host,
            ISeededToolSolver solver,
            ToolCameraSettings cameraSettings,
            RecognitionSettings recognitionSettings,
            ArmClearanceProbe current,
            Predicate<Collider> isIgnored,
            Action<string> log)
        {
            if (host.ToolBody == null)
            {
                log("도구 링크를 찾지 못해 카메라를 달지 못했다.");
                return null;
            }

            ToolCameraFeed feed = ToolCameraMounter.Mount(host.ToolBody, host.ToolPose, cameraSettings, host.RobotRoot, log);
            if (!recognitionSettings.Enabled)
            {
                return null;
            }

            var lineOfSight = new PhysicsLineOfSight(Physics.DefaultRaycastLayers, isIgnored);
            var simulated = new SimulatedItemPoseEstimator(
                feed,
                ShelfItemLivePose.Of,
                lineOfSight.IsBlocked,
                new ObservationNoise(recognitionSettings.NoiseSigmaM, recognitionSettings.NoiseSeed));
            var estimator = new ObservationRecorder(simulated, feed.Record);

            return new ObservationGraspRefiner(
                estimator,
                recognitionSettings,
                solver != null ? new SeededRouteBuilder(solver) : null,
                current != null ? (Func<GraspTargets, bool>)(targets => current.IsClear(targets)) : null,
                log,
                toolPose: () => host.ToolPose,
                onItemGrasped: feed.MarkTargetTaken);
        }
    }
}
