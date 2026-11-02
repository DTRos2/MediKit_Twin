using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandPolicyTests
    {
        private ArmBaseApproachConfigSO _config;
        private ArmBaseStandPlanner _planner;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ArmBaseApproachConfigSO>();
            _planner = new ArmBaseStandPlanner(_config.PickStandoffM, _config.PlaceStandoffM, _config.ApproachExtraM, _config.BoxStandDirection);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void FixedArm_TriesOnceAndHasNoStandPlaces()
        {
            var policy = new StandPolicy(null, null);

            Assert.That(policy.CanMoveBase, Is.False);
            Assert.That(policy.PickStandoffOffsets, Is.EqualTo(new[] { 0f }));
            Assert.That(policy.PlaceStandoffOffsets, Is.EqualTo(new[] { 0f }));
            Assert.That(policy.LateralOffsets, Is.EqualTo(new[] { 0f }));
            Assert.That(policy.ShelfStand(Vector3.zero, Vector3.back, 0f, 0f), Is.Null);
            Assert.That(policy.ShelfWaypoint(Vector3.zero, Vector3.back, 0f, 0f), Is.Null);
            Assert.That(policy.BoxStand(Vector3.zero, 0f, 0f), Is.Null);
            Assert.That(policy.BoxFacingYawDeg(), Is.Null);
            Assert.That(policy.CreateCost(), Is.Null);
            Assert.That(policy.SearchSettings.PlanInAdvance, Is.False);
            Assert.That(policy.SearchSettings.MaxStandAttempts, Is.EqualTo(1));
        }

        [Test]
        public void MovingBase_ReadsTheOffsetsAndSearchSettingsFromTheConfig()
        {
            var policy = new StandPolicy(_planner, _config);

            Assert.That(policy.CanMoveBase, Is.True);
            Assert.That(policy.PickStandoffOffsets, Is.EqualTo(_config.PickStandoffOffsetsM));
            Assert.That(policy.PlaceStandoffOffsets, Is.EqualTo(_config.PlaceStandoffOffsetsM));
            Assert.That(policy.LateralOffsets, Is.EqualTo(_config.StandLateralOffsetsM));
            Assert.That(policy.WaypointExtraM, Is.EqualTo(_config.ApproachExtraM));
            Assert.That(policy.SearchSettings.PlanInAdvance, Is.EqualTo(_config.PlanStandInAdvance));
            Assert.That(policy.SearchSettings.MaxStandAttempts, Is.EqualTo(_config.MaxStandAttempts));
            Assert.That(policy.CreateCost(), Is.Not.Null);
        }

        [Test]
        public void MovingBase_AsksThePlannerForTheStandPlaces()
        {
            var policy = new StandPolicy(_planner, _config);
            Vector3 item = new Vector3(1f, 1.2f, 2f);

            Assert.That(policy.ShelfStand(item, Vector3.back, 0.1f, -0.1f), Is.EqualTo(_planner.ForShelfItem(item, Vector3.back, 0.1f, -0.1f)));
            Assert.That(policy.ShelfWaypoint(item, Vector3.back, 0.1f, -0.1f), Is.EqualTo(_planner.ForShelfItemApproach(item, Vector3.back, 0.1f, -0.1f)));
            Assert.That(policy.BoxStand(item, 0.2f, 0f), Is.EqualTo(_planner.ForBoxSlot(item, 0.2f, 0f)));
        }

        [Test]
        public void MovingBase_FacesTheZone_WhenTheConfigSaysSo()
        {
            var policy = new StandPolicy(_planner, _config);

            Assert.That(_config.FaceTargetAtStand, Is.True);
            Assert.That(policy.ShelfFacingYawDeg(Vector3.back), Is.EqualTo(ArmBaseStandPlanner.ShelfItemFacingYawDeg(Vector3.back, _config.FacingOffsetDeg)));
            Assert.That(policy.BoxFacingYawDeg(), Is.EqualTo(_planner.BoxSlotFacingYawDeg(_config.FacingOffsetDeg)));
        }
    }
}
