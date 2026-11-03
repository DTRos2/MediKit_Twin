using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class BeltScrollTests
    {
        [Test]
        public void AtTheStart_TheOffsetIsZero()
        {
            Assert.That(new BeltScroll().Offset, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TheOffsetMovesAgainstTheUvSlope_SoThePatternMovesWithTheFlow()
        {
            var scroll = new BeltScroll();

            // u가 흐름을 따라 늘어나는 벨트(기울기 +1/m)가 0.25 m/s로 1초 가면 오프셋은 -0.25, 0~1로 말면 0.75다.
            scroll.Advance(new Vector2(1f, 0f), 0.25f, 1f);

            Assert.That(scroll.Offset.x, Is.EqualTo(0.75f).Within(1e-5f));
            Assert.That(scroll.Offset.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void ANegativeSlope_MovesTheOffsetTheOtherWay()
        {
            var scroll = new BeltScroll();

            scroll.Advance(new Vector2(-1f, 0f), 0.25f, 1f);

            Assert.That(scroll.Offset.x, Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void ItAccumulatesOverFrames_AndWrapsIntoZeroToOne()
        {
            var scroll = new BeltScroll();
            var slope = new Vector2(-1f, 0f);

            for (int i = 0; i < 10; i++)
            {
                scroll.Advance(slope, 0.3f, 1f);
            }

            // 3.0 m를 갔으므로 무늬가 정확히 세 번 돌아 처음과 같다.
            Assert.That(scroll.Offset.x, Is.InRange(0f, 1f));
            Assert.That(Mathf.Min(scroll.Offset.x, 1f - scroll.Offset.x), Is.LessThan(1e-4f));
        }

        [Test]
        public void AStoppedBelt_DoesNotMoveThePattern()
        {
            var scroll = new BeltScroll();
            scroll.Advance(new Vector2(-1f, 0f), 0.3f, 0.5f);
            Vector2 before = scroll.Offset;

            scroll.Advance(new Vector2(-1f, 0f), 0f, 1f);

            Assert.That(scroll.Offset, Is.EqualTo(before));
        }

        [Test]
        public void TheOffsetIsNeverNegative_EvenWhenGoingBackwards()
        {
            var scroll = new BeltScroll();

            scroll.Advance(new Vector2(-1f, -1f), -0.4f, 1f);

            Assert.That(scroll.Offset.x, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            Assert.That(scroll.Offset.y, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }
    }
}
