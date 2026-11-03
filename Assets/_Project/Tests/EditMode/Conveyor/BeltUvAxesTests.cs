using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class BeltUvAxesTests
    {
        [Test]
        public void AFlowAlongThePatternAxis_IsNotAcrossThePattern()
        {
            Assert.That(BeltUvAxes.IsAcrossPattern(new Vector2(1f, 0f), UvAxis.U), Is.False);
            Assert.That(BeltUvAxes.IsAcrossPattern(new Vector2(0f, -1f), UvAxis.V), Is.False);
        }

        [Test]
        public void AFlowOnTheOtherAxis_IsAcrossThePattern_SoMovingTheOffsetShowsNothing()
        {
            Assert.That(BeltUvAxes.IsAcrossPattern(new Vector2(0f, 1f), UvAxis.U), Is.True);
            Assert.That(BeltUvAxes.IsAcrossPattern(new Vector2(-1f, 0f), UvAxis.V), Is.True);
        }

        [Test]
        public void AFlowThatIsMostlyAlongThePatternAxis_StaysAsItIs()
        {
            // 흐름이 비스듬해도 무늬 축 성분이 더 크면 무늬가 움직이는 것이 보인다.
            Assert.That(BeltUvAxes.IsAcrossPattern(new Vector2(0.9f, 0.3f), UvAxis.U), Is.False);
            Assert.That(BeltUvAxes.IsAcrossPattern(new Vector2(0.3f, 0.9f), UvAxis.U), Is.True);
        }

        [Test]
        public void SwappingACoordinate_ExchangesUAndV()
        {
            Assert.That(BeltUvAxes.Swapped(new Vector2(0.25f, 0.75f)), Is.EqualTo(new Vector2(0.75f, 0.25f)));
        }

        [Test]
        public void SwappingAFlow_MakesAnAcrossFlowAlongThePattern()
        {
            Vector2 acrossFlow = new Vector2(0f, 1f);

            Vector2 swapped = BeltUvAxes.Swapped(acrossFlow);

            Assert.That(BeltUvAxes.IsAcrossPattern(swapped, UvAxis.U), Is.False);
        }

        [Test]
        public void SwappingAMeshsCoordinates_KeepsTheCountAndDoesNotChangeTheOriginal()
        {
            var uvs = new[] { new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.2f, 1f) };

            Vector2[] swapped = BeltUvAxes.Swapped(uvs);

            Assert.That(swapped.Length, Is.EqualTo(uvs.Length));
            Assert.That(swapped[1], Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(uvs[1], Is.EqualTo(new Vector2(1f, 0.5f)), "원본 좌표는 그대로여야 한다");
        }
    }
}
