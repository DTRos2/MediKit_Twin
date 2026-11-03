using System;
using ConveyorSystem;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    /// <summary>
    /// 컨베이어 속도 맞추기의 입력 검사와 기본값을 본다. 이 시험 폴더는 에디터 전용 어셈블리라 컨베이어 구간(MonoBehaviour)을 만들어 붙일 수 없어서,
    /// 구간에 실제로 속도가 들어갔는지는 씬을 여는 PlayMode 시험(ConveyorHandoffTests)이 본다.
    /// </summary>
    public sealed class ConveyorSpeedTests
    {
        [Test]
        public void Apply_SkipsMissingSegments_WithoutFailing()
        {
            int changed = ConveyorSpeed.Apply(new ConveyorBase[] { null, null }, 0.5f);

            Assert.That(changed, Is.EqualTo(0));
        }

        [Test]
        public void Apply_ToNoSegments_ChangesNothing()
        {
            Assert.That(ConveyorSpeed.Apply(new ConveyorBase[0], 0.5f), Is.EqualTo(0));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Apply_RejectsASpeedThatWouldStopTheBoxes(float speed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ConveyorSpeed.Apply(new ConveyorBase[0], speed));
        }

        [Test]
        public void Apply_RejectsAMissingList()
        {
            Assert.Throws<ArgumentNullException>(() => ConveyorSpeed.Apply(null, 1f));
        }

        [Test]
        public void TheDefaultSpeed_IsSlowerThanTheAssetDefault_ButStillMoves()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.PauserConveyorPrefab);
            Assert.That(prefab, Is.Not.Null, $"컨베이어 프리팹이 없다: {ProjectAssetPaths.PauserConveyorPrefab}");
            float assetDefault = prefab.GetComponent<ConveyorBase>().Speed;

            Assert.That(ConveyorSpeedSetter.DefaultSpeedMps, Is.LessThan(assetDefault));
            Assert.That(ConveyorSpeedSetter.DefaultSpeedMps, Is.GreaterThan(0f));
        }
    }
}
