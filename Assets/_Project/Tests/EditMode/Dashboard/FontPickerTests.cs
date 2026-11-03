using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class FontPickerTests
    {
        private static readonly string[] Names = { "A", "B", "C" };

        [Test]
        public void FirstUsable_ReturnsTheFirstNameThatCanBeCreated()
        {
            string picked = FontPicker.FirstUsable(Names, name => name == "A" ? null : "font-" + name);

            Assert.That(picked, Is.EqualTo("font-B"));
        }

        [Test]
        public void FirstUsable_SkipsNamesInOrderUntilOneWorks()
        {
            var tried = new List<string>();

            FontPicker.FirstUsable(Names, name =>
            {
                tried.Add(name);
                return name == "C" ? "font" : null;
            });

            Assert.That(tried, Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public void FirstUsable_StopsTryingAfterTheFirstSuccess()
        {
            var tried = new List<string>();

            FontPicker.FirstUsable(Names, name =>
            {
                tried.Add(name);
                return "font";
            });

            Assert.That(tried, Is.EqualTo(new[] { "A" }));
        }

        [Test]
        public void FirstUsable_ReturnsNull_WhenNoNameCanBeCreated()
        {
            Assert.That(FontPicker.FirstUsable<string>(Names, _ => null), Is.Null);
        }

        [Test]
        public void FirstUsable_ReturnsNull_ForAnEmptyList()
        {
            Assert.That(FontPicker.FirstUsable(new string[0], name => "font"), Is.Null);
        }

        [Test]
        public void FirstUsable_RejectsMissingArguments()
        {
            Assert.Throws<ArgumentNullException>(() => FontPicker.FirstUsable<string>(null, name => name));
            Assert.Throws<ArgumentNullException>(() => FontPicker.FirstUsable<string>(Names, null));
        }
    }
}
