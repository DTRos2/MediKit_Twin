#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MediKitTwin.ArmA;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// (조사용) 알려 준 선반 칸의 쥐는 자세에서 손끝을 숙인 도구 방향(GraspTargets.TiltedBy)으로 쥐는 자리의 관절 각도가 풀리는지 잰다.
    /// 실제 집기는 숙인 자세를 못 풀면 숙임을 빼고 쥐는데(ObservationGraspRefiner.TryShift), C행만 그렇게 되어 얇은 물건에 손가락이 못 닿는다.
    /// 알려 준 서는 자리에서 물건 쪽으로 advance만큼 더 다가선 자리에 서고, 알려 준 관절에서 출발해 숙임 각도와 쥐는 높이마다 푼다.
    /// 팔은 움직이지 않고 풀이만 묻는다(TrySolveFrom). 환경 변수: MEDIKIT_PROBE_SLOT(칸, 이 값이 있어야 돈다), MEDIKIT_PROBE_ADVANCES(쉼표로 나눈 m, 기본 0).
    /// </summary>
    public sealed class TiltReachProbe
    {
        private const string RobotName = "fr5_robot";
        private const string SlotVariable = "MEDIKIT_PROBE_SLOT";
        private const string AdvanceVariable = "MEDIKIT_PROBE_ADVANCES";
        private const string DefaultSlot = "C-1";
        private const float ApproachDistanceM = 0.15f;
        private static readonly float[] TiltsDeg = { 0f, 5f, 10f, 15f, 20f, 25f, 30f };
        private static readonly float[] HeightOffsetsM = { 0f, -0.02f, -0.03f, -0.04f, -0.05f, -0.06f, -0.08f };

        [UnityTest, Timeout(600000)]
        public IEnumerator TaughtGraspPoses_TiltedByEachAngle_AreSolvedFromTheTaughtJoints()
        {
            RequireEnvironment(SlotVariable);
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();

            string slot = System.Environment.GetEnvironmentVariable(SlotVariable);
            if (string.IsNullOrEmpty(slot))
            {
                slot = DefaultSlot;
            }

            float[] advances = ParseAdvances(System.Environment.GetEnvironmentVariable(AdvanceVariable));

            GameObject robot = GameObject.Find(RobotName);
            Assert.That(robot, Is.Not.Null, $"씬에서 팔({RobotName})을 찾을 수 없다");
            var ik = robot.GetComponentInChildren<IkArmTarget>(true);
            Assert.That(ik, Is.Not.Null, "팔 A의 IkArmTarget을 찾을 수 없다");

            string path = Path.Combine(Application.dataPath, "_Project/Config/TaughtPickPoses.json");
            PickPoseBook book = PickPoseBook.FromJson(File.ReadAllText(path));
            Assert.That(book.TryGet(slot, out TaughtPickPose taught), Is.True, $"칸 {slot}의 알려 준 자세가 없다");

            IArmBasePositioner positioner = null;
            foreach (MonoBehaviour behaviour in robot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IArmBasePositioner found)
                {
                    positioner = found;
                    break;
                }
            }

            Assert.That(positioner, Is.Not.Null, "팔 A의 베이스 이동(IArmBasePositioner)을 찾을 수 없다");

            Pose tool = taught.ToolPose;
            Vector3 insert = tool.rotation * Vector3.forward;
            Vector3 flatInsert = new Vector3(0f, 0f, Mathf.Sign(insert.z));
            float[] seed = taught.JointsRad();
            var solution = new float[seed.Length];

            foreach (float advance in advances)
            {
                Vector3 stand = taught.ArmBasePosition + flatInsert * advance;
                bool arrived = false;
                yield return positioner.MoveArmBaseTo(stand, taught.RootYawDeg, ok => arrived = ok);

                var text = new StringBuilder();
                text.AppendLine(
                    $"[TiltProbe] 칸 {slot} 알려 준 도구 높이 {tool.position.y:0.000}m, 알려 준 서는 자리보다 물건 쪽으로 {advance * 100f:0}cm 더 다가섬(도착 {arrived}). " +
                    "칸마다 G(쥠)·N(접근)이 풀리면 o, 안 풀리면 x. 행은 숙임 각도, 열은 알려 준 높이에서 내린 양(cm).");
                text.Append("숙임\\내림 ");
                foreach (float offset in HeightOffsetsM)
                {
                    text.Append($"| {offset * 100f,4:0} ");
                }

                text.AppendLine();
                foreach (float tilt in TiltsDeg)
                {
                    text.Append($"{tilt,4:0}°     ");
                    foreach (float offset in HeightOffsetsM)
                    {
                        Quaternion rotation = tool.rotation * Quaternion.AngleAxis(tilt, Vector3.up);
                        Vector3 grasp = tool.position + Vector3.up * offset;
                        bool g = ik.TrySolveFrom(new Pose(grasp, rotation), seed, solution);
                        float[] graspSolution = g ? (float[])solution.Clone() : seed;
                        bool n = ik.TrySolveFrom(new Pose(grasp - insert * ApproachDistanceM, rotation), graspSolution, solution);
                        text.Append($"|  {(g ? 'o' : 'x')}{(n ? 'o' : 'x')}  ");
                    }

                    text.AppendLine();
                }

                Debug.Log(text.ToString());
            }

            yield return CellTestScene.StopToolCameraFeeds();
        }

        /// <summary>
        /// 알려 준 칸들의 서는 자리를 물건 쪽으로 advance만큼 당긴 새 자세 모음을 만들어 MEDIKIT_PROBE_OUT 파일에 쓴다. 저장소의 파일은 건드리지 않는다.
        /// 도구 끝점 자세는 그대로 두고, 당긴 서는 자리에서 알려 준 관절을 출발점으로 도구 방향 그대로의 관절 각도를 풀어 새로 알려 준 관절 값으로 쓴다.
        /// 환경 변수: MEDIKIT_PROBE_SLOTS(쉼표로 나눈 칸), MEDIKIT_PROBE_ADVANCES(m 하나), MEDIKIT_PROBE_OUT(쓸 파일). 셋 다 있어야 돈다.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator GenerateTaughtPoses_WithTheStandsAdvancedTowardTheShelf()
        {
            RequireEnvironment("MEDIKIT_PROBE_SLOTS", AdvanceVariable, "MEDIKIT_PROBE_OUT");
            LogAssert.ignoreFailingMessages = true;
            yield return CellTestScene.Load();

            string[] slots = System.Environment.GetEnvironmentVariable("MEDIKIT_PROBE_SLOTS").Split(',').Select(part => part.Trim()).ToArray();
            float advance = float.Parse(System.Environment.GetEnvironmentVariable(AdvanceVariable), CultureInfo.InvariantCulture);
            string outPath = System.Environment.GetEnvironmentVariable("MEDIKIT_PROBE_OUT");

            GameObject robot = GameObject.Find(RobotName);
            var ik = robot.GetComponentInChildren<IkArmTarget>(true);
            PickPoseBook book = PickPoseBook.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Config/TaughtPickPoses.json")));

            IArmBasePositioner positioner = null;
            foreach (MonoBehaviour behaviour in robot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IArmBasePositioner found)
                {
                    positioner = found;
                    break;
                }
            }

            foreach (string slot in slots)
            {
                Assert.That(book.TryGet(slot, out TaughtPickPose taught), Is.True, $"칸 {slot}의 알려 준 자세가 없다");
                Pose tool = taught.ToolPose;
                Vector3 insert = tool.rotation * Vector3.forward;
                Vector3 flatInsert = new Vector3(0f, 0f, Mathf.Sign(insert.z));
                Vector3 armBase = taught.ArmBasePosition + flatInsert * advance;
                Vector3 root = taught.RootPosition + flatInsert * advance;

                bool arrived = false;
                yield return positioner.MoveArmBaseTo(armBase, taught.RootYawDeg, ok => arrived = ok);

                var solution = new float[taught.JointsRad().Length];
                bool solved = ik.TrySolveFrom(tool, taught.JointsRad(), solution);
                Assert.That(solved, Is.True, $"칸 {slot}: 당긴 서는 자리에서 알려 준 도구 자세의 관절 각도를 풀지 못했다(도착 {arrived})");

                var matrix = new StringBuilder($"[TiltProbe] 칸 {slot}: 새 관절(도) ");
                matrix.Append(string.Join(",", solution.Select(rad => (rad * Mathf.Rad2Deg).ToString("0.0", CultureInfo.InvariantCulture))));
                matrix.Append(" — 새 관절에서 출발해 풀린 숙임(G/N 모두 o): ");
                foreach (float tilt in new[] { 15f, 20f, 25f })
                {
                    foreach (float offset in new[] { -0.03f, -0.04f, -0.06f })
                    {
                        Quaternion rotation = tool.rotation * Quaternion.AngleAxis(tilt, Vector3.up);
                        Vector3 grasp = tool.position + Vector3.up * offset;
                        var scratch = new float[solution.Length];
                        bool g = ik.TrySolveFrom(new Pose(grasp, rotation), solution, scratch);
                        bool n = g && ik.TrySolveFrom(new Pose(grasp - insert * ApproachDistanceM, rotation), scratch, new float[solution.Length]);
                        matrix.Append($"{tilt:0}°/{offset * 100f:0}cm={(g && n ? 'o' : 'x')} ");
                    }
                }

                Debug.Log(matrix.ToString());
                book.Set(TaughtPickPose.Capture(slot, root, taught.RootYawDeg, armBase, tool, solution));
            }

            File.WriteAllText(outPath, book.ToJson());
            Debug.Log($"[TiltProbe] 새 알려 준 자세 {slots.Length}칸을 {outPath}에 썼다");
            yield return CellTestScene.StopToolCameraFeeds();
        }

        /// <summary>조사용 시험이라 환경 변수를 주지 않으면 건너뛴다. 전체 PlayMode 시험에 섞여 돌거나 실패하지 않게 한다.</summary>
        private static void RequireEnvironment(params string[] names)
        {
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(name)))
                {
                    Assert.Ignore($"조사용 시험이다. 환경 변수 {name}을 주어야 돈다.");
                }
            }
        }

        private static float[] ParseAdvances(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new[] { 0f };
            }

            return text.Split(',').Select(part => float.Parse(part.Trim(), CultureInfo.InvariantCulture)).ToArray();
        }
    }
}
#endif
