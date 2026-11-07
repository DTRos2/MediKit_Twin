using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ArmJointPanelTextsTests
    {
        [Test]
        public void JointValue_HasOneDecimalAndTheDegreeMark()
        {
            Assert.AreEqual("12.5°", ArmJointPanelTexts.JointValue(12.5f));
            Assert.AreEqual("-3.0°", ArmJointPanelTexts.JointValue(-3.04f));
            Assert.AreEqual("0.0°", ArmJointPanelTexts.JointValue(0f));
        }

        [Test]
        public void ToolPosition_ListsTheWorldPointInMeters()
        {
            Assert.AreEqual(
                "도구 끝점(월드): x 0.50  y 1.23  z -0.20 m",
                ArmJointPanelTexts.ToolPosition(new Vector3(0.5f, 1.234f, -0.2f)));
        }

        [Test]
        public void ToolApproach_AlwaysShowsTheSign()
        {
            Assert.AreEqual(
                "도구 접근 방향(+Z): x +0.00  y -1.00  z +0.50",
                ArmJointPanelTexts.ToolApproach(new Vector3(0f, -1f, 0.5f)));
        }

        [Test]
        public void ToolEuler_WrapsEachAngleToMinus180Through180()
        {
            Assert.AreEqual(
                "도구 방향(월드 각): x -10°  y 10°  z 180°",
                ArmJointPanelTexts.ToolEuler(new Vector3(350f, 10f, 180f)));
        }

        [Test]
        public void TheNumbers_DoNotDependOnTheCulture()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                Assert.AreEqual("12.5°", ArmJointPanelTexts.JointValue(12.5f), "독일어 문화권에서도 소수점은 점이다");
                Assert.AreEqual("도구 끝점(월드): x 0.50  y 1.23  z -0.20 m", ArmJointPanelTexts.ToolPosition(new Vector3(0.5f, 1.234f, -0.2f)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void TheJointRowsAreEnoughForASixAxisArm()
        {
            Assert.GreaterOrEqual(ArmJointPanelTexts.JointRowCapacity, 6);
        }
    }
}
