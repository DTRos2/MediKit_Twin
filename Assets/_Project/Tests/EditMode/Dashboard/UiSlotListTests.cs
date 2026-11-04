using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard.Ui;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class UiSlotListTests
    {
        private const int SlotCount = 3;

        private GameObject _scene;
        private Transform _container;
        private UiTextRow[] _slots;

        [SetUp]
        public void SetUp()
        {
            _scene = new GameObject("SlotTestScene");
            _container = new GameObject("Container").transform;
            _container.SetParent(_scene.transform, false);

            // 편집기에서 미리 계층에 놓아 둔 칸을 흉내 낸다.
            _slots = new UiTextRow[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                _slots[i] = new GameObject($"Slot{i}").AddComponent<UiTextRow>();
                _slots[i].transform.SetParent(_container, false);
                _slots[i].gameObject.SetActive(false);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_scene);
        }

        private int ActiveSlotCount()
        {
            int count = 0;
            foreach (UiTextRow slot in _slots)
            {
                count += slot.gameObject.activeSelf ? 1 : 0;
            }

            return count;
        }

        [Test]
        public void Show_TurnsOnOneSlotPerItem_AndBindsEachToItsItem()
        {
            var list = new UiSlotList<UiTextRow>(_slots);
            var bound = new List<string>();

            int hidden = list.Show(new[] { "a", "b" }, (row, item) => bound.Add(item));

            CollectionAssert.AreEqual(new[] { "a", "b" }, bound);
            Assert.AreEqual(2, ActiveSlotCount());
            Assert.AreEqual(2, list.ActiveCount);
            Assert.AreEqual(0, hidden);
        }

        [Test]
        public void Show_NeverCreatesOrDestroysObjects()
        {
            var list = new UiSlotList<UiTextRow>(_slots);

            list.Show(new[] { 1, 2, 3, 4, 5 }, (row, item) => { });
            list.Show(new[] { 1 }, (row, item) => { });
            list.Show(Array.Empty<int>(), (row, item) => { });

            Assert.AreEqual(SlotCount, _container.childCount, "칸은 미리 놓아 둔 것만 쓰고 실행 중에 늘거나 줄지 않아야 한다");
        }

        [Test]
        public void Show_WithMoreItemsThanSlots_FillsEverySlot_AndReportsHowManyDidNotFit()
        {
            var list = new UiSlotList<UiTextRow>(_slots);
            var bound = new List<int>();

            int hidden = list.Show(new[] { 10, 20, 30, 40, 50 }, (row, item) => bound.Add(item));

            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, bound, "앞의 칸 수만큼만 채운다");
            Assert.AreEqual(SlotCount, ActiveSlotCount());
            Assert.AreEqual(2, hidden);
        }

        [Test]
        public void Show_WithFewerItems_TurnsTheExtraSlotsOff()
        {
            var list = new UiSlotList<UiTextRow>(_slots);
            list.Show(new[] { 1, 2, 3 }, (row, item) => { });

            list.Show(new[] { 1 }, (row, item) => { });

            Assert.AreEqual(1, ActiveSlotCount());
            Assert.AreEqual(1, list.ActiveCount);
        }

        [Test]
        public void Show_WithNoItems_TurnsEverySlotOff()
        {
            var list = new UiSlotList<UiTextRow>(_slots);
            list.Show(new[] { 1, 2 }, (row, item) => { });

            list.Show(Array.Empty<int>(), (row, item) => { });

            Assert.AreEqual(0, ActiveSlotCount());
            Assert.AreEqual(0, list.ActiveCount);
        }

        [Test]
        public void Show_BindsEveryShownSlotAgain_SoStaleValuesNeverSurvive()
        {
            var list = new UiSlotList<UiTextRow>(_slots);
            var seen = new List<int>();
            list.Show(new[] { 10, 20 }, (row, item) => { });

            list.Show(new[] { 30, 40 }, (row, item) => seen.Add(item));

            CollectionAssert.AreEqual(new[] { 30, 40 }, seen);
        }

        [Test]
        public void Capacity_IsTheNumberOfSlotsPlacedInAdvance()
        {
            Assert.AreEqual(SlotCount, new UiSlotList<UiTextRow>(_slots).Capacity);
        }

        [Test]
        public void Constructor_RejectsMissingSlots()
        {
            Assert.Throws<ArgumentNullException>(() => new UiSlotList<UiTextRow>(null));
        }

        [Test]
        public void Show_RejectsMissingItemsOrBinder()
        {
            var list = new UiSlotList<UiTextRow>(_slots);

            Assert.Throws<ArgumentNullException>(() => list.Show<int>(null, (row, item) => { }));
            Assert.Throws<ArgumentNullException>(() => list.Show(new[] { 1 }, null));
        }
    }
}
