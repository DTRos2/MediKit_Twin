using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TextCore.Text;

namespace MediKitTwin.Dashboard.Tests
{
    /// <summary>
    /// 고른 글꼴이 IMGUI가 쓰는 폰트 에셋으로 실제로 바뀌는지 본다. 바뀌지 않는 글꼴이 뽑히면 글자를 그릴 때마다 경고가 쏟아져 화면이 느려진다.
    /// 어떤 글꼴이 깔려 있는지는 컴퓨터마다 달라서, 글꼴이 하나도 없는 환경(null)은 건너뛴다.
    /// </summary>
    public sealed class DashboardFontsTests
    {
        [Test]
        public void Sans_ConvertsToAFontAsset_WithoutWarnings()
        {
            AssertUsable(DashboardFonts.Sans);
        }

        [Test]
        public void Mono_ConvertsToAFontAsset_WithoutWarnings()
        {
            AssertUsable(DashboardFonts.Mono);
        }

        [Test]
        public void TheFontsAreResolvedOnce_SoTheyAreNotTriedAgainEveryFrame()
        {
            Assert.That(DashboardFonts.Sans, Is.SameAs(DashboardFonts.Sans));
            Assert.That(DashboardFonts.Mono, Is.SameAs(DashboardFonts.Mono));
        }

        // Domain Reload를 끈 에디터에서는 이전 Play의 글꼴이 정적 변수에 남아 두 번째 Play부터 글자가 비어 그려졌다.
        [Test]
        public void ResetCache_DiscardsTheCachedFont_AndTheNextUsePicksAFreshOne()
        {
            Font before = DashboardFonts.Sans;
            if (before == null)
            {
                Assert.Ignore("이 컴퓨터에는 쓸 수 있는 글꼴이 없어 Unity 기본 글꼴을 쓴다.");
            }

            DashboardFonts.ResetCache();
            Font after = DashboardFonts.Sans;

            Assert.That(before == null, Is.True, "이전 글꼴이 버려지지 않고 남았다");
            Assert.That(after != null, Is.True, "비운 뒤 새 글꼴을 고르지 못했다");
            Assert.That(after, Is.Not.SameAs(before));
        }

        [Test]
        public void AFontThatWasDestroyed_IsNotHandedOutAgain()
        {
            Font font = DashboardFonts.Sans;
            if (font == null)
            {
                Assert.Ignore("이 컴퓨터에는 쓸 수 있는 글꼴이 없어 Unity 기본 글꼴을 쓴다.");
            }

            Object.DestroyImmediate(font);
            Font next = DashboardFonts.Sans;

            Assert.That(next != null, Is.True, "파괴된 글꼴이 캐시에 남아 그대로 나왔다");
        }

        [Test]
        public void ResetCache_CanBeCalledAnyTime_EvenBeforeAnythingWasResolved()
        {
            DashboardFonts.ResetCache();
            Assert.DoesNotThrow(DashboardFonts.ResetCache);
        }

        private static void AssertUsable(Font font)
        {
            if (font == null)
            {
                Assert.Ignore("이 컴퓨터에는 쓸 수 있는 글꼴이 없어 Unity 기본 글꼴을 쓴다.");
            }

            FontAsset asset = FontAsset.CreateFontAsset(font);

            Assert.That(asset, Is.Not.Null, $"고른 글꼴 '{font.name}'이 폰트 에셋으로 바뀌지 않는다");
            LogAssert.NoUnexpectedReceived();
            Object.DestroyImmediate(asset);
        }
    }
}
