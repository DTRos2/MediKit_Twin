using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class DashboardPaletteTests
    {
        private const float ChannelTolerance = 1f / 255f;

        [Test]
        public void Hex_ReadsSixDigitColors()
        {
            Color color = DashboardPalette.Hex("#0A6CFF");

            Assert.That(color.r, Is.EqualTo(10f / 255f).Within(ChannelTolerance));
            Assert.That(color.g, Is.EqualTo(108f / 255f).Within(ChannelTolerance));
            Assert.That(color.b, Is.EqualTo(1f).Within(ChannelTolerance));
            Assert.That(color.a, Is.EqualTo(1f));
        }

        [Test]
        public void Hex_ReadsTheAlphaChannelWhenGiven()
        {
            Color color = DashboardPalette.Hex("#FFFFFF80");

            Assert.That(color.a, Is.EqualTo(128f / 255f).Within(ChannelTolerance));
        }

        [TestCase("")]
        [TestCase("0A6CFF")]
        [TestCase("#12")]
        public void Hex_ThrowsOnABadFormat_InsteadOfSilentlyUsingAWrongColor(string text)
        {
            Assert.Throws<ArgumentException>(() => DashboardPalette.Hex(text));
        }

        [Test]
        public void ThePalette_HasNoBadColorLiteral()
        {
            // 팔레트의 모든 색 글자가 올바른 형식이어야 한다(잘못되면 처음 쓰는 순간 예외가 나므로 여기서 먼저 잡는다).
            Assert.DoesNotThrow(() => { _ = DashboardPalette.Default.Card; });
        }

        [Test]
        public void TheCards_AreFullyOpaque_SoThe3DViewDoesNotShowThrough()
        {
            Assert.That(DashboardPalette.Default.Card.a, Is.EqualTo(1f));
        }
    }
}
