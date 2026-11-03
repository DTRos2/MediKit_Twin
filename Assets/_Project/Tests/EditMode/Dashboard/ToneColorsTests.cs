using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class ToneColorsTests
    {
        private static readonly DashboardPalette Palette = DashboardPalette.Default;

        [Test]
        public void EachChipTone_UsesItsSoftBackgroundAndStrongText()
        {
            ChipColors.Resolve(ChipTone.Success, Palette, out Color successBackground, out Color successText);
            ChipColors.Resolve(ChipTone.Warning, Palette, out Color warningBackground, out Color warningText);
            ChipColors.Resolve(ChipTone.Danger, Palette, out Color dangerBackground, out Color dangerText);
            ChipColors.Resolve(ChipTone.Accent, Palette, out Color accentBackground, out Color accentText);

            Assert.That((successBackground, successText), Is.EqualTo((Palette.SuccessSoft, Palette.Success)));
            Assert.That((warningBackground, warningText), Is.EqualTo((Palette.WarningSoft, Palette.Warning)));
            Assert.That((dangerBackground, dangerText), Is.EqualTo((Palette.DangerSoft, Palette.Danger)));
            Assert.That((accentBackground, accentText), Is.EqualTo((Palette.AccentSoft, Palette.Accent)));
        }

        [Test]
        public void TheNeutralChipTone_UsesTheSurfaceColors()
        {
            ChipColors.Resolve(ChipTone.Neutral, Palette, out Color background, out Color text);

            Assert.That((background, text), Is.EqualTo((Palette.Surface, Palette.InkSoft)));
        }

        [Test]
        public void EveryChipTone_HasAReadableTextOnItsBackground()
        {
            foreach (ChipTone tone in Enum.GetValues(typeof(ChipTone)))
            {
                ChipColors.Resolve(tone, Palette, out Color background, out Color text);

                Assert.That(text, Is.Not.EqualTo(background), $"{tone}: 글자색이 바탕색과 같으면 읽을 수 없다");
            }
        }

        [Test]
        public void EachTagTone_UsesItsBorderAndTextColors()
        {
            TagColors.Resolve(TagTone.Success, Palette, out Color successBorder, out Color successText);
            TagColors.Resolve(TagTone.Accent, Palette, out Color accentBorder, out Color accentText);
            TagColors.Resolve(TagTone.Violet, Palette, out Color violetBorder, out Color violetText);
            TagColors.Resolve(TagTone.Warning, Palette, out Color warningBorder, out Color warningText);
            TagColors.Resolve(TagTone.Danger, Palette, out Color dangerBorder, out Color dangerText);

            Assert.That((successBorder, successText), Is.EqualTo((Palette.Success, Palette.Success)));
            Assert.That((accentBorder, accentText), Is.EqualTo((Palette.AccentThumb, Palette.Accent)));
            Assert.That((violetBorder, violetText), Is.EqualTo((Palette.KitBl, Palette.KitBlInk)));
            Assert.That((warningBorder, warningText), Is.EqualTo((Palette.KitEm, Palette.Warning)));
            Assert.That((dangerBorder, dangerText), Is.EqualTo((Palette.Danger, Palette.Danger)));
        }

        [Test]
        public void TheNeutralTagTone_UsesTheFaintBorderAndMutedText()
        {
            TagColors.Resolve(TagTone.Neutral, Palette, out Color border, out Color text);

            Assert.That((border, text), Is.EqualTo((Palette.Faint, Palette.Muted)));
        }
    }
}
