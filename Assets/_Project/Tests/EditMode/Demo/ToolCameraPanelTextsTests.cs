using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Demo.Tests
{
    public sealed class ToolCameraPanelTextsTests
    {
        [Test]
        public void CameraPosition_ListsTheWorldPointInCentimeters()
        {
            Assert.AreEqual(
                "x 50.0 · y 123.4 · z -20.0 cm",
                ToolCameraPanelTexts.CameraPosition(new Vector3(0.5f, 1.234f, -0.2f)));
        }

        [Test]
        public void Detect_IsOneWhenFoundAndZeroOtherwise()
        {
            Assert.AreEqual("DETECT 1", ToolCameraPanelTexts.Detect(true));
            Assert.AreEqual("DETECT 0", ToolCameraPanelTexts.Detect(false));
        }
    }
}
