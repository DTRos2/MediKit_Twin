using MediKitTwin.Dashboard.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class UiButtonColorsTests
    {
        private static readonly DashboardPalette Palette = DashboardPalette.Default;

        [TestCase(ButtonKind.Primary)]
        [TestCase(ButtonKind.Secondary)]
        [TestCase(ButtonKind.Selected)]
        public void TheNormalColor_IsTheSameBackgroundTheImguiButtonUses(ButtonKind kind)
        {
            ButtonColors.Resolve(kind, Palette, out Color background, out _, out _, out _);

            Assert.AreEqual(background, UiButtonColors.For(kind, Palette).normalColor);
        }

        [Test]
        public void TheColorsOfOtherStates_FollowTheInteractionShade()
        {
            ColorBlock colors = UiButtonColors.For(ButtonKind.Secondary, Palette);

            Assert.AreEqual(InteractionShade.Apply(colors.normalColor, Palette.Highlight, isHover: true, isPressed: false), colors.highlightedColor);
            Assert.AreEqual(InteractionShade.Apply(colors.normalColor, Palette.Highlight, isHover: true, isPressed: true), colors.pressedColor);
        }

        [Test]
        public void ADisabledButton_IsDimmedAndTheColorChangesAtOnce()
        {
            ColorBlock colors = UiButtonColors.For(ButtonKind.Selected, Palette);

            Assert.AreEqual(colors.normalColor.a * DashboardMetrics.DisabledAlpha, colors.disabledColor.a, 1e-5f);
            Assert.AreEqual(0f, colors.fadeDuration);
        }
    }
}
