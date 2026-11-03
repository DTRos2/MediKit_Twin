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
