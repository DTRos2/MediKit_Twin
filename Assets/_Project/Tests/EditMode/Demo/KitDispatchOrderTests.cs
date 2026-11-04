using System;
using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class KitDispatchOrderTests
    {
        private static KitData Kit(int sequence, float? startTime)
        {
            var kit = new KitData(sequence, sequence, $"LOT-{sequence}", "병원", "BL");
            kit.StartTime = startTime;
            return kit;
        }

        [Test]
        public void WhenBothHaveStartTimes_TheEarlierStartIsEarlier_WhateverTheSequences()
        {
            // 주문을 다시 불러오면 순번이 1부터 다시 시작하므로, 순번은 작아도 나중에 시작한 키트가 있다.
            KitData older = Kit(sequence: 3, startTime: 100f);
            KitData newer = Kit(sequence: 1, startTime: 300f);

            Assert.That(KitDispatchOrder.IsEarlier(older, newer), Is.True);
            Assert.That(KitDispatchOrder.IsEarlier(newer, older), Is.False);
        }

        [Test]
        public void WithoutStartTimes_TheSequenceDecides()
        {
            Assert.That(KitDispatchOrder.IsEarlier(Kit(1, null), Kit(2, null)), Is.True);
            Assert.That(KitDispatchOrder.IsEarlier(Kit(2, null), Kit(1, null)), Is.False);
        }

        [Test]
        public void WhenOnlyOneHasAStartTime_TheSequenceDecides()
        {
            Assert.That(KitDispatchOrder.IsEarlier(Kit(1, 500f), Kit(2, null)), Is.True);
            Assert.That(KitDispatchOrder.IsEarlier(Kit(2, null), Kit(1, 500f)), Is.False);
        }

        [Test]
        public void TheSameKitOrTheSameMoment_IsNotEarlier()
        {
            KitData kit = Kit(1, 10f);

            Assert.That(KitDispatchOrder.IsEarlier(kit, kit), Is.False);
            Assert.That(KitDispatchOrder.IsEarlier(Kit(1, 10f), Kit(2, 10f)), Is.False);
            Assert.That(KitDispatchOrder.IsEarlier(Kit(2, null), Kit(2, null)), Is.False);
        }

        [Test]
        public void ANullKit_IsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => KitDispatchOrder.IsEarlier(null, Kit(1, null)));
            Assert.Throws<ArgumentNullException>(() => KitDispatchOrder.IsEarlier(Kit(1, null), null));
        }
    }
}
