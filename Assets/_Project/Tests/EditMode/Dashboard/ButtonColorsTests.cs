using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class ButtonColorsTests
    {
        private static readonly DashboardPalette Palette = DashboardPalette.Default;

        [Test]
        public void Primary_UsesTheAccentColorWithoutABorder()
        {
            ButtonColors.Resolve(ButtonKind.Primary, Palette, out Color background, out Color foreground, out _, out bool hasBorder);

            Assert.AreEqual(Palette.Accent, background);
            Assert.AreEqual(Palette.OnAccent, foreground);
            Assert.IsFalse(hasBorder);
        }

        [Test]
        public void Secondary_UsesTheSurfaceWithABorder()
        {
            ButtonColors.Resolve(ButtonKind.Secondary, Palette, out Color background, out Color foreground, out Color border, out bool hasBorder);

            Assert.AreEqual(Palette.Surface, background);
            Assert.AreEqual(Palette.Ink, foreground);
            Assert.AreEqual(Palette.SurfaceBorder, border);
            Assert.IsTrue(hasBorder);
        }

        [Test]
        public void Selected_LooksSwitchedOn_WithASoftAccentAndAnAccentBorder()
        {
            ButtonColors.Resolve(ButtonKind.Selected, Palette, out Color background, out Color foreground, out Color border, out bool hasBorder);

            Assert.AreEqual(Palette.AccentSoft, background);
            Assert.AreEqual(Palette.Accent, foreground);
            Assert.AreEqual(Palette.AccentThumb, border);
            Assert.IsTrue(hasBorder);
        }
    }
}
