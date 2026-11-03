using NUnit.Framework;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class KitBadgePaletteTests
    {
        private static readonly DashboardPalette Palette = DashboardPalette.Default;

        [Test]
        public void EachKnownKit_HasItsOwnFillColor()
        {
            Assert.That(KitBadgePalette.Fill("EM", Palette), Is.EqualTo(Palette.KitEm));
            Assert.That(KitBadgePalette.Fill("DX", Palette), Is.EqualTo(Palette.KitDx));
            Assert.That(KitBadgePalette.Fill("BL", Palette), Is.EqualTo(Palette.KitBl));
        }

        [Test]
        public void EachKnownKit_HasItsOwnInkColor()
        {
            Assert.That(KitBadgePalette.Ink("EM", Palette), Is.EqualTo(Palette.KitEmInk));
            Assert.That(KitBadgePalette.Ink("DX", Palette), Is.EqualTo(Palette.KitDxInk));
            Assert.That(KitBadgePalette.Ink("BL", Palette), Is.EqualTo(Palette.KitBlInk));
        }

        [TestCase("em")]
        [TestCase(" EM ")]
        [TestCase("Em")]
        public void TheKitCode_IgnoresCaseAndSurroundingSpaces(string code)
        {
            Assert.That(KitBadgePalette.Fill(code, Palette), Is.EqualTo(Palette.KitEm));
            Assert.That(KitBadgePalette.Ink(code, Palette), Is.EqualTo(Palette.KitEmInk));
        }

        [TestCase("XX")]
        [TestCase("")]
        [TestCase(null)]
        public void AnUnknownKit_GetsTheNeutralColors(string code)
        {
            Assert.That(KitBadgePalette.Fill(code, Palette), Is.EqualTo(Palette.Track));
            Assert.That(KitBadgePalette.Ink(code, Palette), Is.EqualTo(Palette.Muted));
        }

        [Test]
        public void TheThreeKits_AreVisuallyDistinct()
        {
            Assert.That(Palette.KitEm, Is.Not.EqualTo(Palette.KitDx));
            Assert.That(Palette.KitDx, Is.Not.EqualTo(Palette.KitBl));
            Assert.That(Palette.KitEm, Is.Not.EqualTo(Palette.KitBl));
        }
    }
}
