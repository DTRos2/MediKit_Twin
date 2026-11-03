#if UNITY_EDITOR
using ConveyorSystem;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 컨베이어 시험이 Scene_Cell에서 찾아 쓰는 오브젝트 모음. 하나라도 없거나 설정이 어긋나 있으면 시험을 바로 실패시킨다.
    /// </summary>
    internal sealed class ConveyorSceneRefs
    {
        // 벨트 폭은 1m(양옆 난간은 중심에서 0.48m). 멈출 자리는 중심선에서 치우쳐 있어도 되지만 벨트 안에는 있어야 한다.
        private const float StopLateralMaxM = 0.4f;

        public KitBoxSupply Supply { get; private set; }
        public PauserConveyor PackingConveyor { get; private set; }
        public Transform PackingSensor { get; private set; }
        public Transform StopPoint { get; private set; }

        /// <summary>팩킹 구간의 진행 방향(입력점에서 출력점으로).</summary>
        public Vector3 Flow => PackingConveyor.OutputPoints[0].position - PackingConveyor.InputPoint.position;

        public static ConveyorSceneRefs Find()
        {
            var refs = new ConveyorSceneRefs
            {
                Supply = Object.FindAnyObjectByType<KitBoxSupply>(),
                PackingConveyor = Object.FindAnyObjectByType<PauserConveyor>(),
                PackingSensor = GameObject.Find(CellObjectNames.PackingSensor)?.transform
            };

            Assert.That(refs.Supply, Is.Not.Null, "씬에서 KitBoxSupply를 찾을 수 없다");
            Assert.That(refs.PackingConveyor, Is.Not.Null, "씬에서 PauserConveyor(팩킹 구간)를 찾을 수 없다");
            Assert.That(refs.PackingSensor, Is.Not.Null, $"씬에서 {CellObjectNames.PackingSensor}를 찾을 수 없다");
            Assert.That(refs.Supply.HasBox, Is.True, "시작할 때 지그에 박스가 있어야 한다");

            // 박스가 설 자리(팔 B가 맞춰진 자리)는 멈춤 구간 안에 있어야 한다. 아니면 엉뚱한 구간을 멈춤 구간으로 바꾼 것이다.
            refs.StopPoint = refs.PackingSensor.Find(CellObjectNames.PackingStopPoint);
            Assert.That(refs.StopPoint, Is.Not.Null, $"{CellObjectNames.PackingSensor} 아래에 {CellObjectNames.PackingStopPoint}가 없다");
            bool inSegment = SegmentProjection.TryProject(
                refs.PackingConveyor.InputPoint.position, refs.PackingConveyor.OutputPoints[0].position, refs.StopPoint.position, out _, out float lateral);
            Assert.That(inSegment, Is.True, "멈출 자리가 멈춤 구간의 입력점과 출력점 사이에 없다");
            Assert.That(lateral, Is.LessThan(StopLateralMaxM), "멈출 자리가 벨트 폭 밖에 있다");

            AssertSlowedDown();
            return refs;
        }

        // 컨베이어 에셋 기본 속도로는 키트 박스가 너무 빠르다. 속도를 정하는 컴포넌트가 있고, 모든 구간이 하나의 같은 속도로 에셋 기본보다 느리게 가는지 본다.
        private static void AssertSlowedDown()
        {
            Assert.That(Object.FindAnyObjectByType<ConveyorSpeedSetter>(), Is.Not.Null, "씬에 ConveyorSpeedSetter가 없다(Tools/MediKit/Setup Conveyor Handoff로 붙인다)");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.PauserConveyorPrefab);
            float assetDefault = prefab.GetComponent<ConveyorBase>().Speed;

            ConveyorBase[] segments = Object.FindObjectsByType<ConveyorBase>();
            Assert.That(segments, Is.Not.Empty, "씬에 컨베이어 구간이 없다");
            foreach (ConveyorBase segment in segments)
            {
                Assert.That(segment.Speed, Is.EqualTo(segments[0].Speed), $"{segment.name}의 속도가 다른 구간과 다르다");
                Assert.That(segment.Speed, Is.LessThan(assetDefault), $"{segment.name}가 에셋 기본 속도({assetDefault}m/s)보다 느리지 않다");
            }
        }
    }
}
#endif
