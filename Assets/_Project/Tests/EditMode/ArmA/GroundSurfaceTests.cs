using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GroundSurfaceTests
    {
        private const float Underside = 0f;
        private const float Tolerance = 0.03f;

        // 윗면이 topY인 넓은 충돌체.
        private static Bounds WithTop(float topY) => new Bounds(new Vector3(0f, topY - 0.5f, 0f), new Vector3(20f, 1f, 20f));

        [Test]
        public void ASurfaceBelowTheUnderside_IsTheGroundTheBodyRollsOn()
        {
            Assert.That(GroundSurface.IsGround(WithTop(-0.2f), Underside, Tolerance), Is.True);
        }

        [Test]
        public void AFloorFlushWithTheUnderside_IsGround()
        {
            Assert.That(GroundSurface.IsGround(WithTop(0f), Underside, Tolerance), Is.True);
        }

        [Test]
        public void AFloorSlightlyHigherThanTheUndersideWithinTheTolerance_IsStillGround()
        {
            Assert.That(GroundSurface.IsGround(WithTop(0.02f), Underside, Tolerance), Is.True);
        }

        [Test]
        public void ASurfaceRisingAboveTheTolerance_IsAnObstacleNotGround()
        {
            Assert.That(GroundSurface.IsGround(WithTop(0.05f), Underside, Tolerance), Is.False);
        }

        [Test]
        public void ARailStandingAboveTheUnderside_IsNotGround_EvenThoughItReachesDown()
        {
            // 레일은 바닥 아래(-0.025)에서 시작하지만 윗면이 0.975라 차체가 부딪힌다.
            var rail = new Bounds(new Vector3(0f, 0.475f, 0.48f), new Vector3(4f, 1f, 0.04f));

            Assert.That(GroundSurface.IsGround(rail, Underside, Tolerance), Is.False);
        }

        [Test]
        public void WithNoBodyAtAll_NothingIsGround()
        {
            Assert.That(GroundSurface.IsGround(WithTop(-100f), float.NegativeInfinity, Tolerance), Is.False);
        }

        [Test]
        public void TheColliderVersionReadsTheColliderBounds()
        {
            var go = new GameObject("floor");
            try
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(20f, 1f, 20f);
                go.transform.position = new Vector3(0f, -0.5f, 0f);
                Physics.SyncTransforms();

                Assert.That(GroundSurface.Of(Underside, Tolerance)(box), Is.True);
                Assert.That(GroundSurface.Of(-1f, Tolerance)(box), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
