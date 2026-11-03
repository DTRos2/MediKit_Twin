using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class InteractionShadeTests
    {
        private static readonly Color Background = new Color(0.1f, 0.2f, 0.3f, 1f);
        private static readonly Color Highlight = Color.white;

        [Test]
        public void WithoutTheMouse_TheBackgroundIsUntouched()
        {
            Assert.That(InteractionShade.Apply(Background, Highlight, isHover: false, isPressed: false), Is.EqualTo(Background));
        }

        [Test]
        public void Hovering_BlendsTowardTheHighlight()
        {
            Color result = InteractionShade.Apply(Background, Highlight, isHover: true, isPressed: false);

            Assert.That(result, Is.EqualTo(Color.Lerp(Background, Highlight, InteractionShade.HoverAmount)));
            Assert.That(result.g, Is.GreaterThan(Background.g), "어두운 화면에서는 올렸을 때 밝아져야 티가 난다");
        }

        [Test]
        public void Pressing_BlendsMoreThanHovering()
        {
            Color hover = InteractionShade.Apply(Background, Highlight, isHover: true, isPressed: false);
            Color pressed = InteractionShade.Apply(Background, Highlight, isHover: true, isPressed: true);

            Assert.That(pressed.g, Is.GreaterThan(hover.g));
        }

        [Test]
        public void PressingWhileTheMouseIsOutside_DoesNotCountAsPressed()
        {
            // 위젯 위에서 누른 채 밖으로 끌고 나가면 눌린 모양을 보이지 않는다.
            Assert.That(InteractionShade.Apply(Background, Highlight, isHover: false, isPressed: true), Is.EqualTo(Background));
        }

        [Test]
        public void TheBlendAmounts_AreOrderedAndSubtle()
        {
            Assert.That(InteractionShade.HoverAmount, Is.GreaterThan(0f));
            Assert.That(InteractionShade.PressedAmount, Is.GreaterThan(InteractionShade.HoverAmount));
            Assert.That(InteractionShade.PressedAmount, Is.LessThan(0.2f), "너무 많이 섞으면 위의 글자가 바탕에 묻힌다");
        }

        [Test]
        public void TheBackgroundAlpha_IsKept()
        {
            Color translucent = new Color(0.1f, 0.2f, 0.3f, 0.5f);

            Color result = InteractionShade.Apply(translucent, new Color(1f, 1f, 1f, 0.5f), isHover: true, isPressed: false);

            Assert.That(result.a, Is.EqualTo(0.5f).Within(0.0001f));
        }
    }
}
