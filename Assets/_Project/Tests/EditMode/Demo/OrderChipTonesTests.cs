using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class OrderChipTonesTests
    {
        [Test]
        public void ForIssues_IsSuccessWithoutWarnings()
        {
            Assert.AreEqual(ChipTone.Success, OrderChipTones.ForIssues(0));
        }

        [TestCase(1)]
        [TestCase(5)]
        public void ForIssues_IsWarningWhenThereAreWarnings(int count)
        {
            Assert.AreEqual(ChipTone.Warning, OrderChipTones.ForIssues(count));
        }
    }
}
