using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ShelfGraspSequenceTests
    {
        private sealed class FakeTool : IToolPoseTarget
        {
            internal readonly List<Pose> Targets = new List<Pose>();
            internal List<string> Log;

            public bool HasToolTarget => true;
            public bool LastSolveSucceeded => true;
            public IkResult LastResult => default;

            public void SetToolTarget(Pose worldPose)
            {
                Targets.Add(worldPose);
                Log?.Add("move");
            }

            public void ClearToolTarget() { }

            public IkResult Probe(Pose worldPose) => default;

            public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg) => true;
        }

        private sealed class FakeHolder : IItemHolder
        {
            internal readonly List<string> Log;
            internal bool GraspSucceeds = true;

            internal FakeHolder(List<string> log) => Log = log;

            public string LastFailureReason { get; set; } = string.Empty;

            public void OpenGripper() => Log.Add("open");

            public IEnumerator Grasp(ShelfPickItem item, Action<bool> done)
            {
                Log.Add("grasp");
                done(GraspSucceeds);
                yield break;
            }

            public IEnumerator Release(ItemPlaceRequest request)
            {
                yield break;
            }

            public void Discard() { }
        }

        private readonly List<string> _log = new List<string>();
        private FakeTool _tool;
        private FakeHolder _holder;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _tool = new FakeTool { Log = _log };
            _holder = new FakeHolder(_log);
        }

        private sealed class FakeRefiner : IGraspRefiner
        {
            internal readonly List<string> Log;
            internal Func<GraspTargets, GraspRefinement> Decide;
            internal GraspTargets Asked;

            internal FakeRefiner(List<string> log) => Log = log;

            public GraspRefinement Refine(ItemPickRequest request, GraspTargets planned)
            {
                Log.Add("refine");
                Asked = planned;
                return Decide(planned);
            }
        }

        // 쥐는 자리에 닿은 뒤 도구 끝점의 오차를 메우는 쪽까지 흉내 낸다. 마무리를 부른 횟수와 받은 명령을 남긴다.
        private sealed class FakeSettlingRefiner : IGraspRefiner, IGraspSettler
        {
            internal readonly List<string> Log;
            internal readonly List<GraspTargets> Asked = new List<GraspTargets>();
            internal Func<int, GraspTargets, GraspRefinement> DecideSettle;

            internal FakeSettlingRefiner(List<string> log, int attempts)
            {
                Log = log;
                SettleAttempts = attempts;
            }

            public int SettleAttempts { get; }

            public float SettleWaitSeconds => 0.3f;

            public GraspRefinement Refine(ItemPickRequest request, GraspTargets planned)
            {
                Log.Add("refine");
                return GraspRefinement.Keep(planned, string.Empty);
            }

            public GraspRefinement Settle(ItemPickRequest request, GraspTargets current)
            {
                Log.Add("settle");
                Asked.Add(current);
                return DecideSettle(Asked.Count, current);
            }
        }

        private ShelfGraspSequence Create(IGraspRefiner refiner = null)
        {
            var diagnostics = new TransferDiagnostics(_tool, null, null, () => false, 0.01f);
            var tolerances = new MotionTolerances(0.03f, 10f, 0.01f, 5f, 4f, 1.5f);
            var steps = new ArmMotionSteps(null, new ToolPoseMover(_tool, 0.03f, 10f, 5f), null, diagnostics, tolerances);
            return new ShelfGraspSequence(_holder, steps, refiner);
        }

        private ItemPickRequest Request(bool commits = true)
        {
            var item = new ShelfPickItem(new Pose(Vector3.one, Quaternion.identity), Vector3.back, null, "A-1");
            return new ItemPickRequest(item, () =>
            {
                _log.Add("commit");
                return commits;
            });
        }

        private void Run(ShelfGraspSequence sequence, ItemPickRequest request, TransferResult result, Action reachedItem = null)
        {
            CoroutineRunner.Drain(
                sequence.Run(request, Quaternion.identity, Vector3.zero, Vector3.up, Vector3.right, null, reachedItem, result), null);
        }

        [Test]
        public void OpensThenApproachesThenGraspsThenRetreats_AndTakesStockJustBeforeGrasping()
        {
            var result = new TransferResult();

            Run(Create(), Request(), result);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_log, Is.EqualTo(new[] { "open", "move", "move", "commit", "grasp", "move" }));
            Assert.That(_tool.Targets[0].position, Is.EqualTo(Vector3.zero));
            Assert.That(_tool.Targets[1].position, Is.EqualTo(Vector3.up));
            Assert.That(_tool.Targets[2].position, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void TellsWhenTheItemPositionIsReached_BeforeGrasping()
        {
            bool reachedBeforeGrasp = false;

            Run(Create(), Request(), new TransferResult(), () => reachedBeforeGrasp = !_log.Contains("grasp"));

            Assert.That(reachedBeforeGrasp, Is.True);
        }

        [Test]
        public void StopsBeforeGrasping_WhenTheStockIsGone()
        {
            var result = new TransferResult();

            Run(Create(), Request(commits: false), result);

            Assert.That(result.Failure, Is.EqualTo(TransferFailure.CommitRejected));
            Assert.That(_log, Does.Not.Contain("grasp"));
        }

        [Test]
        public void FailsWithTheHoldersReason_WhenTheGripperDoesNotCatchTheItem()
        {
            _holder.GraspSucceeds = false;
            _holder.LastFailureReason = "손가락이 물건에 닿지 않고 끝까지 닫혔다";
            var result = new TransferResult();

            Run(Create(), Request(), result);

            Assert.That(result.Failure, Is.EqualTo(TransferFailure.GraspFailed));
            Assert.That(result.Detail, Does.Contain("손가락이 물건에 닿지 않고"));
            Assert.That(_tool.Targets.Count, Is.EqualTo(2), "쥐지 못했으면 후퇴하지 않는다");
        }

        [Test]
        public void LooksAtTheItemOnceTheApproachIsReached_AndBeforeMovingToTheGraspPosition()
        {
            var refiner = new FakeRefiner(_log) { Decide = planned => GraspRefinement.Keep(planned, string.Empty) };

            Run(Create(refiner), Request(), new TransferResult());

            Assert.That(_log, Is.EqualTo(new[] { "open", "move", "refine", "move", "commit", "grasp", "move" }));
            Assert.That(refiner.Asked.Near, Is.EqualTo(Vector3.zero));
            Assert.That(refiner.Asked.Grasp, Is.EqualTo(Vector3.up));
            Assert.That(refiner.Asked.Retreat, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void GraspsAndRetreatsAtTheCorrectedPositions_ButApproachesAtThePlannedOne()
        {
            var shift = new Vector3(0f, 0.5f, 0f);
            var refiner = new FakeRefiner(_log)
            {
                Decide = planned => GraspRefinement.Corrected(planned.ShiftedBy(shift, null), "고침")
            };

            Run(Create(refiner), Request(), new TransferResult());

            Assert.That(_tool.Targets[0].position, Is.EqualTo(Vector3.zero), "접근은 이미 끝났으니 계획한 자리");
            Assert.That(_tool.Targets[1].position, Is.EqualTo(Vector3.up + shift));
            Assert.That(_tool.Targets[2].position, Is.EqualTo(Vector3.right + shift));
        }

        [Test]
        public void StopsBeforeGraspingOrTakingStock_WhenTheRefinerRefusesTheGrasp()
        {
            var refiner = new FakeRefiner(_log) { Decide = planned => GraspRefinement.Rejected(planned, "카메라로 물건을 확인하지 못했다") };
            var result = new TransferResult();

            Run(Create(refiner), Request(), result);

            Assert.That(result.Failure, Is.EqualTo(TransferFailure.RecognitionFailed));
            Assert.That(result.Detail, Does.Contain("카메라로 물건을 확인하지 못했다"));
            Assert.That(_log, Does.Not.Contain("commit"));
            Assert.That(_log, Does.Not.Contain("grasp"));
            Assert.That(_tool.Targets.Count, Is.EqualTo(1), "접근까지만 갔다");
        }

        [Test]
        public void WithoutARefiner_NothingIsAskedAndThePlannedPositionsAreUsed()
        {
            Run(Create(), Request(), new TransferResult());

            Assert.That(_log, Does.Not.Contain("refine"));
            Assert.That(_tool.Targets[1].position, Is.EqualTo(Vector3.up));
        }

        [Test]
        public void SettlesAfterReachingTheGraspPosition_AndBeforeTakingStockOrGrasping()
        {
            var shift = new Vector3(0f, 0f, 0.02f);
            var refiner = new FakeSettlingRefiner(_log, attempts: 2)
            {
                DecideSettle = (count, current) => count == 1
                    ? GraspRefinement.Corrected(current.ShiftedBy(shift, null), "메움")
                    : GraspRefinement.Keep(current, string.Empty)
            };

            Run(Create(refiner), Request(), new TransferResult());

            Assert.That(_log, Is.EqualTo(new[] { "open", "move", "refine", "move", "settle", "move", "settle", "commit", "grasp", "move" }));
        }

        [Test]
        public void GraspsAndRetreatsAtTheSettledPositions()
        {
            var shift = new Vector3(0f, 0f, 0.02f);
            var refiner = new FakeSettlingRefiner(_log, attempts: 2)
            {
                DecideSettle = (count, current) => count == 1
                    ? GraspRefinement.Corrected(current.ShiftedBy(shift, null), "메움")
                    : GraspRefinement.Keep(current, string.Empty)
            };

            Run(Create(refiner), Request(), new TransferResult());

            Assert.That(_tool.Targets[1].position, Is.EqualTo(Vector3.up), "처음 쥐는 자리");
            Assert.That(_tool.Targets[2].position, Is.EqualTo(Vector3.up + shift), "메운 쥐는 자리로 다시 간다");
            Assert.That(_tool.Targets[3].position, Is.EqualTo(Vector3.right + shift), "후퇴도 메운 만큼 옮긴다");
            Assert.That(refiner.Asked[1].Grasp, Is.EqualTo(Vector3.up + shift), "두 번째 마무리는 메운 명령에서 다시 본다");
        }

        [Test]
        public void StopsSettling_AsSoonAsNothingNeedsFixing()
        {
            var refiner = new FakeSettlingRefiner(_log, attempts: 5)
            {
                DecideSettle = (count, current) => GraspRefinement.Keep(current, string.Empty)
            };

            Run(Create(refiner), Request(), new TransferResult());

            Assert.That(refiner.Asked.Count, Is.EqualTo(1), "고칠 게 없으면 더 보지 않는다");
            Assert.That(_tool.Targets.Count, Is.EqualTo(3), "접근, 쥐는 자리, 후퇴만 움직인다");
        }

        [Test]
        public void SettlesNoMoreThanTheAllowedAttempts()
        {
            var refiner = new FakeSettlingRefiner(_log, attempts: 2)
            {
                DecideSettle = (count, current) => GraspRefinement.Corrected(current.ShiftedBy(Vector3.forward * 0.01f, null), "메움")
            };

            Run(Create(refiner), Request(), new TransferResult());

            Assert.That(refiner.Asked.Count, Is.EqualTo(2));
        }

        [Test]
        public void DoesNotSettle_WhenTheAttemptsAreZero()
        {
            var refiner = new FakeSettlingRefiner(_log, attempts: 0)
            {
                DecideSettle = (count, current) => throw new InvalidOperationException("부르면 안 된다")
            };

            Run(Create(refiner), Request(), new TransferResult());

            Assert.That(_log, Does.Not.Contain("settle"));
        }

        [Test]
        public void FailsWithAGenericReason_WhenTheHolderGivesNone()
        {
            _holder.GraspSucceeds = false;
            var result = new TransferResult();

            Run(Create(), Request(), result);

            Assert.That(result.Failure, Is.EqualTo(TransferFailure.GraspFailed));
            Assert.That(result.Detail, Does.Contain("쥐지 못했다"));
        }
    }
}
