using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class BeltUvFlowTests
    {
        private static readonly Vector3 Up = Vector3.up;

        // 윗면 네 점(가로 length m, 세로 1m). u는 x를 따라 uAtStart에서 uAtEnd로 변한다.
        private static void TopQuad(float length, float uAtStart, float uAtEnd, out Vector3[] positions, out Vector3[] normals, out Vector2[] uvs)
        {
            positions = new[] { new Vector3(0f, 0f, 0f), new Vector3(length, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(length, 0f, 1f) };
            normals = new[] { Up, Up, Up, Up };
            uvs = new[] { new Vector2(uAtStart, 0f), new Vector2(uAtEnd, 0f), new Vector2(uAtStart, 1f), new Vector2(uAtEnd, 1f) };
        }

        [Test]
        public void WhenUGrowsAlongTheFlow_TheSlopeIsPositive()
        {
            TopQuad(1f, 0f, 1f, out Vector3[] p, out Vector3[] n, out Vector2[] uv);

            Vector2 perMeter = BeltUvFlow.PerMeter(p, n, uv, Vector3.right, 0.9f);

            Assert.That(perMeter.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(perMeter.y, Is.EqualTo(0f).Within(1e-5f), "v는 흐름과 수직이라 변하지 않는다");
        }

        [Test]
        public void WhenUShrinksAlongTheFlow_TheSlopeIsNegative()
        {
            TopQuad(1f, 1f, 0f, out Vector3[] p, out Vector3[] n, out Vector2[] uv);

            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.right, 0.9f).x, Is.EqualTo(-1f).Within(1e-5f));
        }

        [Test]
        public void TheSlopeIsPerMeter_SoALongerBeltGivesASmallerSlope()
        {
            TopQuad(2f, 0f, 1f, out Vector3[] p, out Vector3[] n, out Vector2[] uv);

            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.right, 0.9f).x, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void TheFlowDirectionIsNormalised_SoItsLengthDoesNotMatter()
        {
            TopQuad(1f, 0f, 1f, out Vector3[] p, out Vector3[] n, out Vector2[] uv);

            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.right * 7f, 0.9f).x, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void WhenTheFlowIsAlongZ_TheSlopeAlongXIsNotUsed()
        {
            TopQuad(1f, 0f, 1f, out Vector3[] p, out Vector3[] n, out Vector2[] uv);

            Vector2 perMeter = BeltUvFlow.PerMeter(p, n, uv, Vector3.forward, 0.9f);

            Assert.That(perMeter.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(perMeter.y, Is.EqualTo(1f).Within(1e-5f), "v가 z를 따라 0에서 1로 변한다");
        }

        [Test]
        public void FacesThatDoNotLookUp_AreIgnored()
        {
            // 윗면은 u가 0→1, 옆면(법선이 +z)은 같은 x 범위인데 u가 정반대로 5→0이다. 옆면을 섞으면 기울기가 달라진다.
            Vector3[] p = { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 2f), new Vector3(1f, 0f, 2f) };
            Vector3[] n = { Up, Up, Up, Up, Vector3.forward, Vector3.forward };
            Vector2[] uv = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(5f, 0f), new Vector2(0f, 0f) };

            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.right, 0.9f).x, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void WithFewerThanTwoTopVertices_NothingCanBeMeasured()
        {
            Vector3[] p = { Vector3.zero, Vector3.right };
            Vector3[] n = { Up, Vector3.forward };
            Vector2[] uv = { Vector2.zero, Vector2.one };

            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.right, 0.9f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void WithoutAFlowDirection_NothingCanBeMeasured()
        {
            TopQuad(1f, 0f, 1f, out Vector3[] p, out Vector3[] n, out Vector2[] uv);

            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.zero, 0.9f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void WhenThePointsDoNotSpreadAlongTheFlow_NothingCanBeMeasured()
        {
            TopQuad(1f, 0f, 1f, out Vector3[] p, out Vector3[] n, out Vector2[] uv);

            // 흐름이 높이 방향이면 윗면의 점들은 그 방향으로 퍼져 있지 않다.
            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.up, 0.9f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void MismatchedArrays_GiveZeroInsteadOfThrowing()
        {
            Vector3[] p = { Vector3.zero, Vector3.right, Vector3.forward };
            Vector3[] n = { Up, Up };
            Vector2[] uv = { Vector2.zero, Vector2.one, Vector2.up };

            Assert.That(BeltUvFlow.PerMeter(p, n, uv, Vector3.right, 0.9f), Is.EqualTo(Vector2.zero));
        }
    }
}
