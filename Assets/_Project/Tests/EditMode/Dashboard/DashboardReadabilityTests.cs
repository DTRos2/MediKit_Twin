using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    /// <summary>
    /// 어두운 관제 화면에서 글자가 바탕 위에서 읽히는지 수치로 지킨다. 화면을 눈으로 보지 않고도 색을 바꿀 때 대비가 무너지는 것을 잡는다.
    /// </summary>
    public sealed class DashboardReadabilityTests
    {
        private static readonly DashboardPalette Palette = DashboardPalette.Default;

        [Test]
        public void TheTextColors_AreReadableOnTheCard()
        {
            AssertReadable("Ink", Palette.Ink, Palette.Card);
            AssertReadable("InkSoft", Palette.InkSoft, Palette.Card);
            AssertReadable("Muted", Palette.Muted, Palette.Card);
            AssertReadable("Faint", Palette.Faint, Palette.Card);
        }

        [Test]
        public void TheTextColors_AreReadableOnTheSurfaces()
        {
            AssertReadable("Ink on Surface", Palette.Ink, Palette.Surface);
            AssertReadable("InkSoft on Surface", Palette.InkSoft, Palette.Surface);
            AssertReadable("Muted on Surface", Palette.Muted, Palette.Surface);
            AssertReadable("Faint on Surface", Palette.Faint, Palette.Surface);
            AssertReadable("Ink on Field", Palette.Ink, Palette.Field);
        }

        [Test]
        public void TheTextOnTheAccentFill_IsReadable()
        {
            AssertReadable("OnAccent on Accent", Palette.OnAccent, Palette.Accent);
        }

        [Test]
        public void EachStatusColor_IsReadableOnItsSoftBackground()
        {
            AssertReadable("Success", Palette.Success, Palette.SuccessSoft);
            AssertReadable("Warning", Palette.Warning, Palette.WarningSoft);
            AssertReadable("Danger", Palette.Danger, Palette.DangerSoft);
            AssertReadable("Accent", Palette.Accent, Palette.AccentSoft);
        }

        [Test]
        public void EachStatusColor_IsReadableOnTheCard()
        {
            AssertReadable("Success on Card", Palette.Success, Palette.Card);
            AssertReadable("Warning on Card", Palette.Warning, Palette.Card);
            AssertReadable("Danger on Card", Palette.Danger, Palette.Card);
            AssertReadable("Accent on Card", Palette.Accent, Palette.Card);
        }

        [Test]
        public void TheKitInkColors_AreReadableOnTheCard()
        {
            AssertReadable("KitEmInk", Palette.KitEmInk, Palette.Card);
            AssertReadable("KitDxInk", Palette.KitDxInk, Palette.Card);
            AssertReadable("KitBlInk", Palette.KitBlInk, Palette.Card);
        }

        [Test]
        public void TheKitBadgeText_IsReadableOnEachKitFill()
        {
            // 배지는 밝은 키트색 바탕 위에 어두운 글자(OnAccent)를 쓴다. 밝은 글자(Ink)를 쓰면 주황·파랑·보라 위에서 읽히지 않는다.
            AssertReadable("OnAccent on KitEm", Palette.OnAccent, Palette.KitEm);
            AssertReadable("OnAccent on KitDx", Palette.OnAccent, Palette.KitDx);
            AssertReadable("OnAccent on KitBl", Palette.OnAccent, Palette.KitBl);
        }

        [Test]
        public void TheCameraChipText_IsReadableOnTheOverlay_EvenOverABrightImage()
        {
            // 칩은 반투명이라 밝은 카메라 화면 위에서는 바탕이 밝아진다. 가장 나쁜 경우(흰 화면)로 본다.
            Color overlay = Palette.CameraOverlay;
            Color overWhite = Color.Lerp(Color.white, new Color(overlay.r, overlay.g, overlay.b, 1f), overlay.a);

            AssertReadable("Ink on the camera chip over white", Palette.Ink, overWhite);
        }

        [Test]
        public void ThePendingSegment_IsToldApartFromTheCard()
        {
            Assert.That(ContrastRatio.Between(Palette.Track, Palette.Card), Is.GreaterThan(1.2f), "빈 칸이 카드 바탕에 묻히면 칸이 안 보인다");
        }

        [Test]
        public void TheDoneSegmentAndTheActiveOutline_AreClearlyVisibleOnTheTrack()
        {
            Assert.That(ContrastRatio.Between(Palette.Accent, Palette.Track), Is.GreaterThan(3f), "채워진 칸과 진행 중 테두리는 빈 칸 위에서 또렷해야 한다");
        }

        [Test]
        public void ThePanelsAreOpaque()
        {
            Assert.That(Palette.Card.a, Is.EqualTo(1f));
            Assert.That(Palette.CardBorder.a, Is.EqualTo(1f));
            Assert.That(Palette.Surface.a, Is.EqualTo(1f));
        }

        [Test]
        public void TheTextOnAPressedSurface_StaysReadable()
        {
            // 선반 칸·버튼·스위치 줄은 눌린 동안 바탕이 밝아진다. 그 상태에서도 가장 흐린 글자(Muted)까지 읽혀야 한다.
            Color pressed = InteractionShade.Apply(Palette.Surface, Palette.Highlight, isHover: true, isPressed: true);

            AssertReadable("Ink on a pressed Surface", Palette.Ink, pressed);
            AssertReadable("InkSoft on a pressed Surface", Palette.InkSoft, pressed);
            AssertReadable("Muted on a pressed Surface", Palette.Muted, pressed);
        }

        [Test]
        public void TheButtonTexts_StayReadableWhenPressed()
        {
            Color primary = InteractionShade.Apply(Palette.Accent, Palette.Highlight, isHover: true, isPressed: true);
            Color selected = InteractionShade.Apply(Palette.AccentSoft, Palette.Highlight, isHover: true, isPressed: true);

            AssertReadable("OnAccent on a pressed primary button", Palette.OnAccent, primary);
            AssertReadable("Accent on a pressed selected button", Palette.Accent, selected);
        }

        private static void AssertReadable(string what, Color text, Color background)
        {
            float ratio = ContrastRatio.Between(text, background);

            Assert.That(ratio, Is.GreaterThanOrEqualTo(ContrastRatio.MinimumForText), $"{what}: 대비 {ratio:0.0}:1 — {ContrastRatio.MinimumForText}:1 이상이어야 읽힌다");
        }
    }
}
