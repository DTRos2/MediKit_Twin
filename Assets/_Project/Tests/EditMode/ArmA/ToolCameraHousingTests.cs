using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ToolCameraHousingTests
    {
        private GameObject _camera;

        [SetUp]
        public void SetUp()
        {
            _camera = new GameObject("camera");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_camera);
        }

        [Test]
        public void AttachMakesABoxAndALens_UnderTheCamera_WithoutColliders()
        {
            List<Renderer> renderers = ToolCameraHousing.Attach(_camera.transform);

            Assert.That(renderers.Count, Is.EqualTo(2));
            Assert.That(_camera.GetComponentsInChildren<Collider>(), Is.Empty, "도구 링크 물리에 끼면 안 된다");
            Assert.That(renderers.TrueForAll(r => r.transform.IsChildOf(_camera.transform)), Is.True);
        }

        [Test]
        public void TheBoxSitsBehindTheLens_SoItDoesNotBlockTheView()
        {
            ToolCameraHousing.Attach(_camera.transform);

            Transform box = _camera.transform.Find("ToolCameraBox");

            Assert.That(box.localPosition.z, Is.LessThan(0f));
            Assert.That(box.localPosition.z + box.localScale.z * 0.5f, Is.LessThanOrEqualTo(1e-5f), "상자 앞면이 렌즈 면을 넘지 않는다");
        }

        [Test]
        public void RenderImage_IsOnByDefault_SoTheCameraScreenCanBeShown()
        {
            Assert.That(new ToolCameraSettings().RenderImage, Is.True);
        }

        [Test]
        public void ShowCameraBody_IsOnByDefault()
        {
            Assert.That(new ToolCameraSettings().ShowCameraBody, Is.True);
            Assert.That(new ToolCameraSettings().HideOwnBody, Is.True);
        }
    }
}
