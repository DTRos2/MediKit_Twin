using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class TextSpecsTests
    {
        [TestCase(TextRole.Title, DashboardFontRole.Sans, FontStyle.Bold)]
        [TestCase(TextRole.Subtitle, DashboardFontRole.Mono, FontStyle.Normal)]
        [TestCase(TextRole.Body, DashboardFontRole.Sans, FontStyle.Normal)]
        [TestCase(TextRole.BodyBold, DashboardFontRole.Sans, FontStyle.Bold)]
        [TestCase(TextRole.Small, DashboardFontRole.Sans, FontStyle.Normal)]
        [TestCase(TextRole.Mono, DashboardFontRole.Mono, FontStyle.Normal)]
        [TestCase(TextRole.MonoBold, DashboardFontRole.Mono, FontStyle.Bold)]
        [TestCase(TextRole.BigNumber, DashboardFontRole.Mono, FontStyle.Bold)]
        [TestCase(TextRole.State, DashboardFontRole.Sans, FontStyle.Bold)]
        public void For_GivesEachRoleItsFontKindAndWeight(TextRole role, DashboardFontRole font, FontStyle style)
        {
            TextSpec spec = TextSpecs.For(role);

            Assert.AreEqual(font, spec.Font);
            Assert.AreEqual(style, spec.Style);
        }

        [Test]
        public void For_EveryRoleHasAReadableSize()
        {
            foreach (TextRole role in Enum.GetValues(typeof(TextRole)))
            {
                Assert.Greater(TextSpecs.For(role).Size, 0, role.ToString());
            }
        }

        [Test]
        public void For_SizesComeFromTheMetrics_NotFromNumbersOfTheirOwn()
        {
            Assert.AreEqual(DashboardMetrics.TitleFontSize, TextSpecs.For(TextRole.Title).Size);
            Assert.AreEqual(DashboardMetrics.BodyFontSize, TextSpecs.For(TextRole.Body).Size);
            Assert.AreEqual(DashboardMetrics.SmallFontSize, TextSpecs.For(TextRole.Small).Size);
            Assert.AreEqual(DashboardMetrics.MonoFontSize, TextSpecs.For(TextRole.Mono).Size);
            Assert.AreEqual(DashboardMetrics.BigNumberFontSize, TextSpecs.For(TextRole.BigNumber).Size);
            Assert.AreEqual(DashboardMetrics.StateFontSize, TextSpecs.For(TextRole.State).Size);
        }

        [Test]
        public void For_SubtitleIsSmallerThanSmallText_SoTheTitleReadsFirst()
        {
            Assert.Less(TextSpecs.For(TextRole.Subtitle).Size, TextSpecs.For(TextRole.Small).Size);
        }
    }
}
