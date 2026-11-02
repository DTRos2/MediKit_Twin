using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class BasePoseReportTests
    {
        private static readonly BasePlacement Root = new BasePlacement(new Vector3(1.234f, 0.3f, -4.567f), 90f);

        [Test]
        public void ShowsTheRootInCentimetersAndTheYawToATenthOfADegree()
        {
            string text = BasePoseReport.Format(Root, null);

            Assert.That(text, Is.EqualTo("루트 x 1.23  z -4.57  방향 90.0°"));
        }

        [Test]
        public void WithTheArmBase_AppendsItsFloorPosition()
        {
            string text = BasePoseReport.Format(Root, new Vector3(1.5f, 0.9f, -4f));

            Assert.That(text, Is.EqualTo("루트 x 1.23  z -4.57  방향 90.0° | 팔 베이스 x 1.50  z -4.00"));
        }

        [Test]
        public void TheHeightIsNotShown()
        {
            string text = BasePoseReport.Format(Root, new Vector3(1.5f, 7.77f, -4f));

            Assert.That(text, Does.Not.Contain("7.77"));
            Assert.That(text, Does.Not.Contain("0.30"));
        }

        [Test]
        public void TheDecimalMarkIsAlwaysADot()
        {
            string text = BasePoseReport.Format(new BasePlacement(new Vector3(0.5f, 0f, 0.5f), 12.5f), null);

            Assert.That(text, Does.Contain("0.50"));
            Assert.That(text, Does.Contain("12.5"));
        }
    }
}
