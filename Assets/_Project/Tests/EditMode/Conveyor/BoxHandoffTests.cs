using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class BoxHandoffTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private FakeSupply _supply;
        private FakeEntry _entry;
        private List<(string Code, string Message)> _warnings;
        private BoxHandoff _handoff;

        [SetUp]
        public void SetUp()
        {
            _supply = new FakeSupply();
            _entry = new FakeEntry();
            _warnings = new List<(string, string)>();
            _handoff = new BoxHandoff(_supply, _entry, (code, message) => _warnings.Add((code, message)));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _created)
            {
                UnityEngine.Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Enqueue_TakesTheBoxAndPutsItOnTheConveyor_WhenTheEntryIsFree()
        {
            Transform box = NewBox();
            _supply.Boxes.Enqueue(box);
            KitData kit = NewKit(1);

            _handoff.Enqueue(kit);

            Assert.That(_entry.Entered, Is.EqualTo(new[] { (box, kit) }));
            Assert.That(_handoff.PendingCount, Is.EqualTo(0));
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void Enqueue_LeavesTheBoxOnTheJig_AndWarnsOnce_WhileTheEntryIsBlocked()
        {
            _entry.CanEnter = false;
            _supply.Boxes.Enqueue(NewBox());

            _handoff.Enqueue(NewKit(1));
            _handoff.Pump();
            _handoff.Pump();

            Assert.That(_supply.TakeCount, Is.EqualTo(0), "입구가 막혔을 때 박스를 떼면 박스가 갈 곳을 잃는다");
            Assert.That(_handoff.PendingCount, Is.EqualTo(1));
            Assert.That(_warnings, Has.Count.EqualTo(1));
            Assert.That(_warnings[0].Code, Is.EqualTo(ConveyorWarningCodes.EntryBlocked));
        }

        [Test]
        public void Pump_SendsThePendingKit_OnceTheEntryFreesUp()
        {
            Transform box = NewBox();
            _supply.Boxes.Enqueue(box);
            _entry.CanEnter = false;
            _handoff.Enqueue(NewKit(1));

            _entry.CanEnter = true;
            _handoff.Pump();

            Assert.That(_entry.Entered, Has.Count.EqualTo(1));
            Assert.That(_entry.Entered[0].Box, Is.SameAs(box));
            Assert.That(_handoff.HasPending, Is.False);
        }

        [Test]
        public void Pump_WarnsAgain_WhenTheEntryBlocksAgainAfterBeingFree()
        {
            _entry.CanEnter = false;
            _handoff.Enqueue(NewKit(1));
            _entry.CanEnter = true;
            _supply.Boxes.Enqueue(NewBox());
            _handoff.Pump();

            _entry.CanEnter = false;
            _handoff.Enqueue(NewKit(2));

            Assert.That(_warnings.FindAll(w => w.Code == ConveyorWarningCodes.EntryBlocked), Has.Count.EqualTo(2));
        }

        [Test]
        public void Pump_KeepsTheKitPending_WhileTheJigHasNoBox_AndSendsItWhenOneArrives()
        {
            KitData kit = NewKit(1);
            _handoff.Enqueue(kit);
            Assert.That(_handoff.PendingCount, Is.EqualTo(1));
            Assert.That(_entry.Entered, Is.Empty);

            Transform box = NewBox();
            _supply.Boxes.Enqueue(box);
            _handoff.Pump();

            Assert.That(_entry.Entered, Is.EqualTo(new[] { (box, kit) }));
            Assert.That(_handoff.HasPending, Is.False);
        }

        [Test]
        public void Pump_SendsKitsInTheOrderTheyFinished()
        {
            Transform first = NewBox();
            Transform second = NewBox();
            _supply.Boxes.Enqueue(first);
            _supply.Boxes.Enqueue(second);
            KitData kit1 = NewKit(1);
            KitData kit2 = NewKit(2);

            _handoff.Enqueue(kit1);
            _handoff.Enqueue(kit2);

            Assert.That(_entry.Entered, Is.EqualTo(new[] { (first, kit1), (second, kit2) }));
        }

        [Test]
        public void Pump_WarnsAndDropsTheKit_WhenTheEntryRefusesAfterTheBoxWasTaken()
        {
            _supply.Boxes.Enqueue(NewBox());
            _entry.RefuseEntering = true;

            _handoff.Enqueue(NewKit(7));

            Assert.That(_handoff.HasPending, Is.False, "같은 키트를 다시 보내면 지그에 없는 박스를 또 떼려 한다");
            Assert.That(_warnings, Has.Count.EqualTo(1));
            Assert.That(_warnings[0].Code, Is.EqualTo(ConveyorWarningCodes.EntryFailed));
            Assert.That(_warnings[0].Message, Does.Contain("7"));
        }

        [Test]
        public void Constructor_RejectsMissingCollaborators()
        {
            Assert.Throws<ArgumentNullException>(() => new BoxHandoff(null, _entry, (_, __) => { }));
            Assert.Throws<ArgumentNullException>(() => new BoxHandoff(_supply, null, (_, __) => { }));
            Assert.Throws<ArgumentNullException>(() => new BoxHandoff(_supply, _entry, null));
        }

        private Transform NewBox()
        {
            var box = new GameObject("box");
            _created.Add(box);
            return box.transform;
        }

        private static KitData NewKit(int sequence)
        {
            return new KitData(sequence, sequence, "LOT-" + sequence, "병원", "EM");
        }

        private sealed class FakeSupply : IBoxSupply
        {
            public readonly Queue<Transform> Boxes = new Queue<Transform>();

            public int TakeCount { get; private set; }

            public void PlaceBox(KitData kit, BoxSize size)
            {
            }

            public void RemoveBox()
            {
            }

            public Transform TakeBox()
            {
                if (Boxes.Count == 0)
                {
                    return null;
                }

                TakeCount++;
                return Boxes.Dequeue();
            }
        }

        private sealed class FakeEntry : IBoxEntry
        {
            public readonly List<(Transform Box, KitData Kit)> Entered = new List<(Transform, KitData)>();

            public bool RefuseEntering;

            public bool CanEnter { get; set; } = true;

            public bool TryEnter(Transform box, KitData kit)
            {
                if (RefuseEntering)
                {
                    return false;
                }

                Entered.Add((box, kit));
                return true;
            }
        }
    }
}
