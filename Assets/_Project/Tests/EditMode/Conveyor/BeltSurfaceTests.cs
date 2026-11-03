using ConveyorSystem;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class BeltSurfaceTests
    {
        [Test]
        public void RestOffset_IsHowMuchHigherTheInputPointIsThanTheBeltTop()
        {
            Assert.That(BeltSurface.RestOffset(0.91f, 0.85f), Is.EqualTo(0.06f).Within(1e-5f));
        }

        [TestCase(0.85f, 0.85f)]
        [TestCase(0.80f, 0.85f)]
        public void RestOffset_IsZero_WhenTheInputPointIsAtOrBelowTheBeltTop(float inputY, float beltTopY)
        {
            Assert.That(BeltSurface.RestOffset(inputY, beltTopY), Is.EqualTo(0f));
        }

        [Test]
        public void InputAboveSurface_IsZero_WithoutAConveyor()
        {
            Assert.That(BeltSurface.InputAboveSurface(null), Is.EqualTo(0f));
        }

        [Test]
        public void InputAboveSurface_MeasuresTheBodyRendererTop_AgainstTheInputPoint()
        {
            // 윗면이 y=0.85인 벨트(높이 1m, 가운데 0.35)와 y=0.91에 있는 입력점.
            var conveyorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                conveyorObject.transform.position = new Vector3(0f, 0.35f, 0f);
                var conveyor = conveyorObject.AddComponent<StraightConveyor>();
                var input = new GameObject("Input").transform;
                input.SetParent(conveyorObject.transform, true);
                input.position = new Vector3(0.5f, 0.91f, 0f);
                conveyor.InputPoint = input;

                Assert.That(BeltSurface.InputAboveSurface(conveyor), Is.EqualTo(0.06f).Within(1e-4f));
            }
            finally
            {
                Object.DestroyImmediate(conveyorObject);
            }
        }

        [Test]
        public void InputAboveSurface_IsZero_WhenTheConveyorHasNoBodyRenderer()
        {
            // 컨베이어 에셋은 붙을 때 렌더러가 있다고 보고 움직이므로, 붙인 뒤에 렌더러를 떼어 "렌더러 없는 구간"을 만든다.
            var conveyorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var conveyor = conveyorObject.AddComponent<StraightConveyor>();
                Object.DestroyImmediate(conveyorObject.GetComponent<MeshRenderer>());
                var input = new GameObject("Input").transform;
                input.SetParent(conveyorObject.transform, true);
                conveyor.InputPoint = input;

                Assert.That(BeltSurface.InputAboveSurface(conveyor), Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(conveyorObject);
            }
        }
    }
}
